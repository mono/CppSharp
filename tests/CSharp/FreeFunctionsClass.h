#pragma once

#include "../Tests.h"

namespace FreeFunctions
{
    class DLL_API FreeFunctionsClass
    {
    public:
        FreeFunctionsClass();

        int GetInt();
    };
}

// The free function here will have a class generated for it. For CLI we must make the generated class name unique and different to 
// the existing FreeFunctionsClass. We append the word "Helpers" to the generated class name to make it unique.
// The generator uses the file name where free functions live to base the generated class name on. Because the file name
// already matches an existing class name (FreeFunctionsClass), we must make the generated class name for free functions unique.
// For C#, the generated classes are partial, so the conflicting name is not an issue.
namespace FreeFunctions
{
    int DLL_API f();
}