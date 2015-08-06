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

//-----------------------------------//

static RawCommentKind
ConvertRawCommentKind(clang::RawComment::CommentKind Kind)
{
    using clang::RawComment;

    switch(Kind)
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

    auto &SM = C->getSourceManager();
    auto Comment = new RawComment();
    Comment->Kind = ConvertRawCommentKind(RC->getKind());
    Comment->Text = RC->getRawText(SM);
    Comment->BriefText = RC->getBriefText(*AST);

    return Comment;
}

static InlineCommandComment::RenderKind
ConvertRenderKind(clang::comments::InlineCommandComment::RenderKind Kind)
{
    using namespace clang::comments;
    switch(Kind)
    {
    case clang::comments::InlineCommandComment::RenderNormal:
        return CppSharp::CppParser::AST::InlineCommandComment::RenderKind::RenderNormal;
    case clang::comments::InlineCommandComment::RenderBold:
        return CppSharp::CppParser::AST::InlineCommandComment::RenderKind::RenderBold;
    case clang::comments::InlineCommandComment::RenderMonospaced:
        return CppSharp::CppParser::AST::InlineCommandComment::RenderKind::RenderMonospaced;
    case clang::comments::InlineCommandComment::RenderEmphasized:
        return CppSharp::CppParser::AST::InlineCommandComment::RenderKind::RenderEmphasized;
    }
    llvm_unreachable("Unknown render kind");
}

static ParamCommandComment::PassDirection
ConvertParamPassDirection(clang::comments::ParamCommandComment::PassDirection Dir)
{
    using namespace clang::comments;
    switch(Dir)
    {
    case clang::comments::ParamCommandComment::In:
        return CppSharp::CppParser::AST::ParamCommandComment::PassDirection::In;
    case clang::comments::ParamCommandComment::Out:
        return CppSharp::CppParser::AST::ParamCommandComment::PassDirection::Out;
    case clang::comments::ParamCommandComment::InOut:
        return CppSharp::CppParser::AST::ParamCommandComment::PassDirection::InOut;
    }
    llvm_unreachable("Unknown parameter pass direction");
}

static void HandleBlockCommand(const clang::comments::BlockCommandComment *CK,
                               BlockCommandComment* BC)
{
    BC->CommandId = CK->getCommandID();
    for (unsigned I = 0, E = CK->getNumArgs(); I != E; ++I)
    {
        auto Arg = BlockCommandComment::Argument();
        Arg.Text = CK->getArgText(I);
        BC->Arguments.push_back(Arg);
    }
}

