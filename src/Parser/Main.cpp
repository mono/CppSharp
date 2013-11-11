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

    static ParserResult^ ReadAST(ParserOptions^ Opts)
    {
        if (!Opts->FileName) return nullptr;

        using namespace clix;
        std::string File = marshalString<E_UTF8>(Opts->FileName);

        ::Parser parser(Opts);
        return parser.ParseHeader(File);
    }

    static bool WriteAST(ParserOptions^ Opts)
    {
        if (!Opts->FileName) return false;

        using namespace clix;
        std::string File = marshalString<E_UTF8>(Opts->FileName);

        return true;
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