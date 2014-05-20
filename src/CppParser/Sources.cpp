#include "Sources.h"

namespace CppSharp { namespace CppParser {

SourceLocation::SourceLocation()
    : ID(0)
{
}

SourceLocation::SourceLocation(unsigned ID)
    : ID(ID)
{
}

} }