static Comment* ConvertCommentBlock(clang::comments::Comment* C)
{
    using namespace clang;
    using clang::comments::Comment;

    // This needs to have an underscore else we get an ICE under VS2012.
    CppSharp::CppParser::AST::Comment* _Comment = 0;

    switch(C->getCommentKind())
    {
    case Comment::FullCommentKind:
    {
        auto CK = cast<clang::comments::FullComment>(C);
        auto FC = new FullComment();
        _Comment = FC;
        for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
        {
            auto Content = ConvertCommentBlock(*I);
            FC->Blocks.push_back(static_cast<BlockContentComment*>(Content));
        }
        break;
    }
    case Comment::BlockCommandCommentKind:
    {
        auto CK = cast<const clang::comments::BlockCommandComment>(C);
        auto BC = new BlockCommandComment();
        _Comment = BC;
        HandleBlockCommand(CK, BC);
        break;
    }
    case Comment::ParamCommandCommentKind:
    {
        auto CK = cast<clang::comments::ParamCommandComment>(C);
        auto PC = new ParamCommandComment();
        _Comment = PC;
        HandleBlockCommand(CK, PC);
        PC->Direction = ConvertParamPassDirection(CK->getDirection());
        if (CK->isParamIndexValid() && !CK->isVarArgParam())
            PC->ParamIndex = CK->getParamIndex();
        break;
    }
    case Comment::TParamCommandCommentKind:
    {
        auto CK = cast<clang::comments::TParamCommandComment>(C);
        _Comment = new TParamCommandComment();
        auto TC = new TParamCommandComment();
        _Comment = TC;
        HandleBlockCommand(CK, TC);
        if (CK->isPositionValid())
            for (unsigned I = 0, E = CK->getDepth(); I != E; ++I)
                TC->Position.push_back(CK->getIndex(I));
        break;
    }
    case Comment::VerbatimBlockCommentKind:
    {
        auto CK = cast<clang::comments::VerbatimBlockComment>(C);
        auto VB = new VerbatimBlockComment();
        _Comment = VB;
        for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
        {
            auto Line = ConvertCommentBlock(*I);
            VB->Lines.push_back(static_cast<VerbatimBlockLineComment*>(Line));
        }
        break;
    }
    case Comment::VerbatimLineCommentKind:
    {
        auto CK = cast<clang::comments::VerbatimLineComment>(C);
        auto VL = new VerbatimLineComment();
        _Comment = VL;
        VL->Text = CK->getText();
        break;
    }
    case Comment::ParagraphCommentKind:
    {
        auto CK = cast<clang::comments::ParagraphComment>(C);
        auto PC = new ParagraphComment();
        _Comment = PC;
        for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
        {
            auto Content = ConvertCommentBlock(*I);
            PC->Content.push_back(static_cast<InlineContentComment*>(Content));
        }
        PC->IsWhitespace = CK->isWhitespace();
        break;
    }
    case Comment::HTMLStartTagCommentKind:
    {
        auto CK = cast<clang::comments::HTMLStartTagComment>(C);
        auto TC = new HTMLStartTagComment();
        _Comment = TC;
        TC->TagName = CK->getTagName();
        for (unsigned I = 0, E = CK->getNumAttrs(); I != E; ++I)
        {
            auto A = CK->getAttr(I);
            auto Attr = HTMLStartTagComment::Attribute();
            Attr.Name = A.Name;
            Attr.Value = A.Value;
            TC->Attributes.push_back(Attr);
        }
        break;
    }
    case Comment::HTMLEndTagCommentKind:
    {
        auto CK = cast<clang::comments::HTMLEndTagComment>(C);
        auto TC = new HTMLEndTagComment();
        _Comment = TC;
        TC->TagName = CK->getTagName();
        break;
    }
    case Comment::TextCommentKind:
    {
        auto CK = cast<clang::comments::TextComment>(C);
        auto TC = new TextComment();
        _Comment = TC;
        TC->Text = CK->getText();
        break;
    }
    case Comment::InlineCommandCommentKind:
    {
        auto CK = cast<clang::comments::InlineCommandComment>(C);
        auto IC = new InlineCommandComment();
        _Comment = IC;
        IC->CommentRenderKind = ConvertRenderKind(CK->getRenderKind());
        for (unsigned I = 0, E = CK->getNumArgs(); I != E; ++I)
        {
            auto Arg = InlineCommandComment::Argument();
            Arg.Text = CK->getArgText(I);
            IC->Arguments.push_back(Arg);
        }       
        break;
    }
    case Comment::VerbatimBlockLineCommentKind:
    {
        auto CK = cast<clang::comments::VerbatimBlockLineComment>(C);
        auto VL = new VerbatimBlockLineComment();
        _Comment = VL;
        VL->Text = CK->getText();
        break;
    }
    case Comment::NoCommentKind: return nullptr;
    default:
        llvm_unreachable("Unknown comment kind");
    }

    assert(_Comment && "Invalid comment instance");
    return _Comment;
}

void Parser::HandleComments(clang::Decl* D, Declaration* Decl)
{
    using namespace clang;

    const clang::RawComment* RC = 0;
    if (!(RC = AST->getRawCommentForAnyRedecl(D)))
        return;

    auto RawComment = WalkRawComment(RC);
    Decl->Comment = RawComment;

    if (clang::comments::FullComment* FC = RC->parse(*AST, &C->getPreprocessor(), D))
    {
        auto CB = static_cast<FullComment*>(ConvertCommentBlock(FC));
        RawComment->FullCommentBlock = CB;
    }
}
