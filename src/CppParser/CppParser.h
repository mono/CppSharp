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

enum class LanguageVersion
{
    /**
    * The C programming language.
    */
    C,
    /**
    * The C programming language (GNU version).
    */
    GNUC,
    /**
    * The C++ programming language year 1998; supports deprecated constructs.
    */
    CPlusPlus98,
    /**
    * The C++ programming language year 1998; supports deprecated constructs (GNU version).
    */
    GNUPlusPlus98,
    /**
    * The C++ programming language year 2011.
    */
    CPlusPlus11,
    /**
    * The C++ programming language year 2011 (GNU version).
    */
    GNUPlusPlus11
};

struct CS_API CppParserOptions
{
    CppParserOptions();

    VECTOR_STRING(Arguments)

    STRING(LibraryFile)
    // C/C++ header file names.
    VECTOR_STRING(SourceFiles)

    // Include directories
    VECTOR_STRING(IncludeDirs)
    VECTOR_STRING(SystemIncludeDirs)
    VECTOR_STRING(Defines)
    VECTOR_STRING(Undefines)
    VECTOR_STRING(LibraryDirs)

    CppSharp::CppParser::AST::ASTContext* ASTContext;

    int ToolSetToUse;
    STRING(TargetTriple)
    CppAbi Abi;

    bool NoStandardIncludes;
    bool NoBuiltinIncludes;
    bool MicrosoftMode;
    bool Verbose;
    LanguageVersion LanguageVersion;

    ParserTargetInfo* TargetInfo;
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

    STRING(FileName)
    STRING(Message)
    ParserDiagnosticLevel Level;
    int LineNumber;
    int ColumnNumber;
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

    ParserResultKind Kind;
    VECTOR(ParserDiagnostic, Diagnostics)

    CppSharp::CppParser::AST::ASTContext* ASTContext;
    CppSharp::CppParser::AST::NativeLibrary* Library;
    Parser* CodeParser;
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