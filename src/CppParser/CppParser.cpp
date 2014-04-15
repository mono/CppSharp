/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "CppParser.h"

namespace CppSharp { namespace CppParser {

ParserOptions::ParserOptions()
    : ASTContext(0)
    , ToolSetToUse(0)
    , Abi(CppAbi::Itanium)
    , NoStandardIncludes(false)
    , NoBuiltinIncludes(false)
    , MicrosoftMode(false)
    , Verbose(false)
{
}

DEF_VECTOR_STRING(ParserOptions, Arguments)
DEF_STRING(ParserOptions, FileName)
DEF_VECTOR_STRING(ParserOptions, IncludeDirs)
DEF_VECTOR_STRING(ParserOptions, SystemIncludeDirs)
DEF_VECTOR_STRING(ParserOptions, Defines)
DEF_VECTOR_STRING(ParserOptions, LibraryDirs)
DEF_STRING(ParserOptions, TargetTriple)
DEF_STRING(ParserTargetInfo, ABI)

ParserResult::ParserResult()
    : ASTContext(0)
    , Library(0)
{
}

ParserDiagnostic::ParserDiagnostic() {}

DEF_STRING(ParserDiagnostic, FileName)
DEF_STRING(ParserDiagnostic, Message)

DEF_VECTOR(ParserResult, ParserDiagnostic, Diagnostics)



} }