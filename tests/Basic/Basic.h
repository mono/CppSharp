#include "../Tests.h"

#ifdef _WIN32
#include <vadefs.h>
#endif
#include <string>

class DLL_API IgnoredType
{
    class IgnoredNested
    {
    private:
        int i;
    };
private:
    int i;
};

class DLL_API Foo
{
private:
    enum Private
    {
        Value1,
        Value2
    };
public:

    Foo();
    Foo(Private p);
    int A;
    float B;
    IgnoredType ignoredType;
    int fixedArray[3];
    void* ptr;
    static const int unsafe = 10;

	const char* GetANSI();
	// TODO: VC++ does not support char16
	// char16 chr16;

    // Not properly handled yet - ignore
    float nested_array[2][2];
    // Primitive pointer types
    const int* SomePointer;
    const int** SomePointerPointer;

    typedef Foo* FooPtr;

    void TakesTypedefedPtr(FooPtr date);

    bool operator ==(const Foo& other) const;
};

struct DLL_API Bar
{
    enum Item
    {
        Item1,
        Item2
    };

    Bar();
    Item RetItem1();
    int A;
    float B;

    Bar* returnPointerToValueType();

    bool operator ==(const Bar& other) const;
};

class DLL_API Foo2 : public Foo
{
    struct Copy {
        Foo A;
    }* copy;

public:

    Foo2();

    int C;

    Foo2 operator<<(signed int i);
    Foo2 operator<<(signed long l);
    Bar valueTypeField;
    char testCharMarshalling(char c);
    void testKeywordParam(void* where);
};

DLL_API Bar::Item operator |(Bar::Item left, Bar::Item right);

struct DLL_API Bar2 : public Bar
{
    // Conversion operators

    struct DLL_API Nested
    {
        operator int() const;
    };

    operator int() const;
    operator Foo2();
    Foo2 needFixedInstance() const;

    typedef void *Bar2::*FunctionPointerResolvedAsVoidStar;
    operator FunctionPointerResolvedAsVoidStar() const { return 0; }

    int C;
    Bar* pointerToStruct;
    int* pointerToPrimitive;
    Foo2* pointerToClass;
    Bar valueStruct;
};

enum Enum
{
    A = 0, B = 2, C = 5,
    //D = 0x80000000,
    E = 0x1,
    F = -9
};

class DLL_API Hello
{
    union NestedPrivate {
        int i;
        float f;
    };

public:
    union NestedPublic {
        int j;
        float g;
        long l;
    };

    Hello ();
    Hello(const Hello& hello);

    void PrintHello(const char* s);
    bool test1(int i, float f);
    int add(int a, int b);

    int AddFoo(Foo);
    int AddFooRef(Foo&);
    int AddFooPtr(Foo*);
    int AddFooPtrRef(Foo*&);
    Foo RetFoo(int a, float b);

    int AddFoo2(Foo2);

    int AddBar(Bar);
    int AddBar2(Bar2);

    int RetEnum(Enum);
    Hello* RetNull();

    bool TestPrimitiveOut(CS_OUT float* f);
    bool TestPrimitiveOutRef(CS_OUT float& f);

    bool TestPrimitiveInOut(CS_IN_OUT int* i);
    bool TestPrimitiveInOutRef(CS_IN_OUT int& i);

    void EnumOut(int value, CS_OUT Enum* e);
    void EnumOutRef(int value, CS_OUT Enum& e);

    void EnumInOut(CS_IN_OUT Enum* e);
    void EnumInOutRef(CS_IN_OUT Enum& e);

    void StringOut(CS_OUT const char** str);
    void StringOutRef(CS_OUT const char*& str);
    void StringInOut(CS_IN_OUT const char** str);
    void StringInOutRef(CS_IN_OUT const char*& str);
};

class DLL_API AbstractFoo
{
public:
    virtual int pureFunction(int i) = 0;
    virtual int pureFunction1() = 0;
    virtual int pureFunction2() = 0;
};

