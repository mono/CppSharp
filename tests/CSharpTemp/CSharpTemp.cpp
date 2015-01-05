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


Quux::Quux()
{

}

Quux::Quux(int i)
{

}

Quux::Quux(char c)
{

}

Quux::Quux(Foo f)
{

}



QColor::QColor()
{

}

QColor::QColor(Qt::GlobalColor color)
{

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

void Qux::obsolete()
{

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

int Baz::takesQux(const Qux& qux)
{
    return qux.farAwayFunc();
}

Qux Baz::returnQux()
{
    return Qux();
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

template <typename T>
QFlags<T>::QFlags(T t) : flag(t)
{
}

template <typename T>
QFlags<T>::QFlags(Zero) : flag(0)
{
}

template <typename T>
QFlags<T>::operator T()
{
    return flag;
}

ComplexType::ComplexType() : qFlags(QFlags<TestFlag>(TestFlag::Flag2))
{
}

int ComplexType::check()
{
    return 5;
}

QFlags<TestFlag> ComplexType::returnsQFlags()
{
    return qFlags;
}

void ComplexType::takesQFlags(const QFlags<int> f)
{

}

P::P(const Qux &qux)
{

}

P::P(Qux *qux)
{

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

bool P::isTest()
{
    return true;
}

void P::setTest(bool value)
{

}

void P::test()
{

}

bool P::isBool()
{
    return false;
}

void P::setIsBool(bool value)
{

}

int TestDestructors::Marker = 0;

TestCopyConstructorVal::TestCopyConstructorVal()
{
}

TestCopyConstructorVal::TestCopyConstructorVal(const TestCopyConstructorVal& other)
{
    A = other.A;
    B = other.B;
}

Flags operator|(Flags lhs, Flags rhs)
{
    return static_cast<Flags>(static_cast<int>(lhs) | static_cast<int>(rhs));
}

UntypedFlags operator|(UntypedFlags lhs, UntypedFlags rhs)
{
    return static_cast<UntypedFlags>(static_cast<int>(lhs) | static_cast<int>(rhs));
}

QGenericArgument::QGenericArgument(const char *name)
{
    _name = name;
}

MethodsWithDefaultValues::MethodsWithDefaultValues(Foo foo)
{
    m_foo = foo;
}

MethodsWithDefaultValues::MethodsWithDefaultValues(int a)
{
    m_foo.A = a;
}

void MethodsWithDefaultValues::defaultPointer(Foo *ptr)
{
}

void MethodsWithDefaultValues::defaultVoidStar(void* ptr)
{
}

void MethodsWithDefaultValues::defaultValueType(QGenericArgument valueType)
{
}

void MethodsWithDefaultValues::defaultChar(char c)
{
}

void MethodsWithDefaultValues::defaultEmptyChar(char c)
{
}

void MethodsWithDefaultValues::defaultRefTypeBeforeOthers(Foo foo, int i, Bar::Items item)
{
}

void MethodsWithDefaultValues::defaultRefTypeAfterOthers(int i, Bar::Items item, Foo foo)
{
}

void MethodsWithDefaultValues::defaultRefTypeBeforeAndAfterOthers(int i, Foo foo, Bar::Items item, Baz baz)
{
}

void MethodsWithDefaultValues::defaultIntAssignedAnEnum(int i)
{
}

void MethodsWithDefaultValues::defaultRefAssignedValue(const Foo &fooRef)
{
}

void MethodsWithDefaultValues::defaultMappedToEnum(QFlags<Flags> qFlags)
{
}

void MethodsWithDefaultValues::defaultMappedToZeroEnum(QFlags<Flags> qFlags)
{
}

void MethodsWithDefaultValues::defaultImplicitCtorInt(Quux arg)
{
}

void MethodsWithDefaultValues::defaultImplicitCtorChar(Quux arg)
{
}

void MethodsWithDefaultValues::defaultIntWithLongExpression(unsigned int i)
{
}

void MethodsWithDefaultValues::defaultRefTypeEnumImplicitCtor(const QColor &fillColor)
{
}

void MethodsWithDefaultValues::rotate4x4Matrix(float angle, float x, float y, float z)
{
}

int MethodsWithDefaultValues::getA()
{
    return m_foo.A;
}

void HasPrivateOverrideBase::privateOverride(int i)
{
}

void HasPrivateOverride::privateOverride(int i)
{
}

IgnoredType<int> PropertyWithIgnoredType::ignoredType()
{
    return _ignoredType;
}

void PropertyWithIgnoredType::setIgnoredType(const IgnoredType<int> &value)
{
    _ignoredType = value;
}

StructWithPrivateFields::StructWithPrivateFields(int simplePrivateField, Foo complexPrivateField)
{
    this->simplePrivateField = simplePrivateField;
    this->complexPrivateField = complexPrivateField;
}

int StructWithPrivateFields::getSimplePrivateField()
{
    return simplePrivateField;
}

Foo StructWithPrivateFields::getComplexPrivateField()
{
    return complexPrivateField;
}
