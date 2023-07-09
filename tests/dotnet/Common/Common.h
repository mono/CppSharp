#include "../Tests.h"
#include "AnotherUnit.h"

#ifdef _WIN32
#include <vadefs.h>
#endif
#include <string>
#include <vector>
#include <memory>

class DLL_API TestPacking
{
public:
    int i1;
    int i2;
    bool b;
};

#pragma pack(1)
class DLL_API TestPacking1: public TestPacking
{
};

#pragma pack(2)
class DLL_API TestPacking2: public TestPacking
{
};

#pragma pack(4)
class DLL_API TestPacking4: public TestPacking
{
};

#pragma pack(8)
class DLL_API TestPacking8: public TestPacking
{
};
#pragma pack()

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

    class DLL_API NestedAbstract
    {
    public:
        virtual ~NestedAbstract();
        virtual int* abstractFunctionInNestedClass() = 0;
    };

    Foo();
    Foo(const Foo& other);
    Foo(Private p);
    Foo(const float& f);
    int A;
    float B;
    int Init = 20;
    IgnoredType ignoredType;
    int fixedArray[3];
    char fixedCharArray[3];
    void* ptr;
    static const int unsafe;
    static const char charArray[];
    static int readWrite;

    const char* GetANSI();

    // Not properly handled yet - ignore
    float nested_array[2][2];
    // Primitive pointer types
    const int* SomePointer;
    const int** SomePointerPointer;

    typedef Foo* FooPtr;

    typedef uint8_t* typedefPrimitivePointer;
    typedefPrimitivePointer fieldOfTypedefPrimitivePointer;

    void TakesTypedefedPtr(FooPtr date);
    int TakesRef(const Foo& other);

    bool operator ==(const Foo& other) const;

    int fooPtr();
    char16_t returnChar16();

    static Foo staticField;
};

struct DLL_API Bar
{
    enum Item
    {
        Item1,
        Item2
    };

    Bar();
    explicit Bar(const Foo* foo);
    Bar(Foo foo);
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
    Foo2(const Foo2& other);

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
    F = -9,
    NAME_A = 10,
    NAME__A = 20
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

    bool TestPrimitiveInOut(int* i);
    bool TestPrimitiveInOutRef(int& i);

    void EnumOut(int value, CS_OUT Enum* e);
    void EnumOutRef(int value, CS_OUT Enum& e);

    void EnumInOut(Enum* e);
    void EnumInOutRef(Enum& e);

    void StringOut(CS_OUT const char** str);
    void StringOutRef(CS_OUT const char*& str);
    void StringInOut(CS_IN_OUT const char** str);
    void StringInOutRef(CS_IN_OUT const char*& str);

    void StringTypedef(const TypedefChar* str);
};

class DLL_API AbstractFoo
{
public:
    virtual ~AbstractFoo();
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
private:
    virtual int pureFunction2(bool* ok = 0);
};

class DLL_API ReturnsAbstractFoo
{
public:
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
    virtual ~Exception();
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

DLL_API int operator *(TestMoveOperatorToClass klass, int b);

DLL_API TestMoveOperatorToClass operator-(const TestMoveOperatorToClass& b);

DLL_API TestMoveOperatorToClass operator+(const TestMoveOperatorToClass& b1,
    const TestMoveOperatorToClass& b2);

// Not a valid operator overload for Foo2 in managed code - comparison operators need to return bool.
DLL_API int operator==(const Foo2& a, const Foo2& b);

// Tests delegates
typedef int (*DelegateInGlobalNamespace)(int);
typedef int (STDCALL *DelegateStdCall)(int);
typedef int (CDECL *DelegateCDecl)(int n);
typedef void(*DelegateNullCheck)(void);

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
    int MarshalAnonymousDelegate5(int (STDCALL *del)(int n));
    int MarshalAnonymousDelegate6(int (STDCALL *del)(int n));

    void MarshalDelegateInAnotherUnit(DelegateInAnotherUnit del);

    DelegateNullCheck MarshalNullDelegate();

