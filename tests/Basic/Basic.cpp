#include "Basic.h"

Foo::Foo()
{
}

const char* Foo::GetANSI()
{
	return "ANSI";
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

Bar::Item Bar::RetItem1()
{
    return Bar::Item1;
}

Bar* Bar::returnPointerToValueType()
{
    return this;
}

Bar2::Nested::operator int() const
{
    return 300;
}

Bar2::operator int() const
{
    return 500;
}

Bar2::operator Foo2()
{
    Foo2 f;
    f.A = A;
    f.B = B;
    f.C = C;

    return f;
}

Foo2 Bar2::needFixedInstance() const
{
    Foo2 f;
    f.A = A;
    f.B = B;
    f.C = C;

    return f;
}

Hello::Hello ()
{
    //cout << "Ctor!" << "\n";
}

Hello::Hello(const Hello& hello)
{

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

bool Hello::TestPrimitiveOut(CS_OUT float* f)
{
    *f = 10;
    return true;
}

bool Hello::TestPrimitiveOutRef(CS_OUT float& f)
{
    f = 10;
    return true;
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

int ImplementsAbstractFoo::pureFunction(int i)
{
    return 5;
}

int ImplementsAbstractFoo::pureFunction1()
{
    return 10;
}

int ImplementsAbstractFoo::pureFunction2()
{
    return 15;
}

const AbstractFoo& ReturnsAbstractFoo::getFoo()
{
    return i;
}

void DefaultParameters::Foo(int a, int b)
{
}

void DefaultParameters::Foo(int a)
{
}

void DefaultParameters::Bar() const
{
}

void DefaultParameters::Bar()
{
}

int test(basic& s)
{
    return 5;
}
