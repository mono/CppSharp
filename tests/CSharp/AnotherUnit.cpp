#include "AnotherUnit.h"

void SecondaryBase::VirtualMember()
{
}

int SecondaryBase::property()
{
    return 0;
}

void SecondaryBase::setProperty(int value)
{
}

void SecondaryBase::function(bool* ok)
{
}

void SecondaryBase::protectedFunction()
{
}

int SecondaryBase::protectedProperty()
{
    return 0;
}

void SecondaryBase::setProtectedProperty(int value)
{
}

void functionInAnotherUnit()
{
}

MultipleInheritance::MultipleInheritance()
{
}

MultipleInheritance::~MultipleInheritance()
{
}

namespace HasFreeConstant
{
    extern const int DLL_API FREE_CONSTANT_IN_NAMESPACE = 5;
    extern const std::string DLL_API STD_STRING_CONSTANT = "test";
}
