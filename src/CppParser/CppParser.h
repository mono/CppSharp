/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include "AST.h"

#define VECTOR_OPTIONS(type, name) \
    std::vector<type> name; \
    void push##name##(const type& elem) { name.push_back(elem); }

namespace CppSharp { namespace CppParser {

using namespace CppSharp::CppParser::AST;

struct CS_API ParserOptions
{
    ParserOptions()
        : ASTContext(0)
        , ToolSetToUse(0)
        , Abi(CppAbi::Itanium)
        , NoStandardIncludes(false)
        , NoBuiltinIncludes(false)
        , MicrosoftMode(false)
        , Verbose(false)
    {
    }

    // C/C++ header file name.
    std::string FileName;

    // Include directories
    VECTOR_OPTIONS(std::string, IncludeDirs)
    VECTOR_OPTIONS(std::string, SystemIncludeDirs)
    VECTOR_OPTIONS(std::string, Defines)
    VECTOR_OPTIONS(std::string, LibraryDirs)

    CppSharp::CppParser::AST::ASTContext* ASTContext;

    int ToolSetToUse;
    std::string TargetTriple;
    CppAbi Abi;

    bool NoStandardIncludes;
    bool NoBuiltinIncludes;
    bool MicrosoftMode;
    bool Verbose;
};

enum struct ParserDiagnosticLevel
{
    Ignored,
    Note,
    Warning,
    Error,
    Fatal
};

struct CS_API ParserDiagnostic
{
    std::string FileName;
    std::string Message;
    ParserDiagnosticLevel Level;
    int LineNumber;
    int ColumnNumber;
};

enum struct ParserResultKind
{
    Success,
    Error,
    FileNotFound
};

struct CS_API ParserResult
{
    ParserResultKind Kind;
    std::vector<ParserDiagnostic> Diagnostics;

    CppSharp::CppParser::AST::ASTContext* ASTContext;
    CppSharp::CppParser::AST::NativeLibrary* Library;
};

enum class SourceLocationKind
{
    Invalid,
    Builtin,
    CommandLine,
    System,
    User
};

class CS_API ClangParser
{
public:

    static ParserResult* ParseHeader(ParserOptions* Opts);
    static ParserResult* ParseLibrary(ParserOptions* Opts);
};

} }