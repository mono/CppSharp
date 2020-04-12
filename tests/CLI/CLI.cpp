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
