#include "CSharp.h"

Foo::Foo(char* name)
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

void Foo::foo(int i)
{
}

void Foo::takesStdVector(const std::vector<int>& vector)
{
}

int Foo::width()
{
    return 1;
}

void Foo::set_width(int value)
{
}

const int Foo::rename;

int Foo::makeFunctionCall()
{
    return 1;
}

int Foo::propertyCall()
{
    return 1;
}

int Foo::getGetPropertyCall()
{
    return 1;
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

Bar::Bar(Items item)
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

Baz::Baz(Bar::Items item)
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

Bar::Items Proprietor::items() const
{
    return _items;
}

void Proprietor::setItems(const Bar::Items& value)
{
    _items = value;
}

Bar::Items Proprietor::itemsByValue() const
{
    return _itemsByValue;
}

void Proprietor::setItemsByValue(Bar::Items value)
{
    _itemsByValue = value;
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

TestRenaming::TestRenaming()
{
}

TestRenaming::~TestRenaming()
{
}

void TestRenaming::name()
{
}

void TestRenaming::Name()
{
}

int TestRenaming::property()
{
    return 1;
}

UsesPointerToEnumInParamOfVirtual::UsesPointerToEnumInParamOfVirtual()
{
}

UsesPointerToEnumInParamOfVirtual::~UsesPointerToEnumInParamOfVirtual()
{
}

QFlags<Flags> UsesPointerToEnumInParamOfVirtual::hasPointerToEnumInParam(const QFlags<Flags>& pointerToEnum) const
{
    return pointerToEnum;
}

QFlags<Flags> UsesPointerToEnumInParamOfVirtual::callOverrideOfHasPointerToEnumInParam(
        const UsesPointerToEnumInParamOfVirtual* object, const QFlags<Flags>& pointerToEnum)
{
    return object->hasPointerToEnumInParam(pointerToEnum);
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

char* MethodsWithDefaultValues::stringConstant = "test";
int MethodsWithDefaultValues::intConstant = 5;

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

void MethodsWithDefaultValues::defaultNonEmptyCtorWithNullPtr(QGenericArgument arg)
{
}

QFlags<Flags> MethodsWithDefaultValues::defaultMappedToEnum(const QFlags<Flags>& qFlags)
{
    return qFlags;
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

void MethodsWithDefaultValues::defaultImplicitCtorEnum(Baz arg)
{
}

void MethodsWithDefaultValues::defaultImplicitCtorEnumTwo(Bar arg)
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

void MethodsWithDefaultValues::defaultWithFunctionCall(int f)
{
}

void MethodsWithDefaultValues::defaultWithPropertyCall(int f)
{
}

void MethodsWithDefaultValues::defaultWithGetPropertyCall(int f)
{
}

void MethodsWithDefaultValues::defaultWithIndirectStringConstant(const Foo& arg)
{
}

void MethodsWithDefaultValues::defaultWithDirectIntConstant(int arg)
{
}

void MethodsWithDefaultValues::defaultWithEnumInLowerCasedNameSpace(lowerCaseNameSpace::Enum e)
{
}

void MethodsWithDefaultValues::defaultWithCharFromInt(char c)
{
}

void MethodsWithDefaultValues::defaultWithFreeConstantInNameSpace(int c)
{
}

void MethodsWithDefaultValues::defaultWithStdNumericLimits(double d, int i)
{
}

int MethodsWithDefaultValues::DefaultWithParamNamedSameAsMethod(int DefaultWithParamNamedSameAsMethod, const Foo& defaultArg)
{
    return 1;
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

HasOverridesWithIncreasedAccess::HasOverridesWithIncreasedAccess()
{
}

void HasOverridesWithIncreasedAccess::privateOverride(int i)
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

AbstractOverrideFromSecondaryBase::AbstractOverrideFromSecondaryBase()
{
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

QPaintDevice::QPaintDevice() : test(0)
{
}

QPaintDevice::~QPaintDevice()
{
}

void QPaintDevice::changeVTableLayout()
{
}

QWidget::QWidget()
{
    QApplication::instance->notify(this);
}

QWidget::~QWidget()
{
}

void QWidget::event()
{
    QApplication::instance->notify(&child);
}

QPainter::QPainter(QPaintDevice& paintDevice)
{
    paintDevice.test = 5;
}

QPainter::~QPainter()
{
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

VirtualDtorAddedInDerived::VirtualDtorAddedInDerived()
{
}

VirtualDtorAddedInDerived::~VirtualDtorAddedInDerived()
{
    dtorCalled = true;
}

bool VirtualDtorAddedInDerived::dtorCalled = false;

void NamespaceB::B::Function(CS_OUT NamespaceA::A &a)
{
}

HasVirtualTakesReturnsProblematicTypes::HasVirtualTakesReturnsProblematicTypes()
{
}

HasVirtualTakesReturnsProblematicTypes::~HasVirtualTakesReturnsProblematicTypes()
{
}

const char* HasVirtualTakesReturnsProblematicTypes::virtualTakesAndReturnsString(const char* c)
{
    return c;
}

const char* HasVirtualTakesReturnsProblematicTypes::callsVirtualToReturnString(const char* c)
{
    return virtualTakesAndReturnsString(c);
}

bool HasVirtualTakesReturnsProblematicTypes::virtualTakesAndReturnsBool(bool b)
{
    return b;
}

bool HasVirtualTakesReturnsProblematicTypes::callsVirtualToReturnBool(bool b)
{
    return virtualTakesAndReturnsBool(b);
}

extern const unsigned char variableWithFixedPrimitiveArray[2] = { 5, 10 };
extern const unsigned int variableWithVariablePrimitiveArray[] = { 15, 20 };

TestString::TestString() : unicodeConst(L"ქართული ენა"), unicode(0)
{
}

TestString::~TestString()
{
}

PrimaryBaseWithAbstractWithDefaultArg::PrimaryBaseWithAbstractWithDefaultArg()
{
}

PrimaryBaseWithAbstractWithDefaultArg::~PrimaryBaseWithAbstractWithDefaultArg()
{
}

SecondaryBaseWithAbstractWithDefaultArg::SecondaryBaseWithAbstractWithDefaultArg()
{
}

SecondaryBaseWithAbstractWithDefaultArg::~SecondaryBaseWithAbstractWithDefaultArg()
{
}

HasSecondaryBaseWithAbstractWithDefaultArg::HasSecondaryBaseWithAbstractWithDefaultArg()
{
}

HasSecondaryBaseWithAbstractWithDefaultArg::~HasSecondaryBaseWithAbstractWithDefaultArg()
{
}

void HasSecondaryBaseWithAbstractWithDefaultArg::abstract(const Foo& foo)
{
}

void HasSecondaryBaseWithAbstractWithDefaultArg::abstractWithNoDefaultArg(const Foo& foo)
{
}
