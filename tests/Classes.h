#pragma once

#include "Classes2.h"

class Class
{
public:
    void ReturnsVoid() {}
    int ReturnsInt() const { return 0; }
    Class* PassAndReturnsClassPtr(Class* obj) { return obj; }
};

class ClassWithField
{
public:
    ClassWithField()
        : Field(10)
    {
    }
    int Field;
    int ReturnsField() const { return Field; }
};

class ClassWithProperty
{
public:
    ClassWithProperty()
        : Field(10)
    {
    }
    int GetField() const { return Field; }
    void SetField(int value) { Field = value; }

private:
    int Field;
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

// void FunctionPassClassByRef(Class* klass) { }
// Class* FunctionReturnsClassByRef() { return new Class(); }
