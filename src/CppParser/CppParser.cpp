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

ParserResult::ParserResult()
    : ASTContext(0)
    , Library(0)
{
}

} }