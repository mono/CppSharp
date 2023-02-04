#include "CLI.h"

int Types::AttributedSum(int A, int B)
{
    return A + B;
}

TestProtectedDestructors::~TestProtectedDestructors() {}

std::string Date::testStdString(std::string s)
{
    return s + "_test";
}

void testFreeFunction()
{

}

struct IncompleteStruct {};

IncompleteStruct* createIncompleteStruct()
{
    return new IncompleteStruct();
}

DLL_API void useIncompleteStruct(IncompleteStruct * a)
{
    return;
}

TestMappedTypeNonConstRefParam::TestMappedTypeNonConstRefParam(const std::string str)
{
    m_str = str;
}

const TestMappedTypeNonConstRefParam & TestMappedTypeNonConstRefParam::operator=(const std::string str)
{
    m_str = str;

    return *this;
}

void TestMappedTypeNonConstRefParamConsumer::ChangePassedMappedTypeNonConstRefParam(TestMappedTypeNonConstRefParam & v)
{
    v = "ChangePassedMappedTypeNonConstRefParam";
}

VectorPointerGetter::VectorPointerGetter()
{
    vecPtr = new std::vector<std::string>();
    vecPtr->push_back("VectorPointerGetter");
}

VectorPointerGetter::~VectorPointerGetter()
{
    if (vecPtr)
    {
        auto tempVec = vecPtr;
        delete vecPtr;
        tempVec = nullptr;
    }
}

std::vector<std::string>* VectorPointerGetter::GetVecPtr()
{
    return vecPtr;
}

std::string DLL_API MultipleConstantArraysParamsTestMethod(char arr1[9], char arr2[10])
{
    return std::string(arr1, arr1 + 9) + std::string(arr2, arr2 + 10);
}

std::string DLL_API StructWithNestedUnionTestMethod(StructWithNestedUnion val)
{
    return std::string(val.nestedUnion.szText, val.nestedUnion.szText + 10);
}

std::string DLL_API UnionWithNestedStructTestMethod(UnionWithNestedStruct val)
{
    return std::string(val.nestedStruct.szText, val.nestedStruct.szText + 10);
}

std::string DLL_API UnionWithNestedStructArrayTestMethod(UnionWithNestedStructArray arr)
{
    return std::string(arr.nestedStructs[0].szText, arr.nestedStructs[0].szText + 10)
        + std::string(arr.nestedStructs[1].szText, arr.nestedStructs[1].szText + 10);
}