    DelegateInClass A;
    DelegateInGlobalNamespace B;
    // As long as we can't marshal them make sure they're ignored
    MemberDelegate C;
};

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

    TestStaticClass& operator=(const TestStaticClass& oth);

private:

    static int _Mult(int a, int b);

    static int GetFourFiveSix();

    TestStaticClass();
};

struct DLL_API TestStaticClassDerived : TestStaticClass
{
    static int Foo();

private:
    TestStaticClassDerived();
};

class DLL_API TestNotStaticClass
{
public:
    static TestNotStaticClass StaticFunction();
private:
    TestNotStaticClass();
};

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

class DLL_API SomeClassExtendingTheStruct : public SomeStruct
{
};

namespace SomeNamespace
{
    class DLL_API NamespacedAbstractClass
    {
    public:
        virtual void AbstractMethod() = 0;
    };

    class DLL_API NamespacedAbstractImpl
    {
    public:
        virtual void AbstractMethod();
    };

    class Inlines
    {
    public:
        constexpr Inlines(float param, const char* name) {}
        inline operator NamespacedAbstractImpl () const { return NamespacedAbstractImpl(); }
    protected:
        void protectedInlined() {}
    };

    constexpr Inlines constWithParams(5.f, "test");

    class AbstractInlines
    {
    public:
        virtual void hasVariadicArgs(int regular, ...) = 0;
    };
}

// Test operator overloads
class DLL_API ClassWithOverloadedOperators
{
public:
    operator char();
    operator int();
    operator short();

    virtual bool operator<(const ClassWithOverloadedOperators &other) const;
};

// Tests global static function generation
DLL_API int Function();

// Tests properties
struct DLL_API TestProperties
{
public:
    enum class NestedEnum
    {
        Value1,
        Value2
    };

    enum class Conflict
    {
        Value1,
        Value2
    };

    TestProperties();
    TestProperties(const TestProperties& other);
    TestProperties& operator=(const TestProperties& other);
    int Field;
    const int& ConstRefField;

    int getFieldValue();
    void setFieldValue(int Value);

    bool isVirtual();
    virtual void setVirtual(bool value);

    double refToPrimitiveInSetter() const;
    void setRefToPrimitiveInSetter(const double& value);

    int getterAndSetterWithTheSameName();
    void getterAndSetterWithTheSameName(int value);

    int Get() const;
    void Set(int value);

    int get() const;
    void set(int value);

    int setterReturnsBoolean();
    bool setSetterReturnsBoolean(int newValue);

    virtual int virtualSetterReturnsBoolean();
    virtual bool setVirtualSetterReturnsBoolean(int newValue);

    int nestedEnum();
    int nestedEnum(int i);

    int get32Bit();
    bool isEmpty();
    bool empty();

    virtual int virtualGetter();

    int startWithVerb();
    void setStartWithVerb(int value);

    void setSetterBeforeGetter(bool value);
    bool isSetterBeforeGetter();

    bool contains(char c);
    bool contains(const char* str);

    Conflict GetConflict();
    void SetConflict(Conflict _conflict);

    virtual int(*getCallback())(int);
    virtual void setCallback(int(*value)(int));

    int GetArchiveName() const;

protected:
    const int ArchiveName;

private:
    int FieldValue;
    double _refToPrimitiveInSetter;
    int _getterAndSetterWithTheSameName;
    int _setterReturnsBoolean;
    int _virtualSetterReturnsBoolean;
    Conflict _conflict;
    int(*_callback)(int);
};

class DLL_API HasOverridenSetter : public TestProperties
{
public:
    void setVirtual(bool value) override;

    int virtualSetterReturnsBoolean() override;
    bool setVirtualSetterReturnsBoolean(int value) override;

    int virtualGetter() override;
    void setVirtualGetter(int value);
};

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
    Bar& operator[](const Foo& key);
    // Test that we do not generate 'ref int' parameters as C# does not allow it
    int operator[](CS_OUT char key);

private:
    foo_t p;
    TestProperties f;
    Bar bar;
};

struct DLL_API TestIndexedPropertiesInValueType
{
public:
    int operator[](int i);
};

