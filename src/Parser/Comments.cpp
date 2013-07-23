/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "Parser.h"
#include "Interop.h"

#include <clang/AST/ASTContext.h>

//-----------------------------------//

static CppSharp::AST::RawCommentKind
ConvertCommentKind(clang::RawComment::CommentKind Kind)
{
	using clang::RawComment;
	using namespace CppSharp::AST;

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

CppSharp::AST::RawComment^ Parser::WalkRawComment(const clang::RawComment* RC)
{
	using namespace clang;
    using namespace clix;

	auto &SM = C->getSourceManager();
	auto Comment = gcnew CppSharp::AST::RawComment();
	Comment->Kind = ConvertCommentKind(RC->getKind());
	Comment->Text = marshalString<E_UTF8>(RC->getRawText(SM));
	Comment->BriefText = marshalString<E_UTF8>(RC->getBriefText(*AST));

	return Comment;
}

static CppSharp::AST::InlineCommandComment::RenderKind
ConvertRenderKind(clang::comments::InlineCommandComment::RenderKind Kind)
{
	using namespace clang::comments;
	switch(Kind)
	{
	case InlineCommandComment::RenderNormal:
		return CppSharp::AST::InlineCommandComment::RenderKind::RenderNormal;
	case InlineCommandComment::RenderBold:
		return CppSharp::AST::InlineCommandComment::RenderKind::RenderBold;
	case InlineCommandComment::RenderMonospaced:
		return CppSharp::AST::InlineCommandComment::RenderKind::RenderMonospaced;
	case InlineCommandComment::RenderEmphasized:
		return CppSharp::AST::InlineCommandComment::RenderKind::RenderEmphasized;
	}
	llvm_unreachable("Unknown render kind");
}

static CppSharp::AST::ParamCommandComment::PassDirection
ConvertParamPassDirection(clang::comments::ParamCommandComment::PassDirection Dir)
{
	using namespace clang::comments;
	switch(Dir)
	{
	case ParamCommandComment::In:
		return CppSharp::AST::ParamCommandComment::PassDirection::In;
	case ParamCommandComment::Out:
		return CppSharp::AST::ParamCommandComment::PassDirection::Out;
	case ParamCommandComment::InOut:
		return CppSharp::AST::ParamCommandComment::PassDirection::InOut;
	}
	llvm_unreachable("Unknown parameter pass direction");
}

static void HandleBlockCommand(const clang::comments::BlockCommandComment *CK,
							   CppSharp::AST::BlockCommandComment^ BC)
{
	using namespace clix;

	BC->CommandId = CK->getCommandID();
	for (unsigned I = 0, E = CK->getNumArgs(); I != E; ++I)
	{
		auto Arg = CppSharp::AST::BlockCommandComment::Argument();
		Arg.Text = marshalString<E_UTF8>(CK->getArgText(I));
		BC->Arguments->Add(Arg);
	}
}

static CppSharp::AST::Comment^ ConvertCommentBlock(clang::comments::Comment* C)
{
    using namespace clang;
	using clang::comments::Comment;

    using namespace clix;
	using namespace CppSharp::AST;

	// This needs to have an underscore else we get an ICE under VS2012.
	CppSharp::AST::Comment^ _Comment;

	switch(C->getCommentKind())
	{
	case Comment::BlockCommandCommentKind:
	{
		auto CK = cast<const clang::comments::BlockCommandComment>(C);
		auto BC = gcnew BlockCommandComment();
		_Comment = BC;
		HandleBlockCommand(CK, BC);
		break;
	}
	case Comment::ParamCommandCommentKind:
	{
		auto CK = cast<clang::comments::ParamCommandComment>(C);
		auto PC = gcnew ParamCommandComment();
		_Comment = PC;
		HandleBlockCommand(CK, PC);
		PC->Direction = ConvertParamPassDirection(CK->getDirection());
		PC->ParamIndex = CK->getParamIndex();
		break;
	}
	case Comment::TParamCommandCommentKind:
	{
		auto CK = cast<clang::comments::TParamCommandComment>(C);
		_Comment = gcnew TParamCommandComment();
		auto TC = gcnew TParamCommandComment();
		_Comment = TC;
		HandleBlockCommand(CK, TC);
		if (CK->isPositionValid())
			for (unsigned I = 0, E = CK->getDepth(); I != E; ++I)
				TC->Position->Add(CK->getIndex(I));
		break;
	}
	case Comment::VerbatimBlockCommentKind:
	{
		auto CK = cast<clang::comments::VerbatimBlockComment>(C);
		auto VB = gcnew VerbatimBlockComment();
		_Comment = VB;
		for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
		{
			auto Line = ConvertCommentBlock(*I);
			VB->Lines->Add(dynamic_cast<VerbatimBlockLineComment^>(Line));
		}
		break;
	}
	case Comment::VerbatimLineCommentKind:
	{
		auto CK = cast<clang::comments::VerbatimLineComment>(C);
		auto VL = gcnew VerbatimLineComment();
		_Comment = VL;
		VL->Text = marshalString<E_UTF8>(CK->getText());
		break;
	}
	case Comment::ParagraphCommentKind:
	{
		auto CK = cast<clang::comments::ParagraphComment>(C);
		auto PC = gcnew ParagraphComment();
		_Comment = PC;
		for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
		{
			auto Content = ConvertCommentBlock(*I);
			PC->Content->Add(dynamic_cast<InlineContentComment^>(Content));
		}
		PC->IsWhitespace = CK->isWhitespace();
		break;
	}
	case Comment::FullCommentKind:
	{
		auto CK = cast<clang::comments::FullComment>(C);
		auto FC = gcnew FullComment();
		_Comment = FC;
		for (auto I = CK->child_begin(), E = CK->child_end(); I != E; ++I)
		{
			auto Content = ConvertCommentBlock(*I);
			FC->Blocks->Add(dynamic_cast<BlockContentComment^>(Content));
		}
		break;
	}
	case Comment::HTMLStartTagCommentKind:
	{
		auto CK = cast<clang::comments::HTMLStartTagComment>(C);
		auto TC = gcnew HTMLStartTagComment();
		_Comment = TC;
		TC->TagName = marshalString<E_UTF8>(CK->getTagName());
		for (unsigned I = 0, E = CK->getNumAttrs(); I != E; ++E)
		{
			auto A = CK->getAttr(I);
			auto Attr = CppSharp::AST::HTMLStartTagComment::Attribute();
			Attr.Name = marshalString<E_UTF8>(A.Name);
			Attr.Value = marshalString<E_UTF8>(A.Value);
			TC->Attributes->Add(Attr);
		}
		break;
	}
	case Comment::HTMLEndTagCommentKind:
	{
		auto CK = cast<clang::comments::HTMLEndTagComment>(C);
		auto TC = gcnew HTMLEndTagComment();
		_Comment = TC;
		TC->TagName = marshalString<E_UTF8>(CK->getTagName());
		break;
	}
	case Comment::TextCommentKind:
	{
		auto CK = cast<clang::comments::TextComment>(C);
		auto TC = gcnew TextComment();
		_Comment = TC;
		TC->Text = marshalString<E_UTF8>(CK->getText());
		break;
	}
	case Comment::InlineCommandCommentKind:
	{
		auto CK = cast<clang::comments::InlineCommandComment>(C);
		auto IC = gcnew InlineCommandComment();
		_Comment = IC;
		IC->Kind = ConvertRenderKind(CK->getRenderKind());
		for (unsigned I = 0, E = CK->getNumArgs(); I != E; ++I)
		{
			auto Arg = CppSharp::AST::InlineCommandComment::Argument();
			Arg.Text = marshalString<E_UTF8>(CK->getArgText(I));
			IC->Arguments->Add(Arg);
		}		
		break;
	}
	case Comment::VerbatimBlockLineCommentKind:
	{
		auto CK = cast<clang::comments::VerbatimBlockLineComment>(C);
		auto VL = gcnew VerbatimBlockLineComment();
		_Comment = VL;
		VL->Text = marshalString<E_UTF8>(CK->getText());
		break;
	}
	case Comment::NoCommentKind: return nullptr;
	default:
		llvm_unreachable("Unknown comment kind");
	}

	assert(_Comment && "Invalid comment instance");
	return _Comment;
}

void Parser::HandleComments(clang::Decl* D, CppSharp::AST::Declaration^ Decl)
{
    using namespace clang;
	using namespace clang::comments;
    using namespace clix;

    const RawComment* RC = 0;
    if (!(RC = AST->getRawCommentForAnyRedecl(D)))
		return;

	auto RawComment = WalkRawComment(RC);
	Decl->Comment = RawComment;

	if (FullComment* FC = RC->parse(*AST, &C->getPreprocessor(), D))
	{
		auto CB = safe_cast<CppSharp::AST::FullComment^>(ConvertCommentBlock(FC));
		RawComment->FullComment = CB;
	}

	// Debug Text
    SourceManager& SM = C->getSourceManager();
    const LangOptions& LangOpts = C->getLangOpts();

    auto Range = CharSourceRange::getTokenRange(D->getSourceRange());

    bool Invalid;
    StringRef DeclText = Lexer::getSourceText(Range, SM, LangOpts, &Invalid);
    //assert(!Invalid && "Should have a valid location");
    
    if (!Invalid)
        Decl->DebugText = marshalString<E_UTF8>(DeclText);
}
