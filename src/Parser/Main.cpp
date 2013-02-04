/************************************************************************
*
* Cxxi
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "Parser.h"
#include "Interop.h"

public ref class ClangParser
{
public:
    
    static ParserResult^ Parse(ParserOptions^ Opts)
    {
        if (!Opts->FileName) return nullptr;

        using namespace clix;
        std::string File = marshalString<E_UTF8>(Opts->FileName);

        Parser p(Opts);
        return p.Parse(File);
    }
};