#include "Basic.h"

Foo::Foo()
{
}

Foo2 Foo2::operator<<(signed int i)
{
    Foo2 foo;
    foo.C = C << i;
    return foo;
}

Foo2 Foo2::operator<<(signed long l)
{
    Foo2 foo;
    foo.C = C << l;
    return foo;
}

Bar::Bar()
{
}

Hello::Hello ()
{
    //cout << "Ctor!" << "\n";
}

void Hello::PrintHello(const char* s)
{
    //cout << "PrintHello: " << s << "\n";
}

bool Hello::test1(int i, float f)
{
    return i == f;
}

int Hello::add(int a, int b)
{
    return a + b;
}

int Hello::AddFoo(Foo foo)
{
    return (int)(foo.A + foo.B);
}

int Hello::AddFooRef(Foo& foo)
{
    return AddFoo(foo);
}

int Hello::AddFooPtr(Foo* foo)
{
    return AddFoo(*foo);
}

int Hello::AddFoo2(Foo2 foo)
{
    return (int)(foo.A + foo.B + foo.C);
}

int Hello::AddBar(Bar bar)
{
    return (int)(bar.A + bar.B);
}

int Hello::AddBar2(Bar2 bar)
{
    return (int)(bar.A + bar.B + bar.C);
}

Foo Hello::RetFoo(int a, float b)
{
    Foo foo;
    foo.A = a;
    foo.B = b;
    return foo;
}

int Hello::RetEnum(Enum e)
{
    return (int)e;
}

Hello* Hello::RetNull()
{
    return 0;
}

int unsafeFunction(const Bar& ret, char* testForString, void (*foo)(int))
{
    return ret.A;
}

const wchar_t* wcharFunction(const wchar_t* constWideChar)
{
    return constWideChar;
}

Bar operator-(const Bar& b)
{
    Bar nb;
    nb.A = -b.A;
    nb.B = -b.B;
    return nb;
}

Bar operator+(const Bar& b1, const Bar& b2)
{
    Bar b;
    b.A = b1.A + b2.A;
    b.B = b1.B + b2.B;
    return b;
}

Bar indirectReturn()
{
    return Bar();
}