// Tests variables
struct DLL_API TestVariables
{
    static int VALUE;
    void SetValue(int value = VALUE);
};

typedef const wchar_t * LPCWSTR;
struct DLL_API TestWideStrings
{
    LPCWSTR GetWidePointer();
    LPCWSTR GetWideNullPointer();
};

enum struct MyEnum { A, B, C };

typedef void (*VoidPtrRetFunctionTypedef) ();

class DLL_API TestFixedArrays
{
public:
    VoidPtrRetFunctionTypedef Array[10];
#ifndef _MSC_VER
    TestWideStrings ZeroSizedClassArray[0];
    MyEnum ZeroSizedEnumArray[0];
#endif
    int ZeroSizedArray[0];
};

class DLL_API TestArraysPointers
{
public:
    TestArraysPointers(MyEnum *values, int count);

    MyEnum Value;
};

class DLL_API NonPrimitiveType
{
public:
    int GetFoo();

    int foo;
};

class DLL_API TestFixedNonPrimitiveArrays
{
public:
    NonPrimitiveType NonPrimitiveTypeArray[3];
};

struct DLL_API TestGetterSetterToProperties
{
    int getWidth();
    int getHeight();
};

// Tests conversion operators of classes
class DLL_API ClassA
{
public:
    ClassA(int value);
    ClassA(const ClassA& other, bool param = true);
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

class DLL_API ClassD
{
public:
    ClassD(int value);
    // Accessing this field should return reference, not a copy.
    ClassA Field;
};

// Test decltype
int Expr = 0;
DLL_API decltype(Expr) TestDecltype();

DLL_API void TestNullPtrType(decltype(nullptr));

DLL_API decltype(nullptr) TestNullPtrTypeRet();

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

class PureImplementedDtor
{
public:
    virtual ~PureImplementedDtor() = 0;
};

PureImplementedDtor::~PureImplementedDtor()
{
}

DLL_API void va_listFunction(va_list v);

struct DLL_API TestNestedTypes
{
public:
    union as_types
    {
        int as_int;
        struct uchars
        {
            unsigned char blue, green, red, alpha;
        } as_uchar;
    };
    int toVerifyCorrectLayoutBefore;
    union
    {
        int i;
        char c;
    };
    int toVerifyCorrectLayoutAfter;
};

class DLL_API HasStdString
{
public:
    std::string testStdString(const std::string& s);
    std::string testStdStringPassedByValue(std::string s);
    std::string s;
    std::string& getStdString();
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

    template<typename TT>
    friend class FriendTemplate;
};

class DLL_API DifferentConstOverloads
{
public:
    DifferentConstOverloads();
    int getI() const;
    bool operator ==(const DifferentConstOverloads& other);
    bool operator !=(const DifferentConstOverloads& other);
    bool operator ==(int number) const;
    bool operator ==(std::string s) const;
private:
    int i;
};

DLL_API bool operator ==(const DifferentConstOverloads& d, const char* s);

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

class DLL_API RefTypeClassPassTry
{
};

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
protected:
    virtual int getProtectedProperty();
    virtual void setProtectedProperty(int value);
};

class DLL_API ChangedAccessOfInheritedProperty : public HasVirtualProperty
{
public:
    int getProtectedProperty();
    void setProtectedProperty(int value);
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
    virtual int retInt(const Foo& foo);
};

class DLL_API BufferForVirtualFunction : public BaseClassVirtual
{
};

class DLL_API OverridesNonDirectVirtual : public BufferForVirtualFunction
{
public:
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
    virtual ~AbstractWithVirtualDtor();
    virtual void abstract() = 0;
};

class DLL_API NonTrivialDtorBase
{
public:
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
};

typedef union
{
    int c;
} union_t;

int DLL_API func_union(union_t u);

class DLL_API HasProtectedEnum
{
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
    void overload(int& i);
    void overload(int&& i);
    void overload(const int& i);
    void overload(const Foo& rx, int from = -1);
    void overload(Foo& rx, int from = -1);
    void overload(const Foo2& rx, int from = -1);
    void overload(Foo2&& rx, int from = -1);
    void dispose();
};

