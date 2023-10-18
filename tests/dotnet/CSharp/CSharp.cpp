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

SmallPOD Foo::getSmallPod_cdecl() { return { 10000, 40000 }; }
SmallPOD Foo::getSmallPod_stdcall() { return { 10000, 40000 }; }
SmallPOD Foo::getSmallPod_thiscall() { return { 10000, 40000 }; }

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

Bar::Bar() : index(0)
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

Baz::Baz() : P(5), functionTypedef(0) {}

Baz::Baz(Bar::Items item) : Baz()
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

AbstractProprietor::AbstractProprietor() : m_value(0), m_property(0)
{
}

AbstractProprietor::AbstractProprietor(int i) : AbstractProprietor()
{
}

Proprietor::Proprietor() : _items(Bar::Items::Item1), _itemsByValue(Bar::Items::Item1) {}

Proprietor::Proprietor(int i) : AbstractProprietor(i),
    _items(Bar::Items::Item1), _itemsByValue(Bar::Items::Item1)
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

TestDestructors::TestDestructors() { Marker = 0xf00d; }
TestDestructors::~TestDestructors() { Marker = 0xcafe; }

int TestDestructors::Marker = 0;

TestCopyConstructorVal::TestCopyConstructorVal() : A(0), B(0)
{
}

TestCopyConstructorVal::TestCopyConstructorVal(const TestCopyConstructorVal& other)
{
    A = other.A;
    B = other.B;
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

void UsesPointerToEnum::hasPointerToEnumInParam(Flags* flag)
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

QGenericArgument::QGenericArgument(const char* name, const void* data) :
    fixedArrayInValueType { 0 }
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

struct SmallPOD DefaultSmallPODInstance;

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

void MethodsWithDefaultValues::defaultChar(char c, char uc, char Uc, char Lc, unsigned char b)
{
}

void MethodsWithDefaultValues::defaultString(const wchar_t* wideString)
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

void MethodsWithDefaultValues::defaultTypedefMappedToEnumRefAssignedWithCtor(const TypedefedFlags& qFlags)
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

void MethodsWithDefaultValues::defaultWithParamRequiringRename(_ClassWithLeadingUnderscore* ptr)
{
}

void MethodsWithDefaultValues::defaultWithSpecialization(IndependentFields<int> specialization)
{
}

void MethodsWithDefaultValues::defaultOverloadedImplicitCtor(P p)
{
}

void MethodsWithDefaultValues::defaultOverloadedImplicitCtor(Qux q)
{
}

int MethodsWithDefaultValues::DefaultWithParamNamedSameAsMethod(int DefaultWithParamNamedSameAsMethod, const Foo& defaultArg)
{
    return 1;
}

int MethodsWithDefaultValues::defaultIntAssignedAnEnumWithBinaryOperatorAndFlags(int f)
{
    return f;
}

int MethodsWithDefaultValues::defaultWithConstantFlags(int f)
{
    return f;
}

bool MethodsWithDefaultValues::defaultWithPointerToEnum(UntypedFlags* f1, int* f2)
{
    return (f1 == NULL || *f1 == (UntypedFlags)0) && (f2 == NULL || *f2 == 0);
}

SmallPOD* MethodsWithDefaultValues::defaultWithNonPrimitiveType(SmallPOD& pod)
{
    return &pod;
}

int MethodsWithDefaultValues::getA()
{
    return m_foo.A;
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

void HasOverridesWithChangedAccess::privateOverride(int i)
{
}

void HasOverridesWithChangedAccess::publicOverride()
{
}

void HasOverridesWithIncreasedProtectedAccess::differentIncreasedAccessOverride()
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

MI_A0::MI_A0() : F(50) {}
int MI_A0::get() { return F; };

MI_A::MI_A() {}
void MI_A::v(int i) {}

MI_B::MI_B() {}

MI_C::MI_C() {}

MI_A1::MI_A1() {}

MI_D::MI_D() {}

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

void TestPointers::TestDoubleCharPointers(const char** names)
{
}

void TestPointers::TestTripleCharPointers(const char*** names)
{
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

CallDtorVirtually::~CallDtorVirtually()
{
    Destroyed = true;
}

bool CallDtorVirtually::Destroyed = false;

HasVirtualDtor1* CallDtorVirtually::getHasVirtualDtor1(HasVirtualDtor1* returned)
{
    return returned;
}

CallDtorVirtually nonOwnedInstance;
CallDtorVirtually* CallDtorVirtually::getNonOwnedInstance()
{
    return &nonOwnedInstance;
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

void HasProtectedVirtual::protectedVirtual()
{
}

void InheritsProtectedVirtualFromSecondaryBase::protectedVirtual()
{
}

void freeFunctionWithUnsupportedDefaultArg(Foo foo)
{
}

int TypeMappedWithOperator::operator |(int i)
{
   return 0;
}

void HasPropertyWithDerivedType::causeRenamingError()
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

int OverrideFromIndirectSecondaryBaseBase::property()
{
    return 0;
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

int PassConstantArrayRef(int(&arr)[2])
{
    return arr[0];
}

bool TestComparison::operator ==(const TestComparison& other) const
{
    return A == other.A && B == other.B;
}

OverridePropertyFromIndirectPrimaryBaseBase::~OverridePropertyFromIndirectPrimaryBaseBase()
{
}

void OverridePropertyFromDirectPrimaryBase::setProperty(int value)
{
}

int OverridePropertyFromIndirectPrimaryBase::property()
{
    return 5;
}

AbstractOverrideFromSecondaryBase::~AbstractOverrideFromSecondaryBase()
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

void QPaintDevice::changeVTableLayout()
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

QPainter::QPainter(QPaintDevice& paintDevice)
{
    paintDevice.test = 5;
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

const bool StaticVariables::Boolean = true;
const char StaticVariables::Chr = 'G';
const unsigned char StaticVariables::UChr = (unsigned char)'G';
const int StaticVariables::Int = 1020304050;
const float StaticVariables::Float = 0.5020f;
const std::string StaticVariables::String = "Str";
const char StaticVariables::ChrArray[2] { 'A', 'B' };
const int StaticVariables::IntArray[2] { 1020304050, 1526374850 };
const float StaticVariables::FloatArray[2] { 0.5020f, 0.6020f };
const bool StaticVariables::BoolArray[2] { false, true };
const void* StaticVariables::VoidPtrArray[2] { (void*)0x10203040, (void*)0x40302010 };

TestString::TestString() : unicodeConst(L"ქართული ენა"), unicode(0)
{
}

TestChar32String::TestChar32String() :
    thirtyTwoBitConst(U"ქართული ენა")
{
    static std::u32string nonConst = U"Test String";
    thirtyTwoBitNonConst = &nonConst[0];
}

TestChar32String::~TestChar32String() {}
void TestChar32String::UpdateString(const char32_t* s)
{
    static std::u32string nativeOwnedMemory = s;
    thirtyTwoBitConst = nativeOwnedMemory.data();
}

const char32_t* TestChar32String::RetrieveString() { return thirtyTwoBitConst; }
void TestChar32String::functionPointerUTF32(void(*ptr)(const char32_t*)) {}

TestChar16String::TestChar16String() :
    sixteenBitConst(u"ქართული ენა")
{
    static std::u16string nonConst = u"Test String";
    sixteenBitNonConst = &nonConst[0];
}

TestChar16String::~TestChar16String() {}

void TestChar16String::UpdateString(const char16_t* s)
{
    static std::u16string nativeOwnedMemory = s;
    sixteenBitConst = nativeOwnedMemory.data();
}
const char16_t* TestChar16String::RetrieveString() { return sixteenBitConst; }

void decltypeFunctionPointer() {}

void usesDecltypeFunctionPointer(funcPtr func) {}

void HasSecondaryBaseWithAbstractWithDefaultArg::abstract(const Foo& foo)
{
}

void HasSecondaryBaseWithAbstractWithDefaultArg::abstractWithNoDefaultArg(const Foo& foo)
{
}

int MissingObjectOnVirtualCallSecondaryBase::f()
{
    return 5;
}

int MissingObjectOnVirtualCall::f()
{
    return 15;
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

void HasBaseSetter::setBaseSetter(int value)
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

TestIndexedProperties::TestIndexedProperties() : field(0)
{
}

int TestIndexedProperties::operator[](const int& key)
{
    return key;
}

void* TestIndexedProperties::operator[](size_t n) const
{
    field = n;
    return &field;
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

int TestArrays::virtualTakeArrays(Foo *fixedArrayOfPointersToObjects[3], int fixedArrayOfPrimitives[4], int *fixedArrayOfPointersToPrimitives[5]) const
{
    return takeArrays(fixedArrayOfPointersToObjects, fixedArrayOfPrimitives, fixedArrayOfPointersToPrimitives);
}

InterfaceTester::InterfaceTester() : interface(0)
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

void takeMappedEnum(TestFlag value)
{
}

void takeMappedEnum(TestFlags value)
{
}

boolean_t takeTypemapTypedefParam(boolean_t b)
{
    return b;
}

const char* TestCSharpString(const char* in, const char** out)
{
    static std::string ret;
    ret = in;
    *out = ret.data();
    return ret.data();
}

const wchar_t* TestCSharpStringWide(const wchar_t* in, const wchar_t** out)
{
    static std::wstring ret;
    ret = in;
    *out = ret.data();
    return ret.data();
}

const char16_t* TestCSharpString16(const char16_t* in, const char16_t** out)
{
    static std::u16string ret;
    ret = in;
    *out = ret.data();
    return ret.data();
}

const char32_t* TestCSharpString32(const char32_t* in, const char32_t** out)
{
    static std::u32string ret;
    ret = in;
    *out = ret.data();
    return ret.data();
}

ConversionFunctions::operator short* () { return &field; }
ConversionFunctions::operator short& () { return field; }
ConversionFunctions::operator short() { return field; }
ConversionFunctions::operator const short*() const { return &field; }
ConversionFunctions::operator const short&() const { return field; }
ConversionFunctions::operator const short() const { return field; }

const unsigned ClassCustomTypeAlignmentOffsets[5]
{
    offsetof(ClassCustomTypeAlignment, boolean),
    offsetof(ClassCustomTypeAlignment, align16),
    offsetof(ClassCustomTypeAlignment, align1),
    offsetof(ClassCustomTypeAlignment, dbl),
    offsetof(ClassCustomTypeAlignment, align8),
};

const unsigned ClassCustomObjectAlignmentOffsets[2] {
    offsetof(ClassCustomObjectAlignment, boolean),
    offsetof(ClassCustomObjectAlignment, charAligned8),
};

const unsigned ClassMicrosoftObjectAlignmentOffsets[4]
{
    offsetof(ClassMicrosoftObjectAlignment, u8),
    offsetof(ClassMicrosoftObjectAlignment, dbl),
    offsetof(ClassMicrosoftObjectAlignment, i16),
    offsetof(ClassMicrosoftObjectAlignment, boolean),
};

const unsigned StructWithEmbeddedArrayOfStructObjectAlignmentOffsets[2]
{
    offsetof(StructWithEmbeddedArrayOfStructObjectAlignment, boolean),
    offsetof(StructWithEmbeddedArrayOfStructObjectAlignment, embedded_struct),
};

DLL_API FTIStruct TestFunctionToStaticMethod(FTIStruct* bb) { return { 6 }; }
DLL_API int TestFunctionToStaticMethodStruct(FTIStruct* bb, FTIStruct defaultValue) { return defaultValue.a; }
DLL_API int TestFunctionToStaticMethodRefStruct(FTIStruct* bb, FTIStruct& defaultValue) { return defaultValue.a; }
DLL_API int TestFunctionToStaticMethodConstStruct(FTIStruct* bb, const FTIStruct defaultValue) { return defaultValue.a; }
DLL_API int TestFunctionToStaticMethodConstRefStruct(FTIStruct* bb, const FTIStruct& defaultValue) { return defaultValue.a; }

DLL_API int TestClassFunctionToInstanceMethod(TestClass* bb, int value) { return value * value; }
DLL_API int TestClassFunctionToInstanceMethod(TestClass* bb, FTIStruct& value) { return value.a * value.a; }

int RuleOfThreeTester::constructorCalls = 0;
int RuleOfThreeTester::destructorCalls = 0;
int RuleOfThreeTester::copyConstructorCalls = 0;
int RuleOfThreeTester::copyAssignmentCalls = 0;

void RuleOfThreeTester::reset()
{
    constructorCalls = 0;
    destructorCalls = 0;
    copyConstructorCalls = 0;
    copyAssignmentCalls = 0;
}

RuleOfThreeTester::RuleOfThreeTester()
{
    a = 0;
    constructorCalls++;
}

RuleOfThreeTester::RuleOfThreeTester(const RuleOfThreeTester& other)
{
    a = other.a;
    copyConstructorCalls++;
}

RuleOfThreeTester::~RuleOfThreeTester()
{
    destructorCalls++;
}

RuleOfThreeTester& RuleOfThreeTester::operator=(const RuleOfThreeTester& other)
{
    a = other.a;
    copyAssignmentCalls++;
    return *this;
}

// test if generated code correctly calls constructors and destructors when going from C++ to C#
void CallCallByValueInterfaceValue(CallByValueInterface* interface)
{
    RuleOfThreeTester value;
    interface->CallByValue(value);
}

void CallCallByValueInterfaceReference(CallByValueInterface* interface)
{
    RuleOfThreeTester value;
    interface->CallByReference(value);
}

void CallCallByValueInterfacePointer(CallByValueInterface* interface)
{
    RuleOfThreeTester value;
    interface->CallByPointer(&value);
}

static PointerTester internalPointerTesterInstance;

PointerTester::PointerTester()
{
    a = 0;
}

bool PointerTester::IsDefaultInstance()
{
    return this == &internalPointerTesterInstance;
}

bool PointerTester::IsValid()
{
    return a == 0;
}

PointerTester* PointerToClass = &internalPointerTesterInstance;

int ValueTypeOutParameter(UnionTester* testerA, UnionTester* testerB)
{
    testerA->a = 2;
    testerB->b = 2;
    return 2;
}