class DLL_API ImplementsAbstractFoo : public AbstractFoo
{
public:
    virtual int pureFunction(int i);
    virtual int pureFunction1();
    virtual int pureFunction2();
};

class DLL_API ReturnsAbstractFoo
{
public:
    ReturnsAbstractFoo();
    const AbstractFoo& getFoo();

private:
    ImplementsAbstractFoo i;
};

int DLL_API unsafeFunction(const Bar& ret, char* testForString, void (*foo)(int));

DLL_API Bar indirectReturn();

// Tests CheckVirtualOverrideReturnCovariance
struct Exception;
typedef Exception Ex1;

struct DerivedException;
typedef DerivedException Ex2;

struct DLL_API Exception
{
    virtual Ex1* clone() = 0;
};

struct DLL_API DerivedException : public Exception
{
    virtual Ex2* clone() override { return 0; }
};

// Tests for ambiguous call to native functions with default parameters
struct DLL_API DefaultParameters
{
    void Foo(int a, int b = 0);
    void Foo(int a);

    void Bar() const;
    void Bar();
};

// The Curiously Recurring Template Pattern (CRTP)
template<class Derived>
class Base
{
    // methods within Base can use template to access members of Derived
    Derived* create() { return new Derived(); }
};

class Derived : public Base<Derived>
{
};

// Tests the MoveFunctionToClassPass
class DLL_API basic
{

};

DLL_API int test(basic& s);

// Tests the MoveOperatorToClassPass
struct DLL_API TestMoveOperatorToClass
{
    TestMoveOperatorToClass();
    int A;
    int B;
};

TestMoveOperatorToClass::TestMoveOperatorToClass() {}

DLL_API int operator *(TestMoveOperatorToClass klass, int b)
{
    return klass.A * b;
}

DLL_API TestMoveOperatorToClass operator-(const TestMoveOperatorToClass& b)
{
    TestMoveOperatorToClass nb;
    nb.A = -b.A;
    nb.B = -b.B;
    return nb;
}

DLL_API TestMoveOperatorToClass operator+(const TestMoveOperatorToClass& b1,
                                          const TestMoveOperatorToClass& b2)
{
    TestMoveOperatorToClass b;
    b.A = b1.A + b2.A;
    b.B = b1.B + b2.B;
    return b;
}

// Not a valid operator overload for Foo2 in managed code - comparison operators need to return bool.
DLL_API int operator==(const Foo2& a, const Foo2& b)
{
	return 0;
}

// Tests delegates
typedef int (*DelegateInGlobalNamespace)(int);
typedef int (STDCALL *DelegateStdCall)(int);
typedef int (CDECL *DelegateCDecl)(int n);

struct DLL_API TestDelegates
{
    typedef int (*DelegateInClass)(int);
    typedef int(TestDelegates::*MemberDelegate)(int);

    TestDelegates();
    static int Double(int N) { return N * 2; }
    int Triple(int N) { return N * 3; }

    int StdCall(DelegateStdCall del) { return del(1); }
    int CDecl(DelegateCDecl del) { return del(1); }
    void MarshalUnattributedDelegate(DelegateInGlobalNamespace del);

    DelegateInClass A;
    DelegateInGlobalNamespace B;
    // As long as we can't marshal them make sure they're ignored
    MemberDelegate C;
};

TestDelegates::TestDelegates() : A(Double), B(Double), C(&TestDelegates::Triple)
{
}

// Tests memory leaks in constructors
//  C#:  Marshal.FreeHGlobal(arg0);
struct DLL_API TestMemoryLeaks
{
    TestMemoryLeaks(const char* name) {}
};

// Tests that finalizers are generated
/* CLI: ~TestFinalizers() */
struct DLL_API TestFinalizers
{
};

// Tests static classes
struct DLL_API TestStaticClass
{
    static int Add(int a, int b);

	static int GetOneTwoThree() { return 123; }

protected:

	static int _Mult(int a, int b) { return a * b; }

	static int GetFourFiveSix() { return 456; }

private:
    TestStaticClass();
};

