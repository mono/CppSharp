#include "CSharp.h"

Foo::Foo()
{
    A = 10;
    P = 50;
}

Foo::Foo(int a, int p)
{
    A = a;
    P = p;
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

bool Foo::isNoParams()
{
    return false;
}

void Foo::setNoParams()
{
}

const int Foo::rename;

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

Qux::Qux(const Qux& other)
{
    for (int i = 0; i < 3; i++)
    {
        array[i] = other.array[i];
    }
}

Qux::Qux(Foo foo)
{
}

Qux::Qux(Bar bar)
{
}

int Qux::farAwayFunc() const
{
    return 20;
}

void Qux::obsolete()
{

}

Qux* Qux::getInterface()
{
    return this;
}

void Qux::setInterface(Qux *qux)
{
}

void Qux::v()
{
}

Bar::Bar(Qux qux)
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

ForceCreationOfInterface::ForceCreationOfInterface()
{
}

int Baz::takesQux(const Qux& qux)
{
    return qux.farAwayFunc();
}

Qux Baz::returnQux()
{
    return Qux();
}

void Baz::setMethod(int value)
{
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

int AbstractProprietor::parent() const
{
    return 0;
}

AbstractProprietor::AbstractProprietor()
{
}

AbstractProprietor::AbstractProprietor(int i)
{
}

Proprietor::Proprietor(int i) : AbstractProprietor(i)
{
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

ComplexType::ComplexType() : qFlags(QFlags<TestFlag>(TestFlag::Flag2))
{
}

ComplexType::ComplexType(const QFlags<TestFlag> f) : qFlags(QFlags<TestFlag>(TestFlag::Flag2))
{
    qFlags = f;
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

void TestDestructors::InitMarker()
{
    Marker = 0;
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

QPoint::QPoint(int x, int y)
{
}

QSize::QSize(int w, int h)
{
}

QRect::QRect(QPoint p, QSize s)
{
}

MethodsWithDefaultValues::QMargins::QMargins(int left, int top, int right, int bottom)
{
}

MethodsWithDefaultValues::MethodsWithDefaultValues(Foo foo)
{
    m_foo = foo;
}

MethodsWithDefaultValues::MethodsWithDefaultValues(int a)
{
    m_foo.A = a;
}

MethodsWithDefaultValues::MethodsWithDefaultValues(double d, QList<QColor> list)
{
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

void MethodsWithDefaultValues::DefaultRefAssignedValue(const Foo &fooRef)
{
}

void MethodsWithDefaultValues::defaultEnumAssignedBitwiseOr(Flags flags)
{
}

void MethodsWithDefaultValues::defaultEnumAssignedBitwiseOrShort(UntypedFlags flags)
{
}

void MethodsWithDefaultValues::defaultNonEmptyCtor(QGenericArgument arg)
{
}

void MethodsWithDefaultValues::defaultMappedToEnum(QFlags<Flags> qFlags)
{
}

void MethodsWithDefaultValues::defaultMappedToZeroEnum(QFlags<Flags> qFlags)
{
}

void MethodsWithDefaultValues::defaultMappedToEnumAssignedWithCtor(QFlags<Flags> qFlags)
{
}

void MethodsWithDefaultValues::defaultImplicitCtorInt(Quux arg)
{
}

void MethodsWithDefaultValues::defaultImplicitCtorChar(Quux arg)
{
}

void MethodsWithDefaultValues::defaultImplicitCtorFoo(Quux arg)
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

void MethodsWithDefaultValues::defaultPointerToValueType(QGenericArgument* pointer)
{
}

void MethodsWithDefaultValues::defaultDoubleWithoutF(double d1, double d2)
{
}

void MethodsWithDefaultValues::defaultIntExpressionWithEnum(int i)
{
}

void MethodsWithDefaultValues::defaultCtorWithMoreThanOneArg(QMargins m)
{
}

void MethodsWithDefaultValues::defaultWithComplexArgs(const QRect& rectangle)
{
}

void MethodsWithDefaultValues::defaultWithRefManagedLong(long long* i)
{
}

int MethodsWithDefaultValues::getA()
{
    return m_foo.A;
}

HasOverridesWithChangedAccessBase::HasOverridesWithChangedAccessBase()
{
}

void HasOverridesWithChangedAccessBase::privateOverride(int i)
{
}

void HasOverridesWithChangedAccessBase::publicOverride()
{
}

HasOverridesWithChangedAccess::HasOverridesWithChangedAccess()
{
}

void HasOverridesWithChangedAccess::privateOverride(int i)
{
}

void HasOverridesWithChangedAccess::publicOverride()
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

HasVirtualDtor1::HasVirtualDtor1()
{
    testField = 5;
}

HasVirtualDtor1::~HasVirtualDtor1()
{
}

HasVirtualDtor2::HasVirtualDtor2()
{
    hasVirtualDtor1 = new HasVirtualDtor1();
}

HasVirtualDtor2::~HasVirtualDtor2()
{
    delete hasVirtualDtor1;
}

HasVirtualDtor1* HasVirtualDtor2::getHasVirtualDtor1()
{
    return hasVirtualDtor1;
}

void HasVirtualDtor2::virtualFunction(const HasVirtualDtor1& param1, const HasVirtualDtor1& param2)
{
}

TestNativeToManagedMap::TestNativeToManagedMap()
{
    hasVirtualDtor2 = new HasVirtualDtor2();
}

TestNativeToManagedMap::~TestNativeToManagedMap()
{
    delete hasVirtualDtor2;
}

HasVirtualDtor2* TestNativeToManagedMap::getHasVirtualDtor2()
{
    return hasVirtualDtor2;
}

Bar* TestNativeToManagedMap::propertyWithNoVirtualDtor() const
{
    return bar;
}

void TestNativeToManagedMap::setPropertyWithNoVirtualDtor(Bar* bar)
{
    this->bar = bar;
}

CallDtorVirtually::CallDtorVirtually()
{
}

CallDtorVirtually::~CallDtorVirtually()
{
    Destroyed = true;
}

bool CallDtorVirtually::Destroyed = false;

HasVirtualDtor1* CallDtorVirtually::getHasVirtualDtor1(HasVirtualDtor1* returned)
{
    return returned;
}

void SecondaryBase::VirtualMember()
{
}

int SecondaryBase::property()
{
    return 0;
}

void SecondaryBase::setProperty(int value)
{
}

void SecondaryBase::function(bool* ok)
{
}

void SecondaryBase::protectedFunction()
{
}

int SecondaryBase::protectedProperty()
{
    return 0;
}

void SecondaryBase::setProtectedProperty(int value)
{
}

TestOverrideFromSecondaryBase::TestOverrideFromSecondaryBase()
{
}

void TestOverrideFromSecondaryBase::VirtualMember()
{
}

void TestOverrideFromSecondaryBase::setProperty(int value)
{
}

int TestParamToInterfacePassBaseTwo::getM()
{
	return m;
}

void TestParamToInterfacePassBaseTwo::setM(int n)
{
	m = n;
}

const TestParamToInterfacePassBaseTwo& TestParamToInterfacePassBaseTwo::operator++()
{
	++m;
	return *this;
}

TestParamToInterfacePassBaseTwo::TestParamToInterfacePassBaseTwo()
{
	m = 0;
}

TestParamToInterfacePassBaseTwo::TestParamToInterfacePassBaseTwo(int n)
{
	m = n;
}

TestParamToInterfacePassBaseTwo TestParamToInterfacePass::addM(TestParamToInterfacePassBaseTwo b)
{
	this->setM(this->getM() + b.getM());
	return *this;
}

TestParamToInterfacePassBaseTwo TestParamToInterfacePass::operator+(TestParamToInterfacePassBaseTwo b)
{
	return TestParamToInterfacePassBaseTwo(this->getM() + b.getM());
}

TestParamToInterfacePass::TestParamToInterfacePass(TestParamToInterfacePassBaseTwo b)
{
	this->setM(b.getM());
}

TestParamToInterfacePass::TestParamToInterfacePass() : TestParamToInterfacePassBaseOne(), TestParamToInterfacePassBaseTwo()
{
}

HasProtectedVirtual::HasProtectedVirtual()
{
}

void HasProtectedVirtual::protectedVirtual()
{
}

InheritanceBuffer::InheritanceBuffer()
{
}

InheritsProtectedVirtualFromSecondaryBase::InheritsProtectedVirtualFromSecondaryBase()
{
}

void InheritsProtectedVirtualFromSecondaryBase::protectedVirtual()
{
}

void freeFunctionWithUnsupportedDefaultArg(Foo foo)
{
}

TypeMappedWithOperator::TypeMappedWithOperator()
{
}

int TypeMappedWithOperator::operator |(int i)
{
   return 0;
}

HasPropertyWithDerivedType::HasPropertyWithDerivedType()
{
}

void HasPropertyWithDerivedType::causeRenamingError()
{
}

HasOverrideOfHasPropertyWithDerivedType::HasOverrideOfHasPropertyWithDerivedType()
{
}

void HasOverrideOfHasPropertyWithDerivedType::causeRenamingError()
{
}

void MultiOverloadPtrToRef::funcPrimitivePtrToRef(int* pOne, char* pTwo, float* pThree, bool* pFour)
{
}

void MultiOverloadPtrToRef::funcPrimitivePtrToRefWithDefVal(int* pOne, char* pTwo, Foo* pThree, int* pFour)
{
}

void MultiOverloadPtrToRef::funcPrimitivePtrToRefWithMultiOverload(int* pOne, char* pTwo, Foo* pThree, int* pFour, long* pFive)
{
}

MultiOverloadPtrToRef::MultiOverloadPtrToRef(int* param)
{
    arr = new int[3]{0};
}

int* MultiOverloadPtrToRef::ReturnPrimTypePtr()
{
    return arr;
}

void MultiOverloadPtrToRef::TakePrimTypePtr(int* ptr)
{
    ptr[0] = 100;
    ptr[1] = 200;
    ptr[2] = 300;
}

OverrideFromIndirectSecondaryBaseBase::OverrideFromIndirectSecondaryBaseBase()
{
}

int OverrideFromIndirectSecondaryBaseBase::property()
{
    return 0;
}

OverrideFromDirectSecondaryBase::OverrideFromDirectSecondaryBase()
{
}

OverrideFromIndirectSecondaryBase::OverrideFromIndirectSecondaryBase()
{
}

int OverrideFromIndirectSecondaryBase::property()
{
    return 1;
}

void TestOutTypeInterfaces::funcTryInterfaceTypePtrOut(CS_OUT TestParamToInterfacePassBaseTwo* classTry)
{
}

void TestOutTypeInterfaces::funcTryInterfaceTypeOut(CS_OUT TestParamToInterfacePassBaseTwo classTry)
{
}

template <typename T>
TemplateWithDependentField<T>::TemplateWithDependentField()
{
}

DerivesFromTemplateInstantiation::DerivesFromTemplateInstantiation()
{
}

int PassConstantArrayRef(int(&arr)[2])
{
    return arr[0];
}

TestComparison::TestComparison()
{
}

bool TestComparison::operator ==(const TestComparison& other) const
{
    return A == other.A && B == other.B;
}

OverridePropertyFromIndirectPrimaryBaseBase::OverridePropertyFromIndirectPrimaryBaseBase()
{
}

OverridePropertyFromDirectPrimaryBase::OverridePropertyFromDirectPrimaryBase()
{
}

void OverridePropertyFromDirectPrimaryBase::setProperty(int value)
{
}

OverridePropertyFromIndirectPrimaryBase::OverridePropertyFromIndirectPrimaryBase()
{
}

int OverridePropertyFromIndirectPrimaryBase::property()
{
    return 5;
}

QObject::QObject()
{
}

QObject::~QObject()
{
}

void QObject::event()
{
}

QWidget::QWidget()
{
    QApplication::instance->notify(this);
}

void QWidget::event()
{
    QApplication::instance->notify(&child);
}

QApplication::QApplication()
{
    instance = this;
}

QApplication* QApplication::instance = 0;

void QApplication::notify(QObject* receiver)
{
    receiver->event();
}

HasSamePropertyInDerivedAbstractType::HasSamePropertyInDerivedAbstractType()
{
}

char* HasSamePropertyInDerivedAbstractType::property()
{
    return 0;
}

InheritsFromHasSamePropertyInDerivedAbstractType::InheritsFromHasSamePropertyInDerivedAbstractType()
{
}

MultipleInheritanceFieldOffsetsSecondaryBase::MultipleInheritanceFieldOffsetsSecondaryBase() : secondary(2)
{
}

MultipleInheritanceFieldOffsetsPrimaryBase::MultipleInheritanceFieldOffsetsPrimaryBase() : primary(1)
{
}

MultipleInheritanceFieldOffsets::MultipleInheritanceFieldOffsets() : own(3)
{
}
