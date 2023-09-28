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

#define CS_INTERNAL
#define CS_READONLY

namespace CppSharp { namespace CppParser {

using namespace CppSharp::CppParser::AST;

struct CS_API CppParserOptions
{
    CppParserOptions();
    ~CppParserOptions();

    std::string getClangVersion();

    VECTOR_STRING(Arguments)
    VECTOR_STRING(CompilationOptions)
    // C/C++ header file names.
    VECTOR_STRING(SourceFiles)

    // Include directories
    VECTOR_STRING(IncludeDirs)
    VECTOR_STRING(SystemIncludeDirs)
    VECTOR_STRING(Defines)
    VECTOR_STRING(Undefines)
    VECTOR_STRING(SupportedStdTypes)
    VECTOR_STRING(SupportedFunctionTemplates)

    CppSharp::CppParser::AST::ASTContext* ASTContext;

    int toolSetToUse;
    std::string targetTriple;

    bool noStandardIncludes;
    bool noBuiltinIncludes;
    bool microsoftMode;
    bool verbose;
    bool unityBuild;
    bool skipPrivateDeclarations;
    bool skipLayoutInfo;
    bool skipFunctionBodies;

private:
    std::string clangVersion;
};

struct CS_API CppLinkerOptions
{
    CppLinkerOptions();
    ~CppLinkerOptions();

    VECTOR_STRING(Arguments)
    VECTOR_STRING(LibraryDirs)
    VECTOR_STRING(Libraries)
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
    ~ParserDiagnostic();
    std::string fileName;
    std::string message;
    ParserDiagnosticLevel level { ParserDiagnosticLevel::Ignored };
    int lineNumber {0};
    int columnNumber {0};
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
    VECTOR(NativeLibrary*, Libraries)
    ParserTargetInfo* targetInfo;
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
    static ParserResult* ParseLibrary(CppLinkerOptions* Opts);
    static ParserResult* Build(CppParserOptions* Opts,
        const CppLinkerOptions* LinkerOptions, const std::string& File, bool Last);
    static ParserResult* Compile(CppParserOptions* Opts, const std::string& File);
    static ParserResult* Link(CppParserOptions* Opts,
        const CppLinkerOptions* LinkerOptions, const std::string& File, bool Last);
};

} }