int TestStaticClass::Add(int a, int b) { return a + b; }

struct DLL_API TestStaticClassDerived : TestStaticClass
{
    static int Foo();

private:
    TestStaticClassDerived();
};

int TestStaticClassDerived::Foo() { return 0; }

class DLL_API TestNotStaticClass
{
public:
	static TestNotStaticClass StaticFunction();
private:
	TestNotStaticClass();
};

TestNotStaticClass::TestNotStaticClass()
{
}

TestNotStaticClass TestNotStaticClass::StaticFunction()
{
	return TestNotStaticClass();
}

class HasIgnoredField
{
    Base<Derived> fieldOfIgnoredType;
};

template <typename T>
class DependentTypeWithNestedIndependent
{
    union
    {
        int i;
        long l;
    };
};

class DLL_API TestCopyConstructorRef
{
public:
    TestCopyConstructorRef();
    TestCopyConstructorRef(const TestCopyConstructorRef& other);
    int A;
    float B;
};

TestCopyConstructorRef::TestCopyConstructorRef()
{
}

TestCopyConstructorRef::TestCopyConstructorRef(const TestCopyConstructorRef& other)
{
    A = other.A;
    B = other.B;
}

template <class T>
struct EmptyNamedNestedEnum
{
    enum { Value = 10 };
};

typedef unsigned long foo_t;
typedef struct DLL_API SomeStruct
{
	SomeStruct();
	foo_t p;
} SomeStruct;

SomeStruct::SomeStruct() : p(1) {}

class DLL_API SomeClassExtendingTheStruct : public SomeStruct
{
};

namespace SomeNamespace
{
	class DLL_API AbstractClass
	{
	public:
		virtual void AbstractMethod() = 0;
	};
}

// Test operator overloads
class DLL_API ClassWithOverloadedOperators
{
public:
    ClassWithOverloadedOperators();

	operator char();
	operator int();
	operator short();

	virtual bool operator<(const ClassWithOverloadedOperators &other) const;
};

ClassWithOverloadedOperators::ClassWithOverloadedOperators() {}
ClassWithOverloadedOperators::operator char() { return 1; }
ClassWithOverloadedOperators::operator int() { return 2; }
ClassWithOverloadedOperators::operator short() { return 3; }
bool ClassWithOverloadedOperators::
     operator<(const ClassWithOverloadedOperators &other) const {
     return true;
}

// Tests global static function generation
DLL_API int Function()
{
    return 5;
}

// Tests properties
struct DLL_API TestProperties
{
    TestProperties();
    int Field;

    int getFieldValue();
    void setFieldValue(int Value);
};

TestProperties::TestProperties() : Field(0) {}
int TestProperties::getFieldValue() { return Field; }
void TestProperties::setFieldValue(int Value) { Field = Value; }

class DLL_API TestIndexedProperties
{
    foo_t p;
    TestProperties f;
public:
    TestIndexedProperties();
    // Should lead to a read/write indexer with return type uint
    foo_t& operator[](int i);
    // Should lead to a read/write indexer with return type uint
    foo_t* operator[](float f);
    // Should lead to a read-only indexer with return type uint
    foo_t operator[](const char* name);
    // Should lead to a read-only indexer with return type uint*
    const foo_t& operator[](double d);
    // Should lead to a read/write indexer with return type TestProperties
    TestProperties* operator[](unsigned char b);
    // Should lead to a read-only indexer with return type TestProperties
    const TestProperties& operator[](short b);
    // Should lead to a read-only indexer with argument type TestProperties
    foo_t operator[](TestProperties b);
};

TestIndexedProperties::TestIndexedProperties() : p(1), f() {}
foo_t& TestIndexedProperties::operator[](int i) { return p; }
foo_t TestIndexedProperties::operator[](const char* name) { return p; }
foo_t* TestIndexedProperties::operator[](float f) { return &p; }
const foo_t& TestIndexedProperties::operator[](double f) { return p; }
TestProperties* TestIndexedProperties::operator[](unsigned char b) { return &f; }
const TestProperties& TestIndexedProperties::operator[](short b) { return f; }
foo_t TestIndexedProperties::operator[](TestProperties b) { return p; }

