/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#pragma once

#include "Helpers.h"
#include "Sources.h"
#include "Types.h"
#include "Decl.h"
#include "Stmt.h"
#include "Expr.h"
#include <algorithm>

namespace CppSharp { namespace CppParser { namespace AST {

#pragma region Libraries

enum class ArchType
{
    UnknownArch,
    x86,
    x86_64
};

class CS_API NativeLibrary
{
public:
    NativeLibrary();
    ~NativeLibrary();
    std::string fileName;
    ArchType archType;
    VECTOR_STRING(Symbols)
    VECTOR_STRING(Dependencies)
};

#pragma endregion

#pragma region Comments

enum struct CommentKind
{
    FullComment,
    BlockContentComment,
    BlockCommandComment,
    ParamCommandComment,
    TParamCommandComment,
    VerbatimBlockComment,
    VerbatimLineComment,
    ParagraphComment,
    HTMLTagComment,
    HTMLStartTagComment,
    HTMLEndTagComment,
    TextComment,
    InlineContentComment,
    InlineCommandComment,
    VerbatimBlockLineComment
};

class CS_API CS_ABSTRACT Comment
{
public:
    Comment(CommentKind kind);
    CommentKind kind;
};

class CS_API BlockContentComment : public Comment
{
public:
    BlockContentComment();
    BlockContentComment(CommentKind Kind);
};

class CS_API FullComment : public Comment
{
public:
    FullComment();
    ~FullComment();
    VECTOR(BlockContentComment*, Blocks)
};

class CS_API InlineContentComment : public Comment
{
public:
    InlineContentComment();
    InlineContentComment(CommentKind Kind);
    bool hasTrailingNewline;
};

class CS_API ParagraphComment : public BlockContentComment
{
public:
    ParagraphComment();
    ~ParagraphComment();
    bool isWhitespace;
    VECTOR(InlineContentComment*, Content)
};

class CS_API BlockCommandComment : public BlockContentComment
{
public:
    class CS_API Argument
    {
    public:
        Argument();
        Argument(const Argument&);
        std::string text;
    };
    BlockCommandComment();
    BlockCommandComment(CommentKind Kind);
    ~BlockCommandComment();
    unsigned commandId;
    ParagraphComment* paragraphComment;
    VECTOR(Argument, Arguments)
};

class CS_API ParamCommandComment : public BlockCommandComment
{
public:
    enum PassDirection
    {
        In,
        Out,
        InOut
    };
    ParamCommandComment();
    PassDirection direction;
    unsigned paramIndex;
};

class CS_API TParamCommandComment : public BlockCommandComment
{
public:
    TParamCommandComment();
    VECTOR(unsigned, Position)
};

class CS_API VerbatimBlockLineComment : public Comment
{
public:
    VerbatimBlockLineComment();
    std::string text;
};

class CS_API VerbatimBlockComment : public BlockCommandComment
{
public:
    VerbatimBlockComment();
    ~VerbatimBlockComment();
    VECTOR(VerbatimBlockLineComment*, Lines)
};

class CS_API VerbatimLineComment : public BlockCommandComment
{
public:
    VerbatimLineComment();
    std::string text;
};

class CS_API InlineCommandComment : public InlineContentComment
{
public:
    enum RenderKind
    {
        RenderNormal,
        RenderBold,
        RenderMonospaced,
        RenderEmphasized,
        RenderAnchor
    };
    class CS_API Argument
    {
    public:
        Argument();
        Argument(const Argument&);
        std::string text;
    };
    InlineCommandComment();
    unsigned commandId;
    RenderKind commentRenderKind;
    VECTOR(Argument, Arguments)
};

class CS_API HTMLTagComment : public InlineContentComment
{
public:
    HTMLTagComment();
    HTMLTagComment(CommentKind Kind);
};

class CS_API HTMLStartTagComment : public HTMLTagComment
{
public:
    class CS_API Attribute
    {
    public:
        Attribute();
        Attribute(const Attribute&);
        std::string name;
        std::string value;
    };
    HTMLStartTagComment();
    std::string tagName;
    VECTOR(Attribute, Attributes)
};

class CS_API HTMLEndTagComment : public HTMLTagComment
{
public:
    HTMLEndTagComment();
    std::string tagName;
};

class CS_API TextComment : public InlineContentComment
{
public:
    TextComment();
    std::string text;
};

enum class RawCommentKind
{
    Invalid,
    OrdinaryBCPL,
    OrdinaryC,
    BCPLSlash,
    BCPLExcl,
    JavaDoc,
    Qt,
    Merged
};

class CS_API RawComment
{
public:
    RawComment();
    ~RawComment();
    RawCommentKind kind;
    std::string text;
    std::string briefText;
    FullComment* fullCommentBlock;
};

#pragma region Commands

#pragma endregion

#pragma endregion

} } }