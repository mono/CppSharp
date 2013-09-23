/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include "AST.h"

namespace CppSharp { namespace CppParser {

struct CS_API ParserOptions
{
    ParserOptions()
    {
        MicrosoftMode = false;
        NoStandardIncludes = false;
        NoBuiltinIncludes = false;
    }

    // Include directories
    std::vector<std::string> IncludeDirs;
    std::vector<std::string> SystemIncludeDirs;
    std::vector<std::string> Defines;
    std::vector<std::string> LibraryDirs;

    // C/C++ header file name.
    std::string FileName;

    Library* Library;

    int ToolSetToUse;
    std::string TargetTriple;

    bool NoStandardIncludes;
    bool NoBuiltinIncludes;
    bool MicrosoftMode;
    CppAbi Abi;

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
    Library* Library;
    std::vector<ParserDiagnostic> Diagnostics;
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