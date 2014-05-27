/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include "Helpers.h"

namespace CppSharp { namespace CppParser {

struct CS_API CS_VALUE_TYPE SourceLocation
{
    SourceLocation();
    SourceLocation(unsigned ID);
    unsigned ID;
};

} }
