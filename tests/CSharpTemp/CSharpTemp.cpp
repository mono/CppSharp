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

Qux::Qux()
{

}

Qux::Qux(Foo foo)
{

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

Bar Bar::operator *()
{
    return *this;
}

const Bar& Bar::operator *(int m)
{
    index *= m;
    return *this;
}

const Bar& Bar::operator ++()
{
    ++index;
    return *this;
}

Bar Bar::operator ++(int i)
{
    Bar bar = *this;
    index++;
    return bar;
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

int AbstractProprietor::getValue()
{
    return m_value;
}

void AbstractProprietor::setProp(long property)
{
    m_property = property;
}

int AbstractProprietor::parent()
{
    return 0;
}

void Proprietor::setValue(int value)
{
    m_value = value;
}

long Proprietor::prop()
{
    return m_property;
}

void P::setValue(int value)
{
    m_value = value + 10;
}

long P::prop()
{
    return m_property + 100;
}

int ComplexType::check()
{
    return 5;
}

ComplexType P::complexType()
{
    return m_complexType;
}

void P::setComplexType(const ComplexType& value)
{
    m_complexType = value;
}

void P::parent(int i)
{

}
