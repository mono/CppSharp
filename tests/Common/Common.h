#include "../Tests.h"
#include "AnotherUnit.h"

#ifdef _WIN32
#include <vadefs.h>
#endif
#include <string>
#include <vector>

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
    enum
    {
        EmptyEnum1,
        EmptyEnum2
    };

    class NestedAbstract
    {
    public:
        virtual int* abstractFunctionInNestedClass() = 0;
    };

    Foo();
    Foo(Private p);
    int A;
    float B;
    IgnoredType ignoredType;
    int fixedArray[3];
    char fixedCharArray[3];
    void* ptr;
    static const int unsafe;
    static const char charArray[];

    const char* GetANSI();

    // Not properly handled yet - ignore
    float nested_array[2][2];
    // Primitive pointer types
    const int* SomePointer;
    const int** SomePointerPointer;

    typedef Foo* FooPtr;

    void TakesTypedefedPtr(FooPtr date);
    int TakesRef(const Foo& other);

    bool operator ==(const Foo& other) const;
};

// HACK: do not move these to the cpp - C++/CLI is buggy and cannot link static fields initialised in the cpp
const int Foo::unsafe = 10;
const char Foo::charArray[] = "abc";

struct DLL_API Bar
{
    enum Item
    {
        Item1,
        Item2
    };

    Bar();
    Bar(Foo foo);
    explicit Bar(const Foo* foo);
    Item RetItem1() const;
    int A;
    float B;
    Item fixedEnumArray[3];

    Bar* returnPointerToValueType();

    bool operator ==(const Bar& arg1) const;
};

DLL_API bool operator ==(Bar::Item item, const Bar& bar);

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
    void testKeywordParam(void* where, Bar::Item event, int ref);
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

typedef char TypedefChar;

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

    void StringTypedef(const TypedefChar* str);
};

class DLL_API AbstractFoo
{
public:
    virtual int pureFunction(int i = 0) = 0;
    virtual int pureFunction1() = 0;
    virtual int pureFunction2(bool* ok = 0) = 0;
};

class DLL_API ImplementsAbstractFoo : public AbstractFoo
{
public:
    typedef int typedefInOverride;
    virtual int pureFunction(typedefInOverride i = 0);
    virtual int pureFunction1();
    virtual int pureFunction2(bool* ok = 0);
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

struct DLL_API Exception : public Foo
{
    virtual Ex1* clone() = 0;
};

struct DLL_API DerivedException : public Exception
{
    virtual Ex2* clone() override;
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
class DLL_API common
{

};

DLL_API int test(common& s);

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
    static int Double(int N);
    int Triple(int N);

    int StdCall(DelegateStdCall del);
    int CDecl(DelegateCDecl del);
    void MarshalUnattributedDelegate(DelegateInGlobalNamespace del);

    int MarshalAnonymousDelegate(int (*del)(int n));
    void MarshalAnonymousDelegate2(int (*del)(int n));
    void MarshalAnonymousDelegate3(float (*del)(float n));
    int (*MarshalAnonymousDelegate4())(int n);

    void MarshalDelegateInAnotherUnit(DelegateInAnotherUnit del);

    DelegateInClass A;
    DelegateInGlobalNamespace B;
    // As long as we can't marshal them make sure they're ignored
    MemberDelegate C;
};

TestDelegates::TestDelegates() : A(Double), B(Double), C(&TestDelegates::Triple)
{
}

namespace DelegateNamespace
{
    namespace Nested
    {
        void DLL_API f1(void (*)());
    }

    void DLL_API f2(void (*)());
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

	static int GetOneTwoThree();

protected:

	static int _Mult(int a, int b);

