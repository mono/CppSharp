#include "Classes2.h"

class Class
{
public:
    void ReturnsVoid() {}
    int ReturnsInt() { return 0; }
    Class* PassAndReturnsClassPtr(Class* obj) { return obj; }
};

class ClassWithField
{
public:
    ClassWithField() : Field(10) {}
    int Field;
    int ReturnsField() { return Field; }
};

class ClassWithOverloads
{
public:
    ClassWithOverloads() {}
    ClassWithOverloads(int) {}
    void Overload() {}
    void Overload(int) {}
};

class ClassWithSingleInheritance : public Class
{
public:
    int ChildMethod() { return 2; }
};

class ClassWithExternalInheritance : public ClassFromAnotherUnit
{

};

void FunctionPassClassByRef(Class* klass) { }
Class* FunctionReturnsClassByRef() { return new Class(); }