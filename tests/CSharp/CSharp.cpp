#include "CSharp.h"

Foo::Foo(const QString& name)
{
}

Foo::Foo(const char* name) : publicFieldMappedToEnum(TestFlag::Flag2)
{
    A = 10;
    P = 50;
    if (name)
    {
        _name = name;
    }
}

Foo::Foo(int a, int p) : publicFieldMappedToEnum(TestFlag::Flag2)
{
    A = a;
    P = p;
}

Foo::Foo(char16_t ch)
{
}

Foo::Foo(wchar_t ch)
{
}

Foo::Foo(const Foo& other) : A(other.A), P(other.P),
    templateInAnotherUnit(other.templateInAnotherUnit), _name(other._name)
{
}

Foo::~Foo()
{
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

const int& Foo::returnConstRef()
{
    return rename;
}

AbstractTemplate<int>* Foo::getAbstractTemplate()
{
    return new ImplementAbstractTemplate();
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

int Foo::operator ++()
{
    return 5;
}

int Foo::operator --()
{
    return 4;
}

Foo::operator const char*() const
{
    return _name.data();
}

const Foo& Bar::operator[](int i) const
{
    return m_foo;
}


Quux::Quux() : _setterWithDefaultOverload(0)
{

}

Quux::Quux(int i) : Quux()
{
    priv = i;
}

Quux::Quux(char c) : Quux()
{

}

Quux::Quux(Foo f) : Quux()
{

}

Quux::~Quux()
{
    if (_setterWithDefaultOverload)
    {
        delete _setterWithDefaultOverload;
        _setterWithDefaultOverload = 0;
    }
}

int Quux::getPriv() const
{
    return priv;
}

Foo* Quux::setterWithDefaultOverload()
{
    return _setterWithDefaultOverload;
}

void Quux::setSetterWithDefaultOverload(Foo* value)
{
    _setterWithDefaultOverload = value;
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

void Qux::makeClassDynamic()
{
}

int Qux::takeReferenceToPointer(Foo*& ret)
{
    return ret->A;
}

int Qux::type() const
{
    return 0;
}

Bar::Bar(Qux qux)
{
}

Bar::Bar(Items item)
{
}

Bar::~Bar()
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
    bar.index++;
    return bar;
}

int Bar::getIndex()
{
    return index;
}

void Bar::setIndex(int value)
{
    index = value;
}

int Bar::type() const
{
    return 1;
}

ForceCreationOfInterface::ForceCreationOfInterface()
{
}

ForceCreationOfInterface::~ForceCreationOfInterface()
{
}

Baz::Baz(Bar::Items item)
{
}

Baz::~Baz()
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

void Baz::setMethod(ProtectedNestedEnum value)
{
}

int Baz::type() const
{
    return -1;
}

AbstractProprietor::~AbstractProprietor()
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

const Baz& Proprietor::covariant()
{
    static Baz baz;
    return baz;
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

ComplexType::ComplexType(const QFlags<TestFlag> f) : qFlags(f)
{
}

ComplexType::ComplexType(const HasCtorWithMappedToEnum<TestFlag> f)
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

UsesPointerToEnum::UsesPointerToEnum()
{
}

void UsesPointerToEnum::hasPointerToEnumInParam(Flags* flag)
{
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

QGenericArgument::QGenericArgument(const char *name, const void* data)
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

DefaultZeroMappedToEnum::DefaultZeroMappedToEnum(int*)
{
}

MethodsWithDefaultValues::QMargins::QMargins(int left, int top, int right, int bottom)
{
}

const char* MethodsWithDefaultValues::stringConstant = "test";
int MethodsWithDefaultValues::intConstant = 5;

MethodsWithDefaultValues::MethodsWithDefaultValues(Foo foo)
{
    m_foo = foo;
}

MethodsWithDefaultValues::MethodsWithDefaultValues(int a)
{
    m_foo.A = a;
}

MethodsWithDefaultValues::MethodsWithDefaultValues(float a, Zero b)
{
}

MethodsWithDefaultValues::MethodsWithDefaultValues(double d, QList<QColor> list)
{
}

MethodsWithDefaultValues::MethodsWithDefaultValues(QRect* pointer, float f, int i)
{
}

MethodsWithDefaultValues::~MethodsWithDefaultValues()
{
}

void MethodsWithDefaultValues::defaultPointer(Foo *ptr1, Foo *ptr2)
{
}

void MethodsWithDefaultValues::defaultVoidStar(void* ptr)
{
}

void MethodsWithDefaultValues::defaultFunctionPointer(void(*functionPtr)(int p))
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

void MethodsWithDefaultValues::defaultEmptyEnum(Empty e)
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

void MethodsWithDefaultValues::defaultZeroMappedToEnumAssignedWithCtor(DefaultZeroMappedToEnum defaultZeroMappedToEnum)
{
}

Quux MethodsWithDefaultValues::defaultImplicitCtorInt(Quux arg)
{
    return arg;
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

void MethodsWithDefaultValues::defaultEmptyBraces(Foo foo)
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

HasPureVirtualWithDefaultArg::~HasPureVirtualWithDefaultArg()
{
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

void HasOverridesWithChangedAccessBase::differentIncreasedAccessOverride()
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

HasOverridesWithIncreasedProtectedAccess::HasOverridesWithIncreasedProtectedAccess()
{
}

void HasOverridesWithIncreasedProtectedAccess::differentIncreasedAccessOverride()
{
}

HasOverridesWithIncreasedAccess::HasOverridesWithIncreasedAccess()
{
}

void HasOverridesWithIncreasedAccess::privateOverride(int i)
{
}

void HasOverridesWithIncreasedAccess::differentIncreasedAccessOverride()
{
}

AbstractWithProperty::~AbstractWithProperty()
{
}

HasOverriddenInManaged::HasOverriddenInManaged()
{
}

HasOverriddenInManaged::~HasOverriddenInManaged()
{
}

void HasOverriddenInManaged::setOverriddenInManaged(Baz* value)
{
    overriddenInManaged = value;
}

int HasOverriddenInManaged::callOverriddenInManaged()
{
    return overriddenInManaged->type();
}

IgnoredType PropertyWithIgnoredType::ignoredType()
{
    return _ignoredType;
}

void PropertyWithIgnoredType::setIgnoredType(const IgnoredType& value)
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

TestOverrideFromSecondaryBase::TestOverrideFromSecondaryBase()
{
}

TestOverrideFromSecondaryBase::~TestOverrideFromSecondaryBase()
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

InheritanceBuffer::~InheritanceBuffer()
{
}

InheritsProtectedVirtualFromSecondaryBase::InheritsProtectedVirtualFromSecondaryBase()
{
}

InheritsProtectedVirtualFromSecondaryBase::~InheritsProtectedVirtualFromSecondaryBase()
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

TestOutTypeInterfaces::TestOutTypeInterfaces()
{
}

void TestOutTypeInterfaces::funcTryInterfaceTypePtrOut(CS_OUT TestParamToInterfacePassBaseTwo* classTry)
{
}

void TestOutTypeInterfaces::funcTryInterfaceTypeOut(CS_OUT TestParamToInterfacePassBaseTwo classTry)
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

OverridePropertyFromIndirectPrimaryBaseBase::~OverridePropertyFromIndirectPrimaryBaseBase()
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

AbstractOverrideFromSecondaryBase::~AbstractOverrideFromSecondaryBase()
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

InheritsFromHasSamePropertyInDerivedAbstractType::~InheritsFromHasSamePropertyInDerivedAbstractType()
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

HasPrivateVirtualProperty::~HasPrivateVirtualProperty()
{
}

int HasPrivateVirtualProperty::property()
{
    return 0;
}

void HasPrivateVirtualProperty::protectedMethod()
{
}

int HasPrivateOverriddenProperty::property()
{
    return 0;
}

void HasPrivateOverriddenProperty::protectedAbstractMethod()
{
}

void HasPrivateOverriddenProperty::protectedMethod()
{
}

int HasPrivateOverriddenProperty::protectedProperty()
{
    return 5;
}

int HasConflictWithProperty::conflictWithProperty()
{
    return 0;
}

int HasConflictWithProperty::getConflictWithProperty()
{
    return 0;
}

HasConflictWithAbstractProperty::~HasConflictWithAbstractProperty()
{
}

int HasConflictWithAbstractProperty::conflictWithProperty()
{
    return 0;
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

MissingObjectOnVirtualCallSecondaryBase::MissingObjectOnVirtualCallSecondaryBase()
{
}

int MissingObjectOnVirtualCallSecondaryBase::f()
{
    return 5;
}

MissingObjectOnVirtualCall::MissingObjectOnVirtualCall()
{
}

int MissingObjectOnVirtualCall::f()
{
    return 15;
}

HasMissingObjectOnVirtualCall::HasMissingObjectOnVirtualCall()
{
}

int HasMissingObjectOnVirtualCall::makeMissingObjectOnVirtualCall()
{
    return stackOverflowOnVirtualCall->f();
}

void HasMissingObjectOnVirtualCall::setMissingObjectOnVirtualCall(MissingObjectOnVirtualCall* value)
{
    stackOverflowOnVirtualCall = value;
}

AbstractPrimaryBase::~AbstractPrimaryBase()
{
}

AbstractSecondaryBase::~AbstractSecondaryBase()
{
}

ImplementsAbstractsFromPrimaryAndSecondary::ImplementsAbstractsFromPrimaryAndSecondary() : field(200)
{
}

ImplementsAbstractsFromPrimaryAndSecondary::~ImplementsAbstractsFromPrimaryAndSecondary()
{
}

int ImplementsAbstractsFromPrimaryAndSecondary::abstractInPrimaryBase()
{
    return 101;
}

int ImplementsAbstractsFromPrimaryAndSecondary::abstractInSecondaryBase()
{
    return 5;
}

int ImplementsAbstractsFromPrimaryAndSecondary::abstractReturnsFieldInPrimaryBase()
{
    return field + 1;
}

int ImplementsAbstractsFromPrimaryAndSecondary::abstractReturnsFieldInSecondaryBase()
{
    return field + 2;
}

HasBaseSetter::HasBaseSetter()
{
}

HasBaseSetter::~HasBaseSetter()
{
}

void HasBaseSetter::setBaseSetter(int value)
{
}

HasGetterAndOverriddenSetter::HasGetterAndOverriddenSetter()
{
}

HasGetterAndOverriddenSetter::~HasGetterAndOverriddenSetter()
{
}

int HasGetterAndOverriddenSetter::baseSetter()
{
    return field;
}

void HasGetterAndOverriddenSetter::setBaseSetter(int value)
{
    field = value;
}

void hasArrayOfConstChar(const char* const arrayOfConstChar[])
{
}

struct IncompleteStruct {};

IncompleteStruct* createIncompleteStruct()
{
    return new IncompleteStruct();
}

DLL_API void useIncompleteStruct(IncompleteStruct * a)
{
    return;
}

struct DuplicateDeclaredStruct {
    int i = 0;
};

DLL_API ForwardDeclaredStruct* createForwardDeclaredStruct(int i)
{
    auto ptr = new ForwardDeclaredStruct();
    ptr->i = i;
    return ptr;
}

DLL_API int useForwardDeclaredStruct(ForwardDeclaredStruct* s)
{
    return s->i;
}

DLL_API DuplicateDeclaredStruct* createDuplicateDeclaredStruct(int i)
{
    auto ptr = new DuplicateDeclaredStruct();
    ptr->i = i;
    return ptr;
}

DLL_API int useDuplicateDeclaredStruct(DuplicateDeclaredStruct* s)
{
    return s->i;
}

ComplexArrayElement::ComplexArrayElement() : BoolField(false), IntField(0), FloatField(0)
{
}

HasComplexArray::HasComplexArray()
{
}

TestIndexedProperties::TestIndexedProperties() : field(0)
{
}

void useStdStringJustAsParameter(std::string s)
{
}

int funcWithTypedefedFuncPtrAsParam(typedefedFuncPtr* func)
{
    Foo* a = 0;
    Bar b;
    return func(a, b);
}

typedefedFuncPtr* TestDuplicateDelegate::testDuplicateDelegate(int a)
{
    return 0;
}

void InlineNamespace::FunctionInsideInlineNamespace()
{
}

TestArrays::TestArrays()
{
}

TestArrays::~TestArrays()
{
}

int TestArrays::takeArrays(Foo* arrayOfPointersToObjects[], int arrayOfPrimitives[], Foo arrayOfObjects[]) const
{
    return arrayOfPointersToObjects[0]->A + arrayOfPointersToObjects[1]->A +
           arrayOfPrimitives[0] + arrayOfPrimitives[1] +
           arrayOfObjects[0].A + arrayOfObjects[1].A;
}

int TestArrays::takeArrays(Foo* fixedArrayOfPointersToObjects[3], int fixedArrayOfPrimitives[4],
                           int* fixedArrayOfPointersToPrimitives[5]) const
{
    int sum = 0;
    for (int i = 0; i < 3; i++)
    {
        sum += fixedArrayOfPointersToObjects[i]->A;
    }
    for (int i = 0; i < 4; i++)
    {
        sum += fixedArrayOfPrimitives[i];
    }
    for (int i = 0; i < 5; i++)
    {
        sum += *fixedArrayOfPointersToPrimitives[i];
    }
    return sum;
}

std::string TestArrays::takeStringArray(const char* arrayOfStrings[])
{
    std::string result;
    for (int i = 0; i < 3; i++)
    {
        result += arrayOfStrings[i];
    }
    return result;
}

std::string TestArrays::takeConstStringArray(const char* const arrayOfStrings[])
{
    return takeStringArray(const_cast<const char**>(arrayOfStrings));
}

int TestArrays::virtualTakeArrays(Foo* arrayOfPointersToObjects[], int arrayOfPrimitives[], Foo arrayOfObjects[]) const
{
    return takeArrays(arrayOfPointersToObjects, arrayOfPrimitives, arrayOfObjects);
}

int TestArrays::virtualTakeArrays(Foo *fixedArrayOfPointersToObjects[], int fixedArrayOfPrimitives[], int *fixedArrayOfPointersToPrimitives[]) const
{
    return takeArrays(fixedArrayOfPointersToObjects, fixedArrayOfPrimitives, fixedArrayOfPointersToPrimitives);
}

HasFixedArrayOfPointers::HasFixedArrayOfPointers()
{
}

HasFixedArrayOfPointers::~HasFixedArrayOfPointers()
{
}

SimpleInterface::SimpleInterface()
{
}

SimpleInterface::~SimpleInterface()
{
}

InterfaceTester::InterfaceTester() : interface(0)
{
}

InterfaceTester::~InterfaceTester()
{
}

int InterfaceTester::capacity()
{
    return interface->capacity();
}

int InterfaceTester::size()
{
    return interface->size();
}

void* InterfaceTester::get(int n)
{
    return interface->get(n);
}

void InterfaceTester::setInterface(SimpleInterface* i)
{
    interface = i;
}

HasFunctionPtrField::HasFunctionPtrField()
{
}

HasFunctionPtrField::~HasFunctionPtrField()
{
}

void va_listFunction(va_list v)
{
}

char* returnCharPointer()
{
    return 0;
}

char* takeCharPointer(char* c)
{
    return c;
}

char* takeConstCharRef(const char& c)
{
    return const_cast<char*>(&c);
}

const char*& takeConstCharStarRef(const char*& c)
{
    return c;
}

const void*& rValueReferenceToPointer(void*&& v)
{
    return (const void*&) v;
}

const Foo*& takeReturnReferenceToPointer(const Foo*& foo)
{
    return foo;
}

boolean_t takeTypemapTypedefParam(boolean_t b)
{
    return b;
}
