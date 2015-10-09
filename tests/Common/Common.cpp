#include "Common.h"
#include <string.h>

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

const int Foo::unsafe;

const char* Foo::GetANSI()
{
	return "ANSI";
}

void Foo::TakesTypedefedPtr(FooPtr date)
{
}

bool Foo::operator ==(const Foo& other) const
{
    return A == other.A && B == other.B;
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

Bar::Bar(Foo foo)
{
}

Bar::Bar(const Foo* foo)
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

bool Bar::operator ==(const Bar& other) const
{
    return A == other.A && B == other.B;
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

int ImplementsAbstractFoo::pureFunction(int i)
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

void DelegateNamespace::Nested::f1(void (*)())
{
}

void TestDelegates::MarshalDelegateInAnotherUnit(DelegateInAnotherUnit del)
{
}

void DelegateNamespace::f2(void (*)())
{
}

std::string HasStdString::testStdString(std::string s)
{
    return s + "_test";
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

bool DifferentConstOverloads::operator ==(const DifferentConstOverloads& other)
{
    return true;
}

bool DifferentConstOverloads::operator ==(int number) const
{
    return false;
}

int HasVirtualProperty::getProperty()
{
    return 1;
}

void HasVirtualProperty::setProperty(int target)
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

int BaseClassVirtual::retInt()
{
	return 1;
}

BaseClassVirtual BaseClassVirtual::getBase()
{
	return DerivedClassVirtual();
}

int DerivedClassVirtual::retInt()
{
	return 2;
}

DerivedClassOverrideAbstractVirtual::DerivedClassOverrideAbstractVirtual()
{
}

int DerivedClassOverrideAbstractVirtual::retInt()
{
    return 1;
}

BufferForVirtualFunction::BufferForVirtualFunction()
{
}

OverridesNonDirectVirtual::OverridesNonDirectVirtual()
{
}

int OverridesNonDirectVirtual::retInt()
{
    return 3;
}
