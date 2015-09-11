/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "CppParser.h"
#include "Parser.h"

namespace CppSharp { namespace CppParser {

ParserOptions::ParserOptions()
    : ASTContext(0)
    , ToolSetToUse(0)
    , Abi(CppAbi::Itanium)
    , NoStandardIncludes(false)
    , NoBuiltinIncludes(false)
    , MicrosoftMode(false)
    , Verbose(false)
    , LanguageVersion(CppParser::LanguageVersion::CPlusPlus11)
    , TargetInfo(0)
{
}

DEF_VECTOR_STRING(ParserOptions, Arguments)
DEF_STRING(ParserOptions, FileName)
DEF_VECTOR_STRING(ParserOptions, IncludeDirs)
DEF_VECTOR_STRING(ParserOptions, SystemIncludeDirs)
DEF_VECTOR_STRING(ParserOptions, Defines)
DEF_VECTOR_STRING(ParserOptions, Undefines)
DEF_VECTOR_STRING(ParserOptions, LibraryDirs)
DEF_STRING(ParserOptions, TargetTriple)
DEF_STRING(ParserTargetInfo, ABI)

ParserResult::ParserResult()
    : ASTContext(0)
    , Library(0)
    , CodeParser(0)
{
}

ParserResult::ParserResult(const ParserResult& rhs)
    : Kind(rhs.Kind)
    , Diagnostics(rhs.Diagnostics)
    , ASTContext(rhs.ASTContext)
    , Library(rhs.Library)
    , CodeParser(rhs.CodeParser)
{}

ParserResult::~ParserResult()
{
    delete CodeParser;
}

ParserDiagnostic::ParserDiagnostic() {}

ParserDiagnostic::ParserDiagnostic(const ParserDiagnostic& rhs)
    : FileName(rhs.FileName)
    , Message(rhs.Message)
    , Level(rhs.Level)
    , LineNumber(rhs.LineNumber)
    , ColumnNumber(rhs.ColumnNumber)
{}

DEF_STRING(ParserDiagnostic, FileName)
DEF_STRING(ParserDiagnostic, Message)

DEF_VECTOR(ParserResult, ParserDiagnostic, Diagnostics)

} }