	static int GetFourFiveSix();

private:
    TestStaticClass();
};

int TestStaticClass::Add(int a, int b) { return a + b; }

int TestStaticClass::GetOneTwoThree() { return 123; }

int TestStaticClass::_Mult(int a, int b) { return a * b; }

int TestStaticClass::GetFourFiveSix() { return 456; }

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
    T array[1];

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

class DLL_API TypeMappedIndex
{
public:
    TypeMappedIndex();
};

class DLL_API TestIndexedProperties
{
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
    Bar& operator[](unsigned long i);
    Bar& operator[](const TypeMappedIndex& key);
    // Test that we do not generate 'ref int' parameters as C# does not allow it
    int operator[](CS_OUT char key);

private:
    foo_t p;
    TestProperties f;
    Bar bar;
};

TestIndexedProperties::TestIndexedProperties() : p(1), f() {}
foo_t& TestIndexedProperties::operator[](int i) { return p; }
foo_t TestIndexedProperties::operator[](const char* name) { return p; }
foo_t* TestIndexedProperties::operator[](float f) { return &p; }
const foo_t& TestIndexedProperties::operator[](double f) { return p; }
TestProperties* TestIndexedProperties::operator[](unsigned char b) { return &f; }
const TestProperties& TestIndexedProperties::operator[](short b) { return f; }
foo_t TestIndexedProperties::operator[](TestProperties b) { return p; }
Bar& TestIndexedProperties::operator[](unsigned long i)
{
    return bar;
}
Bar& TestIndexedProperties::operator[](const TypeMappedIndex& key)
{
    return bar;
}
int TestIndexedProperties::operator[](CS_OUT char key)
{
    return key;
}


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
    LPCWSTR GetWideNullPointer();
};

LPCWSTR TestWideStrings::GetWidePointer() { return L"Hello"; }
LPCWSTR TestWideStrings::GetWideNullPointer() { return 0; }

enum struct MyEnum { A, B, C };

typedef void (*VoidPtrRetFunctionTypedef) ();

class DLL_API TestFixedArrays
{
public:
    TestFixedArrays();
    VoidPtrRetFunctionTypedef Array[10];    
};

TestFixedArrays::TestFixedArrays() {}

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
    ClassA(int value);
    int Value;
};
class DLL_API ClassB
{
public:
    // conversion from ClassA (constructor):
    ClassB(const ClassA& x);
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
    ClassC(const ClassA* x);
    // This should lead to an explicit conversion
    explicit ClassC(const ClassB& x);
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

private:
    struct Bitset { int length : sizeof(T); };
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
    struct
    {
        struct
        {
        };
    };
    struct
    {
        struct
        {
        };
    };
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
    DLL_API friend const HasFriend operator+(const HasFriend& f1, const HasFriend& f2);
    DLL_API friend const HasFriend operator-(const HasFriend& f1, const HasFriend& f2);
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
    DifferentConstOverloads();
    bool operator ==(const DifferentConstOverloads& other);
    bool operator !=(const DifferentConstOverloads& other);
    bool operator ==(int number) const;
private:
    int i;
};

class TestNamingAnonymousTypesInUnion
{
public:
    union {
        struct {
        } argb;
        struct {
        } ahsv;
        struct {
        } acmyk;
    } ct;
};

class DLL_API RefTypeClassPassTry { };

void DLL_API funcTryRefTypePtrOut(CS_OUT RefTypeClassPassTry* classTry);
void DLL_API funcTryRefTypeOut(CS_OUT RefTypeClassPassTry classTry);

#define ARRAY_LENGTH 5
#define CS_VALUE_TYPE
struct CS_VALUE_TYPE ValueTypeArrays
{
	float firstValueTypeArrray[ARRAY_LENGTH];
	int secondValueTypeArray[ARRAY_LENGTH];
	char thirdValueTypeArray[ARRAY_LENGTH];
	size_t size;
};

class DLL_API HasVirtualProperty
{
public:
    virtual int getProperty();
    virtual void setProperty(int target);
};

class DLL_API ChangedAccessOfInheritedProperty : public HasVirtualProperty
{
public:
    ChangedAccessOfInheritedProperty();
protected:
    int getProperty();
    void setProperty(int value);
};

class DLL_API Empty
{
};

class DLL_API ReturnsEmpty
{
public:
    Empty getEmpty();
};

class DLL_API CS_VALUE_TYPE ValueTypeClassPassTry { };