DLL_API void hasPointerParam(Foo* foo, int i);
DLL_API void hasPointerParam(const Foo& foo);

enum EmptyEnum { };

enum __enum_with_underscores { lOWER_BEFORE_CAPITAL, CAPITALS_More, underscore_at_end_, usesDigits1_0 };

void DLL_API sMallFollowedByCapital();

class DLL_API HasCopyAndMoveConstructor
{
public:
    HasCopyAndMoveConstructor(int value);
    HasCopyAndMoveConstructor(const HasCopyAndMoveConstructor& other);
    HasCopyAndMoveConstructor(HasCopyAndMoveConstructor&& other);
    int getField();
private:
    int field;
};

class DLL_API HasVirtualFunctionsWithStringParams
{
public:
    virtual void PureVirtualFunctionWithStringParams(std::string testString1, std::string testString2) = 0;
    virtual int VirtualFunctionWithStringParam(std::string testString);
};

class DLL_API ImplementsVirtualFunctionsWithStringParams : public HasVirtualFunctionsWithStringParams
{
public:
    virtual void PureVirtualFunctionWithStringParams(std::string testString1, std::string testString2);
};

class DLL_API HasVirtualFunctionWithBoolParams
{
public:
    virtual bool virtualFunctionWithBoolParamAndReturnsBool(bool testBool);
};

class DLL_API HasProtectedCtorWithProtectedParam
{
protected:
    enum ProtectedEnum
    {
        Member
    };
    HasProtectedCtorWithProtectedParam(ProtectedEnum protectedParam);
};

class DLL_API SecondaryBaseWithIgnoredVirtualMethod
{
public:
    // HACK: do not delete: work around https://github.com/mono/CppSharp/issues/1534
    ~SecondaryBaseWithIgnoredVirtualMethod();
    virtual void generated();
    virtual void ignored(const IgnoredType& ignoredParam);
};

class DLL_API DerivedFromSecondaryBaseWithIgnoredVirtualMethod : public Foo, public SecondaryBaseWithIgnoredVirtualMethod
{
public:
    // HACK: do not delete: work around https://github.com/mono/CppSharp/issues/1534
    ~DerivedFromSecondaryBaseWithIgnoredVirtualMethod();
    void generated();
    void ignored(const IgnoredType& ignoredParam);
};

class DLL_API AmbiguousParamNames
{
public:
    AmbiguousParamNames(int instance, int in);
};

class DLL_API HasPropertyNamedAsParent
{
public:
    int hasPropertyNamedAsParent;
};

class DLL_API ReturnByValueWithReturnParam
{
public:
    int getUseCount();

private:
    std::shared_ptr<int> _ptr = std::shared_ptr<int>(new int[1]);
};

class DLL_API ReturnByValueWithReturnParamFactory
{
public:
    static ReturnByValueWithReturnParam generate();
};

struct DLL_API NestedUnionWithNested
{
    union
    {
        struct
        {
            int nestedField1;
            int nestedField2;
        };
        int unionField;
    };
};

template<typename T> void TemplatedFunction(T type)
{

}

inline namespace InlineNamespace
{
    void FunctionInsideInlineNamespace()
    {

    }
}

union
{
    struct
    {
        struct
        {
            long Capabilities;
        } Server;
        struct
        {
            long Capabilities;
        } Share;
    } Smb2;
} ProtocolSpecific;


template<class _Other>
using UsingTemplatePtr = _Other *;

struct TemplateWithUsingTemplateMember
{
    UsingTemplatePtr<TemplateWithUsingTemplateMember> _Ref;
};

namespace hasUnnamedDecl
{
    extern "C"
    {
    }
}

enum ItemsDifferByCase
{
    Case_a,
    Case_A
};

template <typename T> struct MyListBase
{
protected:
    ~MyListBase() {}
};

template <typename T>
class MyList : public MyListBase<T>
{
public:
    inline MyList() { }
};

template <> struct MyListBase<int>
{
};

class MyIntList : public MyList<int>
{
    inline MyIntList(MyList<int> &&l) { }
};

void MyFunc(MyList<void *> *list);

