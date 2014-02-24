#include "VTables.h"

Foo::Foo()
{
}

int Foo::vfoo()
{
    return 5;
}

int Foo::vbar()
{
    return vfoo();
}

int Foo::append()
{
    return 1;
}

int Foo::append(int a)
{
    return ++a;
}

int Foo::callVirtualWithParameter(int a)
{
    return append(a);
}

int FooCallFoo(Foo* foo)
{
    return foo->vfoo() + 2;
}
