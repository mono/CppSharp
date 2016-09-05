/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "CppParser.h"
#include "Parser.h"

namespace CppSharp { namespace CppParser {

CppParserOptions::CppParserOptions()
    : ASTContext(0)
    , ToolSetToUse(0)
    , Abi(CppAbi::Itanium)
    , NoStandardIncludes(false)
    , NoBuiltinIncludes(false)
    , MicrosoftMode(false)
    , Verbose(false)
    , LanguageVersion(CppParser::LanguageVersion::GNUPlusPlus11)
    , TargetInfo(0)
{
}

DEF_VECTOR_STRING(CppParserOptions, Arguments)
DEF_STRING(CppParserOptions, LibraryFile)
DEF_VECTOR_STRING(CppParserOptions, SourceFiles)
DEF_VECTOR_STRING(CppParserOptions, IncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, SystemIncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, Defines)
DEF_VECTOR_STRING(CppParserOptions, Undefines)
DEF_VECTOR_STRING(CppParserOptions, LibraryDirs)
DEF_STRING(CppParserOptions, TargetTriple)
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