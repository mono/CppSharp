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

std::string DLL_API MultipleConstantArraysParamsTestMethod(char arr1[9], char arr2[10]);

struct DLL_API PointerToTypedefPointerTest
{
    int val;
};
typedef PointerToTypedefPointerTest *LPPointerToTypedefPointerTest;

void DLL_API PointerToTypedefPointerTestMethod(LPPointerToTypedefPointerTest* lp, int valToSet);

typedef long *LPLONG;

void DLL_API PointerToPrimitiveTypedefPointerTestMethod(LPLONG lp, long valToSet);

union DLL_API UnionNestedInsideStruct
{
    char szText[10];
};

struct DLL_API StructWithNestedUnion
{
    UnionNestedInsideStruct nestedUnion;
};

std::string DLL_API StructWithNestedUnionTestMethod(StructWithNestedUnion val);

struct DLL_API StructNestedInsideUnion
{
    char szText[10];
};

union DLL_API UnionWithNestedStruct
{
    StructNestedInsideUnion nestedStruct;
};

std::string DLL_API UnionWithNestedStructTestMethod(UnionWithNestedStruct val);

union DLL_API UnionWithNestedStructArray
{
    StructNestedInsideUnion nestedStructs[2];
};

std::string DLL_API UnionWithNestedStructArrayTestMethod(UnionWithNestedStructArray val);

class DLL_API VectorPointerGetter
{
public:
    VectorPointerGetter();
    ~VectorPointerGetter();

    std::vector<std::string>* GetVecPtr();

private:
    std::vector<std::string>* vecPtr;
};