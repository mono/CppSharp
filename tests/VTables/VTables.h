#include "../Tests.h"

class DLL_API Foo
{
public:
    class Vfoo
    {

    };

    Foo();
    virtual int vfoo();
    virtual int vbar();

    virtual int append();
    virtual int append(int a);
    int callVirtualWithParameter(int a);
};

DLL_API int FooCallFoo(Foo* foo);

class DLL_API BaseClassVirtual
{
public:
    BaseClassVirtual();
    BaseClassVirtual(const BaseClassVirtual& other);
    static int virtualCallRetInt(BaseClassVirtual* base);
    virtual int retInt();
    static BaseClassVirtual getBase();
    static BaseClassVirtual* getBasePtr();
};

class DLL_API DerivedClassVirtual : public BaseClassVirtual
{
public:
    DerivedClassVirtual();
    virtual int retInt() override;
};
