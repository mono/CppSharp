#include "../Tests.h"
#include <string>

#pragma once

class DLL_API SecondaryBase
{
public:
    enum Property
    {
        P1,
        P2
    };
    enum Function
    {
        M1,
        M2
    };

    virtual void VirtualMember();
    int property();
    void setProperty(int value);
    void function(bool* ok = 0);
    typedef void HasPointerToEnum(Property* pointerToEnum);
    HasPointerToEnum* hasPointerToEnum;
protected:
    void protectedFunction();
    int protectedProperty();
    void setProtectedProperty(int value);
};

void DLL_API functionInAnotherUnit();

struct DLL_API ForwardDeclaredStruct;

struct DLL_API DuplicateDeclaredStruct;

template <typename T>
class DLL_API TemplateInAnotherUnit
{
    T field;
};

class ForwardInOtherUnitButSameModule
{
};

class DLL_API MultipleInheritance : ForwardInOtherUnitButSameModule, SecondaryBase
{
public:
    MultipleInheritance();
    ~MultipleInheritance();
};

namespace HasFreeConstant
{
    extern const int DLL_API FREE_CONSTANT_IN_NAMESPACE;
    extern const std::string DLL_API STD_STRING_CONSTANT;
}
