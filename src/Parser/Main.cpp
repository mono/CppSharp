/************************************************************************
*
* Flush3D <http://www.flush3d.com> © (2008-201x) 
* Licensed under the LGPL 2.1 (GNU Lesser General Public License)
*
************************************************************************/

#include "Parser.h"
#include "Interop.h"

public ref class ClangParser
{
public:
    
    static bool Parse(ParserOptions^ Opts)
    {
        if (!Opts->FileName) return false;

        using namespace clix;
        std::string File = marshalString<E_UTF8>(Opts->FileName);

        Parser p(Opts);
        return p.Parse(File);
    }
};