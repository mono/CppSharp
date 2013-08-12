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
    return 3;
}

int FooCallFoo(Foo* foo)
{
    return foo->vfoo() + 2;
}