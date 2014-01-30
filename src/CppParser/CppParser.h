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
    type get##name (unsigned i) { return name[i]; } \
    void add##name (const type& s) { return name.push_back(s); } \
    unsigned get##name##Count () { return name.size(); }

#define VECTOR_STRING_OPTIONS(name) \
    std::vector<std::string> name; \
    const char* get##name (unsigned i) { return name[i].c_str(); } \
    void add##name (const char* s) { return name.push_back(std::string(s)); } \
    unsigned get##name##Count () { return name.size(); }

#define STRING_OPTIONS(name) \
    std::string name; \
    const char* get##name() { return name.c_str(); } \
    void set##name(const char* s) { name = s; }

namespace CppSharp { namespace CppParser {

using namespace CppSharp::CppParser::AST;

struct CS_API ParserOptions
{
    ParserOptions();

    // C/C++ header file name.
    STRING_OPTIONS(FileName)

    // Include directories
    VECTOR_STRING_OPTIONS(IncludeDirs)
    VECTOR_STRING_OPTIONS(SystemIncludeDirs)
    VECTOR_STRING_OPTIONS(Defines)
    VECTOR_STRING_OPTIONS(LibraryDirs)

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
    VECTOR_OPTIONS(ParserDiagnostic, Diagnostics)

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