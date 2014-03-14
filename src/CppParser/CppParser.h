/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include "AST.h"
#include "Helpers.h"
#include "Target.h"

namespace CppSharp { namespace CppParser {

using namespace CppSharp::CppParser::AST;

struct CS_API ParserOptions
{
    ParserOptions();

    VECTOR_STRING(Arguments)

    // C/C++ header file name.
    STRING(FileName)

    // Include directories
    VECTOR_STRING(IncludeDirs)
    VECTOR_STRING(SystemIncludeDirs)
    VECTOR_STRING(Defines)
    VECTOR_STRING(LibraryDirs)

    CppSharp::CppParser::AST::ASTContext* ASTContext;

    int ToolSetToUse;
    STRING(TargetTriple)
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
    STRING(FileName)
    STRING(Message)
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
    ParserResult();

    ParserResultKind Kind;
    VECTOR(ParserDiagnostic, Diagnostics)

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
    static ParserTargetInfo*  GetTargetInfo(ParserOptions* Opts);

};

} }