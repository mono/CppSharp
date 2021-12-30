#include "../Tests.h"


namespace OverlappingNamespace
{
    enum ColorsEnum {
        white,
        black,
        red,
        blue,
        green,
    };

    class DLL_API InBaseLib
    {
    };
}



class DLL_API Base
{
public:
    Base(int i);
    Base();
    int parent();

private:
    int b;
};

class DLL_API Base2 : public Base
{
public:
    Base2(int i);
    Base2();
    virtual void parent(int i);
};

class DLL_API Abstract
{
public:
    virtual void abstractFunction() = 0;
};

template <typename T>
class TemplateClass
{
public:
    T getField() const;
    void setField(const T& value);
private:
    union
    {
        int i;
        float f;
    };
    T t;
};

template <typename T>
T TemplateClass<T>::getField() const
{
    return t;
}

template <typename T>
void TemplateClass<T>::setField(const T& value)
{
    t = value;
}

template <typename T>
class IndependentFields
{
public:
    class Nested
    {
    private:
        T field;
    };
private:
    union
    {
        int i;
        float* f;
    };
};

template <typename T>
class DependentFields
{
public:
    class Nested
    {
    };
    Nested useDependentPointer(const T* t);
    const T& constField() const;
private:
    T* t = new T;
    Nested nested;
};

template <typename T>
const T& DependentFields<T>::constField() const
{
    return *t;
}

template <typename T>
typename DependentFields<T>::Nested DependentFields<T>::useDependentPointer(const T* t)
{
    return Nested();
}

class DLL_API HasVirtualInCore
{
public:
    HasVirtualInCore();
    HasVirtualInCore(TemplateClass<HasVirtualInCore> t);
    virtual int virtualInCore(int parameter);
};

class DLL_API DerivedFromSecondaryBaseInDependency;
typedef DerivedFromSecondaryBaseInDependency RenameDerivedBeforeBase;

class DLL_API SecondaryBase
{
public:
    void function();
};