void DLL_API funcTryValTypePtrOut(CS_OUT ValueTypeClassPassTry* classTry);
void DLL_API funcTryValTypeOut(CS_OUT ValueTypeClassPassTry classTry);

class DLL_API HasProblematicFields
{
public:
    HasProblematicFields();
    bool b;
    char c;
};

class DLL_API HasVirtualReturningHasProblematicFields
{
public:
    HasVirtualReturningHasProblematicFields();
    virtual HasProblematicFields returnsProblematicFields();
};

class DLL_API BaseClassVirtual
{
public:
    typedef Foo Foo1;
    virtual int retInt(const Foo1& foo);
    static BaseClassVirtual getBase();
};

class DLL_API DerivedClassVirtual : public BaseClassVirtual
{
public:
    typedef Foo Foo2;
    virtual int retInt(const Foo2& foo);
};

class DLL_API DerivedClassAbstractVirtual : public DerivedClassVirtual
{
public:
    virtual int retInt(const Foo& foo) = 0;
};

class DLL_API DerivedClassOverrideAbstractVirtual : public DerivedClassAbstractVirtual
{
public:
    DerivedClassOverrideAbstractVirtual();
    virtual int retInt(const Foo& foo);
};

class DLL_API BufferForVirtualFunction : public BaseClassVirtual
{
public:
    BufferForVirtualFunction();
};

class DLL_API OverridesNonDirectVirtual : public BufferForVirtualFunction
{
public:
    OverridesNonDirectVirtual();
    virtual int retInt(const Foo& foo);
};

namespace boost
{
    template <class T>         struct is_member_pointer_cv         { static const bool value = false; };
    template <class T, class U>struct is_member_pointer_cv<T U::*> { static const bool value = true; };

    // all of this below tests corner cases with type locations
    template<class T>
    struct make_tuple_traits
    {
        typedef T type;

        // commented away, see below  (JJ)
        //  typedef typename IF<
        //  boost::is_function<T>::value,
        //  T&,
        //  T>::RET type;
    };

    namespace detail
    {
        struct swallow_assign;
        typedef void (detail::swallow_assign::*ignore_t)();
        struct swallow_assign
        {
            swallow_assign(ignore_t(*)(ignore_t));
            template<typename T>
            swallow_assign const& operator=(const T&) const;
        };

        swallow_assign::swallow_assign(ignore_t (*)(ignore_t))
        {
        }

        template<typename T>
        swallow_assign const& swallow_assign::operator=(const T&) const
        {
            return *this;
        }

    } // namespace detail

    template<>
    struct make_tuple_traits<detail::ignore_t(detail::ignore_t)>
    {
        typedef detail::swallow_assign type;
    };

    template<class T>
    struct is_class_or_union
    {
        template <class U>
        static char is_class_or_union_tester(void(U::*)(void));
    };
}

template <std::size_t N, std::size_t... I>
struct build_index_impl : build_index_impl<N - 1, N - 1, I...> {};

template <typename T>
class AbstractTemplate
{
public:
    AbstractTemplate();
    virtual void abstractFunction() = 0;
};

template <typename T>
AbstractTemplate<T>::AbstractTemplate()
{
}

class DLL_API AbstractWithVirtualDtor
{
public:
    AbstractWithVirtualDtor();
    virtual ~AbstractWithVirtualDtor();
    virtual void abstract() = 0;
};

class DLL_API NonTrivialDtorBase
{
public:
    NonTrivialDtorBase();
    ~NonTrivialDtorBase();
};

class DLL_API NonTrivialDtor : public NonTrivialDtorBase
{
public:
    NonTrivialDtor();
    ~NonTrivialDtor();
    static bool getDtorCalled();
    static void setDtorCalled(bool value);
private:
    static bool dtorCalled;
};

// HACK: do not move these to the cpp - C++/CLI is buggy and cannot link static fields initialised in the cpp
bool NonTrivialDtor::dtorCalled = false;

bool NonTrivialDtor::getDtorCalled()
{
    return true;
}

void NonTrivialDtor::setDtorCalled(bool value)
{
    dtorCalled = true;
}

