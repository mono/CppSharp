/************************************************************************
 *
 * CppSharp
 * Licensed under the simplified BSD license. All rights reserved.
 *
 ************************************************************************/

#include "Parser.h"

#include <clang/AST/Comment.h>
#include <clang/AST/ASTContext.h>

using namespace CppSharp::CppParser;
using namespace CppSharp::CppParser::AST;

//-----------------------------------//

static RawCommentKind
ConvertRawCommentKind(clang::RawComment::CommentKind Kind)
{
    using clang::RawComment;

    switch (Kind)
    {
        case RawComment::RCK_Invalid: return RawCommentKind::Invalid;
        case RawComment::RCK_OrdinaryBCPL: return RawCommentKind::OrdinaryBCPL;
        case RawComment::RCK_OrdinaryC: return RawCommentKind::OrdinaryC;
        case RawComment::RCK_BCPLSlash: return RawCommentKind::BCPLSlash;
        case RawComment::RCK_BCPLExcl: return RawCommentKind::BCPLExcl;
        case RawComment::RCK_JavaDoc: return RawCommentKind::JavaDoc;
        case RawComment::RCK_Qt: return RawCommentKind::Qt;
        case RawComment::RCK_Merged: return RawCommentKind::Merged;
    }

    llvm_unreachable("Unknown comment kind");
}

RawComment* Parser::WalkRawComment(const clang::RawComment* RC)
{
    using namespace clang;

    auto& SM = c->getSourceManager();
    auto Comment = new AST::RawComment();
    Comment->kind = ConvertRawCommentKind(RC->getKind());
    Comment->text = RC->getRawText(SM).str();
    Comment->briefText = RC->getBriefText(c->getASTContext());

    return Comment;
}

static InlineCommandComment::RenderKind
ConvertRenderKind(std::string Kind)
{
    using namespace clang::comments;
    if (Kind == "b")
        return AST::InlineCommandComment::RenderKind::RenderBold;
    else if (Kind == "c")
        return AST::InlineCommandComment::RenderKind::RenderMonospaced;
    else if (Kind == "a")
        return AST::InlineCommandComment::RenderKind::RenderAnchor;
    else if (Kind == "e")
        return AST::InlineCommandComment::RenderKind::RenderEmphasized;
    else
        return AST::InlineCommandComment::RenderKind::RenderNormal;
    llvm_unreachable("Unknown render kind");
}

static void HandleInlineContent(const clang::comments::InlineContentComment* CK,
                                InlineContentComment* IC)
{
    IC->hasTrailingNewline = CK->hasTrailingNewline();
}

static void HandleBlockCommand(const clang::comments::BlockCommandComment* CK,
                               BlockCommandComment* BC)
{
    BC->commandId = CK->getCommandID();
    for (unsigned I = 0, E = CK->getNumArgs(); I != E; ++I)
    {
        auto Arg = BlockCommandComment::Argument();
        Arg.text = CK->getArgText(I).str();
        BC->Arguments.push_back(Arg);
    }
}

