#include "../Tests.h"

#include "UseTemplateTypeFromIgnoredClassTemplate/Employee.h"
#include "UseTemplateTypeFromIgnoredClassTemplate/EmployeeOrg.h"

#include "NestedEnumInClassTest/ClassWithNestedEnum.h"
#include "NestedEnumInClassTest/NestedEnumConsumer.h"

#include <ostream>
#include <vector>

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

struct CompleteIncompleteStruct;

typedef struct IncompleteStruct IncompleteStruct;

DLL_API IncompleteStruct* createIncompleteStruct();
DLL_API void useIncompleteStruct(IncompleteStruct* a);

class DLL_API TestMappedTypeNonConstRefParam
{
public:
    TestMappedTypeNonConstRefParam(const std::string);
    const TestMappedTypeNonConstRefParam& operator=(const std::string);

    std::string m_str;
};

class DLL_API TestMappedTypeNonConstRefParamConsumer
{
public:
    void ChangePassedMappedTypeNonConstRefParam(TestMappedTypeNonConstRefParam&);
};

class DLL_API VectorPointerGetter
{
public:
    VectorPointerGetter();
    ~VectorPointerGetter();

    std::vector<std::string>* GetVecPtr();

private:
    std::vector<std::string>* vecPtr;
};

// Previously passing multiple constant arrays was generating the same variable name for each array inside the method body.
// This is fixed by using the same generation code in CLIMarshal.VisitArrayType for both when there is a return var name specified and
// for when no return var name is specified.
std::string DLL_API MultipleConstantArraysParamsTestMethod(char arr1[9], char arr2[10]);

// Ensures marshalling arrays is handled correctly for value types used within reference types.
union DLL_API UnionNestedInsideStruct
{
    char szText[10];
};

struct DLL_API StructWithNestedUnion
{
    UnionNestedInsideStruct nestedUnion;
};

std::string DLL_API StructWithNestedUnionTestMethod(StructWithNestedUnion val);

// Ensures marshalling arrays is handled correctly for reference types used within value types.
struct DLL_API StructNestedInsideUnion
{
    char szText[10];
};

union DLL_API UnionWithNestedStruct
{
    StructNestedInsideUnion nestedStruct;
};

std::string DLL_API UnionWithNestedStructTestMethod(UnionWithNestedStruct val);

// Ensures marshalling arrays is handled corectly for arrays of reference types used within value types.
union DLL_API UnionWithNestedStructArray
{
    StructNestedInsideUnion nestedStructs[2];
};

std::string DLL_API UnionWithNestedStructArrayTestMethod(UnionWithNestedStructArray val);