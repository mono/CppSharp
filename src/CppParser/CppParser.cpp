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
    , toolSetToUse(0)
    , abi(CppAbi::Itanium)
    , noStandardIncludes(false)
    , noBuiltinIncludes(false)
    , microsoftMode(false)
    , verbose(false)
    , languageVersion(CppParser::LanguageVersion::GNUPlusPlus11)
    , targetInfo(0)
{
}

CppParserOptions::~CppParserOptions() {}

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
    , library(0)
    , codeParser(0)
{
}

ParserResult::ParserResult(const ParserResult& rhs)
    : kind(rhs.kind)
    , Diagnostics(rhs.Diagnostics)
    , ASTContext(rhs.ASTContext)
    , library(rhs.library)
    , codeParser(rhs.codeParser)
{}

ParserResult::~ParserResult()
{
    delete codeParser;
}

ParserDiagnostic::ParserDiagnostic() {}

ParserDiagnostic::ParserDiagnostic(const ParserDiagnostic& rhs)
    : FileName(rhs.FileName)
    , Message(rhs.Message)
    , level(rhs.level)
    , lineNumber(rhs.lineNumber)
    , columnNumber(rhs.columnNumber)
{}

DEF_STRING(ParserDiagnostic, FileName)
DEF_STRING(ParserDiagnostic, Message)

DEF_VECTOR(ParserResult, ParserDiagnostic, Diagnostics)

} }