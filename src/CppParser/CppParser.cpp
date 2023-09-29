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
    , clangVersion(CLANG_VERSION_STRING)
{
}

CppParserOptions::~CppParserOptions() {}

std::string CppParserOptions::getClangVersion() { return clangVersion; }

DEF_VECTOR_STRING(CppParserOptions, Arguments)
DEF_VECTOR_STRING(CppParserOptions, CompilationOptions)
DEF_VECTOR_STRING(CppParserOptions, SourceFiles)
DEF_VECTOR_STRING(CppParserOptions, IncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, SystemIncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, Defines)
DEF_VECTOR_STRING(CppParserOptions, Undefines)
DEF_VECTOR_STRING(CppParserOptions, SupportedStdTypes)
DEF_VECTOR_STRING(CppParserOptions, SupportedFunctionTemplates)

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
    : fileName(rhs.fileName)
    , message(rhs.message)
    , level(rhs.level)
    , lineNumber(rhs.lineNumber)
    , columnNumber(rhs.columnNumber)
{}

ParserDiagnostic::~ParserDiagnostic() {}

} }