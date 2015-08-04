#include "../Tests.h"

#include <ostream>

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

// Tests code generator to not generate a destructor/finalizer pair
// if the destructor of the C++ class is not public.
class DLL_API TestProtectedDestructors
{
    ~TestProtectedDestructors();
};

TestProtectedDestructors::~TestProtectedDestructors() {}

// Tests the insertion operator (<<) to ToString method pass
class DLL_API Date
{
public:
    Date(int m, int d, int y)
    {
        mo = m; da = d; yr = y;
    }
    // Not picked up by parser yet
    //friend std::ostream& operator<<(std::ostream& os, const Date& dt);
    int mo, da, yr;

    std::string testStdString(std::string s);
};

std::ostream& operator<<(std::ostream& os, const Date& dt)
{
    os << dt.mo << '/' << dt.da << '/' << dt.yr;
    return os;
}

DLL_API void testFreeFunction();