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

struct CS_API CppParserOptions
{
    CppParserOptions();
    ~CppParserOptions();

    VECTOR_STRING(Arguments)
    std::string libraryFile;
    // C/C++ header file names.
    VECTOR_STRING(SourceFiles)

    // Include directories
    VECTOR_STRING(IncludeDirs)
    VECTOR_STRING(SystemIncludeDirs)
    VECTOR_STRING(Defines)
    VECTOR_STRING(Undefines)
    VECTOR_STRING(LibraryDirs)

    CppSharp::CppParser::AST::ASTContext* ASTContext;

    int toolSetToUse;
    std::string targetTriple;
    std::string currentDir;
    CppAbi abi;

    bool noStandardIncludes;
    bool noBuiltinIncludes;
    bool microsoftMode;
    bool verbose;

    ParserTargetInfo* targetInfo;
};

enum class ParserDiagnosticLevel
{
    Ignored,
    Note,
    Warning,
    Error,
    Fatal
};

struct CS_API ParserDiagnostic
{
    ParserDiagnostic();
    ParserDiagnostic(const ParserDiagnostic&);
    std::string fileName;
    std::string message;
    ParserDiagnosticLevel level;
    int lineNumber;
    int columnNumber;
};

enum class ParserResultKind
{
    Success,
    Error,
    FileNotFound
};

class Parser;

struct CS_API ParserResult
{
    ParserResult();
    ParserResult(const ParserResult&);
    ~ParserResult();

    ParserResultKind kind;
    VECTOR(ParserDiagnostic, Diagnostics)

    CppSharp::CppParser::AST::ASTContext* ASTContext;
    NativeLibrary* library;
    Parser* codeParser;
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

    static ParserResult* ParseHeader(CppParserOptions* Opts);
    static ParserResult* ParseLibrary(CppParserOptions* Opts);
    static ParserTargetInfo* GetTargetInfo(CppParserOptions* Opts);
};

} }