#include "CSharpTemp.h"

Foo::Foo()
{
    A = 10;
    P = 50;
}

int Foo::method()
{
    return 1;
}

int Foo::operator[](int i) const
{
    return 5;
}

int Foo::operator[](unsigned int i)
{
    return 15;
}

int& Foo::operator[](int i)
{
    return P;
}

const Foo& Bar::operator[](int i) const
{
    return m_foo;
}

int Qux::farAwayFunc() const
{
    return 20;
}

int Bar::method()
{
    return 2;
}

Foo& Bar::operator[](int i)
{
    return m_foo;
}

Baz::Nested::operator int() const
{
    return 300;
}

int Baz::takesQux(const Qux& qux)
{
    return qux.farAwayFunc();
}

Qux Baz::returnQux()
{
    return Qux();
}

Baz::operator int() const
{
    return 500;
}