static Comment* ConvertCommentBlock(clang::comments::Comment* C, clang::CompilerInstance* CI)
{
    using namespace clang;
    using clang::comments::Comment;

    // This needs to have an underscore else we get an ICE under VS2012.
    CppSharp::CppParser::AST::Comment* _Comment = nullptr;
    auto kind = C->getCommentKind();
    if (auto CK = dyn_cast<const comments::FullComment>(C))
    {
        auto FC = new FullComment();
        _Comment = FC;
        for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
            FC->Blocks.push_back(static_cast<BlockContentComment*>(ConvertCommentBlock(*I, CI)));
    }
    else if (auto CK = dyn_cast<const comments::ParamCommandComment>(C))
    {
        auto PC = new ParamCommandComment();
        _Comment = PC;
        HandleBlockCommand(CK, PC);
        if (CK->isParamIndexValid() && !CK->isVarArgParam())
            PC->paramIndex = CK->getParamIndex();
        PC->paragraphComment = static_cast<ParagraphComment*>(ConvertCommentBlock(CK->getParagraph(), CI));
    }
    else if (auto CK = dyn_cast<const comments::TParamCommandComment>(C))
    {
        auto TC = new TParamCommandComment();
        _Comment = TC;
        HandleBlockCommand(CK, TC);
        if (CK->isPositionValid())
            for (unsigned I = 0, E = CK->getDepth(); I != E; ++I)
                TC->Position.push_back(CK->getIndex(I));
        TC->paragraphComment = static_cast<ParagraphComment*>(ConvertCommentBlock(CK->getParagraph(), CI));
    }
    else if (auto CK = dyn_cast<const comments::VerbatimBlockComment>(C))
    {
        auto VB = new VerbatimBlockComment();
        _Comment = VB;
        for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
            VB->Lines.push_back(static_cast<VerbatimBlockLineComment*>(ConvertCommentBlock(*I, CI)));
    }
    else if (auto CK = dyn_cast<const comments::VerbatimLineComment>(C))
    {
        auto VL = new VerbatimLineComment();
        _Comment = VL;
        VL->text = CK->getText().str();
    }
    else if (auto CK = dyn_cast<const comments::BlockCommandComment>(C))
    {
        auto BC = new BlockCommandComment();
        _Comment = BC;
        HandleBlockCommand(CK, BC);
        BC->paragraphComment = static_cast<ParagraphComment*>(ConvertCommentBlock(CK->getParagraph(), CI));
    }
    else if (auto CK = dyn_cast<const comments::ParagraphComment>(C))
    {
        auto PC = new ParagraphComment();
        _Comment = PC;
        for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
            PC->Content.push_back(static_cast<InlineContentComment*>(ConvertCommentBlock(*I, CI)));
        PC->isWhitespace = CK->isWhitespace();
    }
    else if (auto CK = dyn_cast<const comments::HTMLStartTagComment>(C))
    {
        auto TC = new HTMLStartTagComment();
        _Comment = TC;
        HandleInlineContent(CK, TC);
        TC->tagName = CK->getTagName().str();
        for (unsigned I = 0, E = CK->getNumAttrs(); I != E; ++I)
        {
            auto A = CK->getAttr(I);
            auto Attr = HTMLStartTagComment::Attribute();
            Attr.name = A.Name.str();
            Attr.value = A.Value.str();
            TC->Attributes.push_back(Attr);
        }
    }
    else if (auto CK = dyn_cast<const comments::HTMLEndTagComment>(C))
    {
        auto TC = new HTMLEndTagComment();
        _Comment = TC;
        HandleInlineContent(CK, TC);
        TC->tagName = CK->getTagName().str();
    }
    else if (auto CK = dyn_cast<const comments::TextComment>(C))
    {
        auto TC = new TextComment();
        _Comment = TC;
        HandleInlineContent(CK, TC);
        TC->text = CK->getText().str();
    }
    else if (auto CK = dyn_cast<const comments::InlineCommandComment>(C))
    {
        auto IC = new InlineCommandComment();
        _Comment = IC;
        HandleInlineContent(CK, IC);
        IC->commandId = CK->getCommandID();

        IC->commentRenderKind = ConvertRenderKind(CK->getCommandName(CI->getASTContext().getCommentCommandTraits()).str());
        for (unsigned I = 0, E = CK->getNumArgs(); I != E; ++I)
        {
            auto Arg = InlineCommandComment::Argument();
            Arg.text = CK->getArgText(I).str();
            IC->Arguments.push_back(Arg);
        }
    }
    else if (auto CK = dyn_cast<const comments::VerbatimBlockLineComment>(C))
    {
        auto VL = new VerbatimBlockLineComment();
        _Comment = VL;
        VL->text = CK->getText().str();
    }
    else
        llvm_unreachable("Unknown comment kind");

    assert(_Comment && "Invalid comment instance");
    return _Comment;
}

void Parser::HandleComments(const clang::Decl* D, Declaration* Decl)
{
    using namespace clang;

    const clang::RawComment* RC = c->getASTContext().getRawCommentForAnyRedecl(D);
    if (!RC)
        return;

    auto RawComment = WalkRawComment(RC);
    Decl->comment = RawComment;

    if (clang::comments::FullComment* FC = RC->parse(c->getASTContext(), &c->getPreprocessor(), D))
    {
        auto CB = static_cast<FullComment*>(ConvertCommentBlock(FC, c.get()));
        RawComment->fullCommentBlock = CB;
    }
}