template<class T> using InvokeGenSeq = typename T::Type;

template<int N> struct DerivedTypeAlias;
template<int N> using TypeAlias = InvokeGenSeq<DerivedTypeAlias<N>>;

template<int N>
struct DerivedTypeAlias : TypeAlias<N / 2> {};

DLL_API ImplementsAbstractFoo freeFunctionReturnsVirtualDtor();
DLL_API void integerOverload(int i);
DLL_API void integerOverload(unsigned int i);
DLL_API void integerOverload(long i);
DLL_API void integerOverload(unsigned long i);
DLL_API void takeReferenceToVoidStar(const void*& p);
DLL_API void takeVoidStarStar(void** p);
DLL_API void overloadPointer(void* p, int i = 0);
DLL_API void overloadPointer(const void* p, int i = 0);
DLL_API const char* takeReturnUTF8(const char* utf8);
typedef const char* LPCSTR;
DLL_API LPCSTR TakeTypedefedMappedType(LPCSTR string);
DLL_API std::string UTF8;

typedef enum SE4IpAddr_Tag {
    V4,
    V6,
} SE4IpAddr_Tag;

typedef struct {
    uint8_t _0[4];
} SE4V4_Body;

typedef struct {
    uint8_t _0[16];
} SE4V6_Body;

typedef struct {
    SE4IpAddr_Tag tag;
    union {
        SE4V4_Body v4;
        SE4V6_Body v6;
    };
} SE4IpAddr;

struct DLL_API StructWithCopyCtor
{
    StructWithCopyCtor();
    StructWithCopyCtor(const StructWithCopyCtor& other);
    uint16_t mBits;
};

uint16_t DLL_API TestStructWithCopyCtorByValue(StructWithCopyCtor s);

// Issue: https://github.com/mono/CppSharp/issues/1266
struct BaseCovariant;
typedef std::unique_ptr<BaseCovariant> PtrCovariant;

struct DLL_API BaseCovariant {
    virtual ~BaseCovariant();
    virtual PtrCovariant clone() const = 0;
};

struct DLL_API DerivedCovariant: public BaseCovariant {
    virtual ~DerivedCovariant();
  std::unique_ptr<BaseCovariant> clone() const override {
    return PtrCovariant(new DerivedCovariant());
  }
};

// Issue: https://github.com/mono/CppSharp/issues/1268
template <typename T>
class AbstractClassTemplate {
  public:
    virtual void func() = 0;
};

class DerivedClass: public AbstractClassTemplate<int> {
  public:
    void func() override {}
};

// Issue: https://github.com/mono/CppSharp/issues/1235
#include <functional>

template <typename X, typename Y>
class TemplateClassBase {
  public:
    using XType = X;
};

template <typename A, typename B = A>
class TemplateClass : TemplateClassBase<A,B> {
  public:
    using typename TemplateClassBase<A,B>::XType;
    using Func = std::function<B(XType)>;
    explicit TemplateClass(Func function) {}
};

template <typename T>
class QScopedPointer
{
public:
    typedef T* QScopedPointer::* RestrictedBool;
    operator RestrictedBool()
    {
    }
};

template <typename T>
struct dependentVariable { static const size_t var = alignof(T); };

class QObjectData {
};

QScopedPointer<QObjectData> d_ptr;

struct DLL_API PointerToTypedefPointerTest
{
    int val;
};
typedef PointerToTypedefPointerTest *LPPointerToTypedefPointerTest;

void DLL_API PointerToTypedefPointerTestMethod(LPPointerToTypedefPointerTest* lp, int valToSet);

typedef int *LPINT;

void DLL_API PointerToPrimitiveTypedefPointerTestMethod(LPINT lp, int valToSet);

// this name must match a universally accessible system function or class to reproduce the bug
struct system
{
    int32_t field1;
    int32_t field2;
};

extern "C"
{
    DLL_API void takeConflictName(struct system* self);
    DLL_API struct system freeFunctionReturnByValue();
} // extern "C"

void DLL_API FunctionWithFlagsAsDefaultParameter(int defaultParam = A | B);
