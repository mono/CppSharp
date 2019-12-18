#include "Common.h"
#include <string.h>

TestPacking::TestPacking()
{
}

TestPacking1::TestPacking1()
{
}

TestPacking1::~TestPacking1()
{
}

TestPacking2::TestPacking2()
{
}

TestPacking2::~TestPacking2()
{
}

TestPacking4::TestPacking4()
{
}

TestPacking4::~TestPacking4()
{
}

TestPacking8::TestPacking8()
{
}

TestPacking8::~TestPacking8()
{
}

Foo::NestedAbstract::~NestedAbstract()
{
}

Foo::Foo()
{
    auto p = new int[4];
    for (int i = 0; i < 4; i++)
        p[i] = i;
    SomePointer = p;
    SomePointerPointer = &SomePointer;
}

Foo::Foo(Private p)
{
}

const char* Foo::GetANSI()
{
    return "ANSI";
}

void Foo::TakesTypedefedPtr(FooPtr date)
{
}

int Foo::TakesRef(const Foo &other)
{
    return other.A;
}

bool Foo::operator ==(const Foo& other) const
{
    return A == other.A && B == other.B;
}

int Foo::fooPtr()
{
    return 1;
}

char16_t Foo::returnChar16()
{
    return 'a';
}

Foo2::Foo2() {}

Foo2 Foo2::operator<<(signed int i)
{
    Foo2 foo;
    foo.C = C << i;
    foo.valueTypeField = valueTypeField;
    foo.valueTypeField.A <<= i;
    return foo;
}

Foo2 Foo2::operator<<(signed long l)
{
    return *this << (signed int) l;
}

char Foo2::testCharMarshalling(char c)
{
    return c;
}

void Foo2::testKeywordParam(void* where, Bar::Item event, int ref)
{
}

Bar::Bar()
{
}

Bar::Bar(const Foo* foo)
{
}

Bar::Bar(Foo foo)
{
}

Bar::Item Bar::RetItem1() const
{
    return Bar::Item1;
}

Bar* Bar::returnPointerToValueType()
{
    return this;
}

bool Bar::operator ==(const Bar& arg1) const
{
    return A == arg1.A && B == arg1.B;
}

