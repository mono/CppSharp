/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "CppParser.h"
#include "Parser.h"
#include <llvm/TargetParser/Host.h>
#include <clang/Basic/Version.inc>

namespace CppSharp { namespace CppParser {

CppParserOptions::CppParserOptions()
    : ASTContext(0)
    , toolSetToUse(0)
    , noStandardIncludes(false)
    , noBuiltinIncludes(false)
    , microsoftMode(false)
    , verbose(false)
    , unityBuild(false)
    , skipPrivateDeclarations(true)
    , skipLayoutInfo(false)
    , skipFunctionBodies(true)
{
}

CppParserOptions::~CppParserOptions() {}

const char * CppParserOptions::getClangVersion()
{
    return CLANG_VERSION_STRING;
}

DEF_VECTOR_STRING(CppParserOptions, Arguments)
DEF_VECTOR_STRING(CppParserOptions, CompilationOptions)
DEF_STRING(CppParserOptions, LibraryFile)
DEF_VECTOR_STRING(CppParserOptions, SourceFiles)
DEF_VECTOR_STRING(CppParserOptions, IncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, SystemIncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, Defines)
DEF_VECTOR_STRING(CppParserOptions, Undefines)
DEF_VECTOR_STRING(CppParserOptions, SupportedStdTypes)
DEF_VECTOR_STRING(CppParserOptions, SupportedFunctionTemplates)
DEF_STRING(CppParserOptions, TargetTriple)
DEF_STRING(ParserTargetInfo, ABI)

ParserResult::ParserResult()
    : targetInfo(0)
{
}

ParserResult::ParserResult(const ParserResult& rhs)
    : kind(rhs.kind)
    , Diagnostics(rhs.Diagnostics)
    , Libraries(rhs.Libraries)
    , targetInfo(rhs.targetInfo)
{}

ParserResult::~ParserResult()
{
    for (auto Library : Libraries)
    {
        delete Library;
    }
}

DEF_VECTOR(ParserResult, ParserDiagnostic, Diagnostics)
DEF_VECTOR(ParserResult, NativeLibrary*, Libraries)

CppLinkerOptions::CppLinkerOptions()
{
}

CppLinkerOptions::~CppLinkerOptions() {}

DEF_VECTOR_STRING(CppLinkerOptions, Arguments)
DEF_VECTOR_STRING(CppLinkerOptions, LibraryDirs)
DEF_VECTOR_STRING(CppLinkerOptions, Libraries)

ParserDiagnostic::ParserDiagnostic() {}

ParserDiagnostic::ParserDiagnostic(const ParserDiagnostic& rhs)
    : FileName(rhs.FileName)
    , Message(rhs.Message)
    , level(rhs.level)
    , lineNumber(rhs.lineNumber)
    , columnNumber(rhs.columnNumber)
{}

ParserDiagnostic::~ParserDiagnostic() {}

DEF_STRING(ParserDiagnostic, FileName)
DEF_STRING(ParserDiagnostic, Message)

} }