/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "Parser.h"
#include "Interop.h"

namespace CppSharp { namespace Parser {

public ref class ClangParser
{
public:

    static ParserTargetInfo^ GetTargetInfo(ParserOptions^ Opts)
    {
        ::Parser parser(Opts);
        return parser.GetTargetInfo();
    }

    static ParserResult^ ParseHeader(ParserOptions^ Opts)
    {
        if (!Opts->FileName) return nullptr;

        using namespace clix;
        std::string File = marshalString<E_UTF8>(Opts->FileName);

        ::Parser parser(Opts);
        return parser.ParseHeader(File);
    }

    static ParserResult^ ParseLibrary(ParserOptions^ Opts)
    {
        if (!Opts->FileName) return nullptr;

        using namespace clix;
        std::string File = marshalString<E_UTF8>(Opts->FileName);

        ::Parser parser(Opts);
        return parser.ParseLibrary(File);
    }
};

} }