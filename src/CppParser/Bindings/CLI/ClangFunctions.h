#pragma once

#include "AST.h"
#include "CppSharp.h"
#include <CppParser.h>

using namespace CppSharp::Parser::AST;

namespace CppSharp
{
    public ref class ClangFunctions
    {
    public:
        static int getBaseClassOffset(ClassLayout^ Layout, Class^ Base);
    };
}
