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