template <class T> class ForwardedTemplate;

ForwardedTemplate<int> returnsForwardedTemplate();

template <class T> class ForwardedTemplate
{
    ForwardedTemplate<T> functionInForwardedTemplate() const;
};

template <class T>
ForwardedTemplate<T> ForwardedTemplate<T>::functionInForwardedTemplate() const
{
    return ForwardedTemplate<T>();
}

template <typename T>
class TemplateWithVirtual
{
public:
    TemplateWithVirtual();
    virtual void v();
};

template <class T>
TemplateWithVirtual<T>::TemplateWithVirtual()
{
}

template <class T>
void TemplateWithVirtual<T>::v()
{
}

template <typename T>
int FunctionTemplateWithDependentTypeDefaultExpr(size_t size = sizeof(T)) {
	return size;
}

class DLL_API DerivedFromTemplateInstantiationWithVirtual : public TemplateWithVirtual<int>
{
public:
    DerivedFromTemplateInstantiationWithVirtual();
};

typedef DLL_API union
{
    int c;
} union_t;

int DLL_API func_union(union_t u)
{
    return u.c;
}

class DLL_API HasProtectedEnum
{
public:
    HasProtectedEnum();
protected:
    enum class ProtectedEnum
    {
        Member1,
        Member2
    };
    void function(ProtectedEnum param);
};

using custom_int_t = int;
DLL_API void FuncWithTypeAlias(custom_int_t i);

template<typename T>
using TypeAliasTemplate = TemplateWithVirtual<T>;
DLL_API void FuncWithTemplateTypeAlias(TypeAliasTemplate<int> i);

struct TestsTypes
{
    int(*FunctionNoProto)();
};

template <class T>
struct SpecialisesVoid
{
private:
    T t;
};

template <class T>
class SpecialisesVoidInUnion
{
    union {
        SpecialisesVoid<T>* e;
    }* u;
};

class UsesSpecialisationOfVoid
{
private:
    SpecialisesVoid<void>* s;
    SpecialisesVoidInUnion<void>* h;
    SpecialisesVoid<int> i;
    SpecialisesVoid<long> l;
    SpecialisesVoid<unsigned int> u;
};

class DLL_API HasAbstractOperator
{
public:
    virtual bool operator==(const HasAbstractOperator& other) = 0;
};

template<size_t _Len, class _Ty>
struct _Aligned;

template<size_t _Len>
struct _Aligned<_Len, int>
{
    typedef int type;
};

template<size_t _Len>
struct _Aligned<_Len, char>
{
    typedef typename _Aligned<_Len, int>::type type;
};

typedef _Aligned<16, char>::type type;

template <typename T, template <typename> class InteriorRings = SpecialisesVoid>
struct polygon
{
    InteriorRings<T> interior_rings;
};

class HasSystemBase : public std::string
{
};

typedef SpecialisesVoid<std::vector<std::string>> SpecialisesWithNestedSystemTypes;

struct HasLongDoubles
{
    long double simple;
    long double array[4];
};

enum
{
    EmptyEnumsWithSameMemberPrefix1,
    EmptyEnumsWithSameMemberPrefix2
};

enum
{
    EmptyEnumsWithSameMemberPrefix3,
    EmptyEnumsWithSameMemberPrefix4
};

enum
{
    EmptyEnumsWithSameMemberPrefixAndUnderscore_1,
    EmptyEnumsWithSameMemberPrefixAndUnderscore_2
};

enum
{
    EmptyEnumsWithSameMemberPrefixAndUnderscore_3,
    EmptyEnumsWithSameMemberPrefixAndUnderscore_4
};

class DLL_API HasOverloadsWithDifferentPointerKindsToSameType
{
public:
    HasOverloadsWithDifferentPointerKindsToSameType();
    ~HasOverloadsWithDifferentPointerKindsToSameType();
    void overload(int& i);
    void overload(int&& i);
    void overload(const int& i);
};

DLL_API void hasPointerParam(Foo* foo, int i);
DLL_API void hasPointerParam(const Foo& foo);