struct DLL_API TestIndexedPropertiesInValueType
{
public:
    int operator[](int i);
};

int TestIndexedPropertiesInValueType::operator[](int i) { return i; }

// Tests variables
struct DLL_API TestVariables
{
	static int VALUE;
	void SetValue(int value = VALUE);
};

int TestVariables::VALUE;
void TestVariables::SetValue(int value) { VALUE = value; }

typedef const wchar_t * LPCWSTR;
struct DLL_API TestWideStrings
{
	LPCWSTR GetWidePointer();
};

LPCWSTR TestWideStrings::GetWidePointer() { return L"Hello"; }

enum struct MyEnum { A, B, C };

class DLL_API TestArraysPointers
{
public:
    TestArraysPointers(MyEnum *values, int count);

    MyEnum Value;
};

TestArraysPointers::TestArraysPointers(MyEnum *values, int count)
{
    if (values && count) Value = values[0];
}

struct DLL_API TestGetterSetterToProperties
{
    int getWidth();
    int getHeight();
};

int TestGetterSetterToProperties::getWidth() { return 640; }
int TestGetterSetterToProperties::getHeight() { return 480; }

// Tests conversion operators of classes
class DLL_API ClassA 
{
public:
    ClassA(int value) { Value = value; }
    int Value;
};
class DLL_API ClassB
{
public:
    // conversion from ClassA (constructor):
    ClassB(const ClassA& x) { Value = x.Value; }
    int Value;
    // conversion from ClassA (assignment):
    //ClassB& operator= (const ClassA& x) { return *this; }
    // conversion to ClassA (type-cast operator)
    //operator ClassA() { return ClassA(); }
};
class DLL_API ClassC
{
public:
    // This should NOT lead to a conversion
    ClassC(const ClassA* x) { Value = x->Value; }
    // This should lead to an explicit conversion
    explicit ClassC(const ClassB& x) { Value = x.Value; }
    int Value;
};

// Test decltype
int Expr = 0;
DLL_API decltype(Expr) TestDecltype()
{
    return Expr;
}

DLL_API void TestNullPtrType(decltype(nullptr))
{
}

DLL_API decltype(nullptr) TestNullPtrTypeRet()
{
    return nullptr;
}

// Tests dependent name types
template<typename T> struct DependentType
{
    DependentType(typename T::Dependent* t) { }
};

class PureDtor
{
public:
    virtual ~PureDtor() = 0;
};

DLL_API void va_listFunction(va_list v);

struct DLL_API TestNestedTypes
{
public:
    struct
    {
        struct
        {
        };
    };

    union as_types
    {
        int as_int;
        struct uchars
        {
            unsigned char blue, green, red, alpha;
        } as_uchar;
    };
};

class DLL_API HasStdString
{
    // test if these are ignored with the C# back-end
public:
    std::string testStdString(std::string s);
    std::string s;
};

class DLL_API InternalCtorAmbiguity
{
public:
    InternalCtorAmbiguity(void* param);
};

class DLL_API InvokesInternalCtorAmbiguity
{
public:
    InvokesInternalCtorAmbiguity();
    InternalCtorAmbiguity* InvokeInternalCtor();
private:
    InternalCtorAmbiguity* ptr;
};

class DLL_API HasFriend
{
public:
    HasFriend(int m);
    DLL_API friend inline const HasFriend operator+(const HasFriend& f1, const HasFriend& f2);
    int getM();
private:
    int m;
};

template<typename T> class FriendTemplate
{
    template<typename TT>
    friend FriendTemplate<TT> func(const FriendTemplate<TT>&);

    friend FriendTemplate;
    friend class FriendTemplate;

    template<typename TT>
    friend class FriendTemplate;
};

class DLL_API DifferentConstOverloads
{
public:
    bool operator ==(const DifferentConstOverloads& other);
    bool operator ==(int number) const;
};