bool operator ==(Bar::Item item, const Bar& bar)
{
    return item == bar.RetItem1();
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

int Hello::AddFooPtrRef(Foo*& foo)
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

bool Hello::TestPrimitiveInOut(CS_IN_OUT int* i)
{
    *i += 10;
    return true;
}

bool Hello::TestPrimitiveInOutRef(CS_IN_OUT int& i)
{
    i += 10;
    return true;
}

void Hello::EnumOut(int value, CS_OUT Enum* e)
{
    *e = (Enum)value;
}

void Hello::EnumOutRef(int value, CS_OUT Enum& e)
{
    e = (Enum)value;
}

void Hello::EnumInOut(CS_IN_OUT Enum* e)
{
        if (*e == Enum::E)
                *e = Enum::F;
}

void Hello::EnumInOutRef(CS_IN_OUT Enum& e)
{
        if (e == Enum::E)
                e = Enum::F;
}

void Hello::StringOut(CS_OUT const char** str)
{
        *str = "HelloStringOut";
}

void Hello::StringOutRef(CS_OUT const char*& str)
{
        str = "HelloStringOutRef";
}

void Hello::StringInOut(CS_IN_OUT const char** str)
{
        if (strcmp(*str, "Hello") == 0)
                *str = "StringInOut";
        else
                *str = "Failed";
}

void Hello::StringInOutRef(CS_IN_OUT const char*& str)
{
        if (strcmp(str, "Hello") == 0)
                str = "StringInOutRef";
        else
                str = "Failed";
}

void Hello::StringTypedef(const TypedefChar* str)
{
}

int unsafeFunction(const Bar& ret, char* testForString, void (*foo)(int))
{
    return ret.A;
}

const wchar_t* wcharFunction(const wchar_t* constWideChar)
{
    return constWideChar;
}

Bar indirectReturn()
{
    return Bar();
}

int TestDelegates::Double(int N)
{
    return N * 2;
}

int TestDelegates::Triple(int N)
{
    return N * 3;
}

int TestDelegates::StdCall(DelegateStdCall del)
{
    return del(1);
}

int TestDelegates::CDecl(DelegateCDecl del)
{
    return del(1);
}

AbstractFoo::~AbstractFoo()
{
}

int ImplementsAbstractFoo::pureFunction(typedefInOverride i)
{
    return 5;
}

int ImplementsAbstractFoo::pureFunction1()
{
    return 10;
}

int ImplementsAbstractFoo::pureFunction2(bool* ok)
{
    return 15;
}

ReturnsAbstractFoo::ReturnsAbstractFoo() {}

const AbstractFoo& ReturnsAbstractFoo::getFoo()
{
    return i;
}

Exception::~Exception()
{
}

Ex2* DerivedException::clone()
{
    return 0;
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

int test(common& s)
{
    return 5;
}

Bar::Item operator |(Bar::Item left, Bar::Item right)
{
    return left | right;
}

void va_listFunction(va_list v)
{
}

TestNestedTypes::TestNestedTypes()
{
}

TestNestedTypes::~TestNestedTypes()
{
}

void TestDelegates::MarshalUnattributedDelegate(DelegateInGlobalNamespace del)
{
}

int TestDelegates::MarshalAnonymousDelegate(int (*del)(int n))
{
    return del(1);
}

void TestDelegates::MarshalAnonymousDelegate2(int (*del)(int n))
{
}

void TestDelegates::MarshalAnonymousDelegate3(float (*del)(float n))
{
}

int f(int n)
{
    return n * 2;
}

int (*TestDelegates::MarshalAnonymousDelegate4())(int n)
{
    return f;
}

int TestDelegates::MarshalAnonymousDelegate5(int (STDCALL *del)(int))
{
    return del(2);
}

int TestDelegates::MarshalAnonymousDelegate6(int (STDCALL *del)(int))
{
    return del(3);
}

ClassA::ClassA(int value)
{
    Value = value;
}

ClassB::ClassB(const ClassA &x)
{
    Value = x.Value;
}

ClassC::ClassC(const ClassA *x)
{
    Value = x->Value;
}

ClassC::ClassC(const ClassB &x)
{
    Value = x.Value;
}

ClassD::ClassD(int value)
    : Field(value)
{
}

void DelegateNamespace::Nested::f1(void (*)())
{
}

void TestDelegates::MarshalDelegateInAnotherUnit(DelegateInAnotherUnit del)
{
}

DelegateNullCheck TestDelegates::MarshalNullDelegate()
{
    return nullptr;
}

void DelegateNamespace::f2(void (*)())
{
}

HasStdString::HasStdString()
{
}

HasStdString::~HasStdString()
{
}

std::string HasStdString::testStdString(const std::string& s)
{
    return s + "_test";
}

std::string HasStdString::testStdStringPassedByValue(std::string s)
{
    return s + "_test";
}

std::string& HasStdString::getStdString()
{
    return s;
}

SomeNamespace::AbstractClass::~AbstractClass()
{
}

TestProperties::TestProperties() : Field(0), _refToPrimitiveInSetter(0),
    _getterAndSetterWithTheSameName(0), _setterReturnsBoolean(0), _virtualSetterReturnsBoolean(0)
{
}

TestProperties::TestProperties(const TestProperties& other) : Field(other.Field),
    FieldValue(other.FieldValue),
    _refToPrimitiveInSetter(other._refToPrimitiveInSetter),
    _getterAndSetterWithTheSameName(other._getterAndSetterWithTheSameName),
    _setterReturnsBoolean(other._setterReturnsBoolean),
    _virtualSetterReturnsBoolean(other._virtualSetterReturnsBoolean)
{
}

int TestProperties::getFieldValue()
{
    return Field;
}

void TestProperties::setFieldValue(int Value)
{
    Field = Value;
}

bool TestProperties::isVirtual()
{
    return false;
}

void TestProperties::setVirtual(bool value)
{
}

double TestProperties::refToPrimitiveInSetter() const
{
    return _refToPrimitiveInSetter;
}

void TestProperties::setRefToPrimitiveInSetter(const double& value)
{
    _refToPrimitiveInSetter = value;
}

int TestProperties::getterAndSetterWithTheSameName()
{
    return _getterAndSetterWithTheSameName;
}

void TestProperties::getterAndSetterWithTheSameName(int value)
{
    _getterAndSetterWithTheSameName = value;
}

void TestProperties::set(int value)
{
}

int TestProperties::setterReturnsBoolean()
{
    return _setterReturnsBoolean;
}

bool TestProperties::setterReturnsBoolean(int value)
{
    bool changed = _setterReturnsBoolean != value;
    _setterReturnsBoolean = value;
    return changed;
}

int TestProperties::virtualSetterReturnsBoolean()
{
    return _virtualSetterReturnsBoolean;
}

bool TestProperties::setVirtualSetterReturnsBoolean(int value)
{
    bool changed = _virtualSetterReturnsBoolean != value;
    _virtualSetterReturnsBoolean = value;
    return changed;
}

int TestProperties::nestedEnum()
{
    return 5;
}

int TestProperties::nestedEnum(int i)
{
    return i;
}

int TestProperties::get32Bit()
{
    return 10;
}

bool TestProperties::isEmpty()
{
    return empty();
}

bool TestProperties::empty()
{
    return false;
}

int TestProperties::virtualGetter()
{
    return 15;
}

int TestProperties::startWithVerb()
{
    return 25;
}

void TestProperties::setStartWithVerb(int value)
{
}

void TestProperties::setSetterBeforeGetter(bool value)
{
}

bool TestProperties::isSetterBeforeGetter()
{
    return true;
}

bool TestProperties::contains(char c)
{
    return true;
}

bool TestProperties::contains(const char* str)
{
    return true;
}

HasOverridenSetter::HasOverridenSetter()
{
}

void HasOverridenSetter::setVirtual(bool value)
{
}

int HasOverridenSetter::virtualSetterReturnsBoolean()
{
    return TestProperties::virtualSetterReturnsBoolean();
}

bool HasOverridenSetter::setVirtualSetterReturnsBoolean(int value)
{
    return TestProperties::setVirtualSetterReturnsBoolean(value);
}

int HasOverridenSetter::virtualGetter()
{
    return 20;
}

void HasOverridenSetter::setVirtualGetter(int value)
{
}

TypeMappedIndex::TypeMappedIndex()
{
}

Bar& TestIndexedProperties::operator[](const Foo& key)
{
    return bar;
}

InternalCtorAmbiguity::InternalCtorAmbiguity(void* param)
{
    // cause a crash to indicate this is the incorrect ctor to invoke
    throw;
}

InvokesInternalCtorAmbiguity::InvokesInternalCtorAmbiguity() : ptr(0)
{
}

InternalCtorAmbiguity* InvokesInternalCtorAmbiguity::InvokeInternalCtor()
{
    return ptr;
}

HasFriend::HasFriend(int m)
{
    this->m = m;
}

int HasFriend::getM()
{
    return m;
}

DLL_API const HasFriend operator+(const HasFriend& f1, const HasFriend& f2)
{
    return HasFriend(f1.m + f2.m);
}

DLL_API const HasFriend operator-(const HasFriend& f1, const HasFriend& f2)
{
    return HasFriend(f1.m - f2.m);
}

DifferentConstOverloads::DifferentConstOverloads() : i(5)
{
}

int DifferentConstOverloads::getI() const
{
    return i;
}

bool DifferentConstOverloads::operator ==(const DifferentConstOverloads& other)
{
    return i == other.i;
}

bool DifferentConstOverloads::operator !=(const DifferentConstOverloads& other)
{
    return i != other.i;
}

bool DifferentConstOverloads::operator ==(int number) const
{
    return i == number;
}

bool DifferentConstOverloads::operator ==(std::string s) const
{
    return i == s.length();
}

bool operator ==(const DifferentConstOverloads& d, const char* s)
{
    return d.getI() == strlen(s);
}

int HasVirtualProperty::getProperty()
{
    return 1;
}

void HasVirtualProperty::setProperty(int target)
{
}

int HasVirtualProperty::getProtectedProperty()
{
    return 2;
}

void HasVirtualProperty::setProtectedProperty(int value)
{
}

ChangedAccessOfInheritedProperty::ChangedAccessOfInheritedProperty()
{
}

int ChangedAccessOfInheritedProperty::getProperty()
{
    return 2;
}

void ChangedAccessOfInheritedProperty::setProperty(int value)
{
}

int ChangedAccessOfInheritedProperty::getProtectedProperty()
{
    return 3;
}

void ChangedAccessOfInheritedProperty::setProtectedProperty(int value)
{
}

Empty ReturnsEmpty::getEmpty()
{
    return Empty();
}

void funcTryRefTypePtrOut(CS_OUT RefTypeClassPassTry* classTry)
{
}

void funcTryRefTypeOut(CS_OUT RefTypeClassPassTry classTry)
{
}

void funcTryValTypePtrOut(CS_OUT ValueTypeClassPassTry* classTry)
{
}

void funcTryValTypeOut(CS_OUT ValueTypeClassPassTry classTry)
{
}

HasProblematicFields::HasProblematicFields() : b(false), c(0)
{
}

HasVirtualReturningHasProblematicFields::HasVirtualReturningHasProblematicFields()
{
}

HasProblematicFields HasVirtualReturningHasProblematicFields::returnsProblematicFields()
{
    return HasProblematicFields();
}

int BaseClassVirtual::retInt(const Foo1& foo)
{
    return 1;
}

BaseClassVirtual BaseClassVirtual::getBase()
{
    return DerivedClassVirtual();
}

int DerivedClassVirtual::retInt(const Foo2& foo)
{
    return 2;
}

DerivedClassAbstractVirtual::~DerivedClassAbstractVirtual()
{
}

DerivedClassOverrideAbstractVirtual::DerivedClassOverrideAbstractVirtual()
{
}

int DerivedClassOverrideAbstractVirtual::retInt(const Foo& foo)
{
    return 1;
}

BufferForVirtualFunction::BufferForVirtualFunction()
{
}

OverridesNonDirectVirtual::OverridesNonDirectVirtual()
{
}

int OverridesNonDirectVirtual::retInt(const Foo& foo)
{
    return 3;
}

AbstractWithVirtualDtor::AbstractWithVirtualDtor()
{
}

AbstractWithVirtualDtor::~AbstractWithVirtualDtor()
{
}

NonTrivialDtorBase::NonTrivialDtorBase()
{
}

NonTrivialDtorBase::~NonTrivialDtorBase()
{
}

NonTrivialDtor::NonTrivialDtor()
{
    dtorCalled = false;
}

NonTrivialDtor::~NonTrivialDtor()
{
    dtorCalled = true;
}

DerivedFromTemplateInstantiationWithVirtual::DerivedFromTemplateInstantiationWithVirtual()
{
}

HasProtectedEnum::HasProtectedEnum()
{
}

void HasProtectedEnum::function(ProtectedEnum param)
{
}

void FuncWithTypeAlias(custom_int_t i)
{
}

void FuncWithTemplateTypeAlias(TypeAliasTemplate<int> i)
{
}

HasAbstractOperator::~HasAbstractOperator()
{
}

HasOverloadsWithDifferentPointerKindsToSameType::HasOverloadsWithDifferentPointerKindsToSameType()
{
}

HasOverloadsWithDifferentPointerKindsToSameType::~HasOverloadsWithDifferentPointerKindsToSameType()
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(int& i)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(int&& i)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(const int& i)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(const Foo& rx, int from)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(Foo& rx, int from)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(const Foo2& rx, int from)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::overload(Foo2&& rx, int from)
{
}

void HasOverloadsWithDifferentPointerKindsToSameType::dispose()
{
}

void hasPointerParam(Foo* foo, int i)
{
}

void hasPointerParam(const Foo& foo)
{
}

void sMallFollowedByCapital()
{
}

TestStaticClass& TestStaticClass::operator=(const TestStaticClass& oth)
{
    return *this;
}

HasCopyAndMoveConstructor::HasCopyAndMoveConstructor(int value)
{
    field = value;
}

HasCopyAndMoveConstructor::HasCopyAndMoveConstructor(const HasCopyAndMoveConstructor &other)
{
    field = other.field;
}

HasCopyAndMoveConstructor::HasCopyAndMoveConstructor(HasCopyAndMoveConstructor&& other)
{
    field = other.field;
}

HasCopyAndMoveConstructor::~HasCopyAndMoveConstructor()
{
}

int HasCopyAndMoveConstructor::getField()
{
    return field;
}

HasVirtualFunctionsWithStringParams::HasVirtualFunctionsWithStringParams()
{
}

HasVirtualFunctionsWithStringParams::~HasVirtualFunctionsWithStringParams()
{
}

ImplementsVirtualFunctionsWithStringParams::ImplementsVirtualFunctionsWithStringParams()
{
}

ImplementsVirtualFunctionsWithStringParams::~ImplementsVirtualFunctionsWithStringParams()
{
}

void ImplementsVirtualFunctionsWithStringParams::PureVirtualFunctionWithStringParams(std::string testString1, std::string testString2)
{
}

int HasVirtualFunctionsWithStringParams::VirtualFunctionWithStringParam(std::string testString)
{
    return 5;
}

HasVirtualFunctionWithBoolParams::HasVirtualFunctionWithBoolParams()
{
}

HasVirtualFunctionWithBoolParams::~HasVirtualFunctionWithBoolParams()
{
}

bool HasVirtualFunctionWithBoolParams::virtualFunctionWithBoolParamAndReturnsBool(bool testBool)
{
    return testBool;
}

SecondaryBaseWithIgnoredVirtualMethod::SecondaryBaseWithIgnoredVirtualMethod()
{
}

SecondaryBaseWithIgnoredVirtualMethod::~SecondaryBaseWithIgnoredVirtualMethod()
{
}

void SecondaryBaseWithIgnoredVirtualMethod::generated()
{
}

void SecondaryBaseWithIgnoredVirtualMethod::ignored(const IgnoredType& ignoredParam)
{
}

DerivedFromSecondaryBaseWithIgnoredVirtualMethod::DerivedFromSecondaryBaseWithIgnoredVirtualMethod()
{
}

DerivedFromSecondaryBaseWithIgnoredVirtualMethod::~DerivedFromSecondaryBaseWithIgnoredVirtualMethod()
{
}

void DerivedFromSecondaryBaseWithIgnoredVirtualMethod::generated()
{
}

void DerivedFromSecondaryBaseWithIgnoredVirtualMethod::ignored(const IgnoredType& ignoredParam)
{
}

AmbiguousParamNames::AmbiguousParamNames(int instance, int in)
{
}

AmbiguousParamNames::~AmbiguousParamNames()
{
}

HasPropertyNamedAsParent::HasPropertyNamedAsParent()
{
}

HasPropertyNamedAsParent::~HasPropertyNamedAsParent()
{
}

void integerOverload(int i)
{
}

void integerOverload(unsigned int i)
{
}

void integerOverload(long i)
{
}

void integerOverload(unsigned long i)
{
}

void takeReferenceToVoidStar(const void*& p)
{
}

void takeVoidStarStar(void** p)
{
}

void overloadPointer(void* p, int i)
{
}

void overloadPointer(const void* p, int i)
{
}

const char* takeReturnUTF8(const char* utf8)
{
    UTF8 = utf8;
    return UTF8.data();
}

StructWithCopyCtor::StructWithCopyCtor() {}
StructWithCopyCtor::StructWithCopyCtor(const StructWithCopyCtor& other) : mBits(other.mBits) {}

uint16_t TestStructWithCopyCtorByValue(StructWithCopyCtor s)
{
    return s.mBits;
}

BaseCovariant::~BaseCovariant()
{
}

DerivedCovariant::~DerivedCovariant()
{
}
