#include "../Tests.h"

// Tests for C++ types
struct DLL_API Types
{
    // AttributedType
#ifdef __clang__
#define ATTR __attribute__((stdcall))
#else
#define ATTR
#endif

    // Note: This fails with C# currently due to mangling bugs.
    // Move it back once it's fixed upstream.
    typedef int AttributedFuncType(int, int) ATTR;
    AttributedFuncType AttributedSum;
};
