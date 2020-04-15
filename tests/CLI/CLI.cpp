#include "CLI.h"

int Types::AttributedSum(int A, int B)
{
    return A + B;
}

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

void DLL_API PointerToTypedefPointerTestMethod(LPPointerToTypedefPointerTest* lp, int valToSet)
{
    (*(*lp)).val = valToSet;
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