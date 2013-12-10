#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

#define CS_OUT

class DLL_API Foo
{
public:

    Foo();
    int A;
    float B;

	const char* GetANSI();
};

class DLL_API Foo2 : public Foo
{
    struct Copy {
        Foo A;
    }* copy;

public:

    int C;

    Foo2 operator<<(signed int i);
    Foo2 operator<<(signed long l);
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
};

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
};

enum Enum
{
    A = 0, B = 2, C = 5,
    D = 0x80000000,
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
    };

    Hello ();
    Hello(const Hello& hello);

    void PrintHello(const char* s);
    bool test1(int i, float f);
    int add(int a, int b);

    int AddFoo(Foo);
    int AddFooRef(Foo&);
    int AddFooPtr(Foo*);
    Foo RetFoo(int a, float b);

    int AddFoo2(Foo2);

    int AddBar(Bar);
    int AddBar2(Bar2);

    int RetEnum(Enum);
    Hello* RetNull();

    bool TestPrimitiveOut(CS_OUT float* f);
    bool TestPrimitiveOutRef(CS_OUT float& f);
};

class DLL_API AbstractFoo
{
public:
    virtual int pureFunction(int i) = 0;
    virtual int pureFunction1() = 0;
    virtual int pureFunction2() = 0;
    typedef void (*QTextStreamFunction)(int &);
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
    const AbstractFoo& getFoo();

private:
    ImplementsAbstractFoo i;
};

DLL_API Bar operator-(const Bar &);
DLL_API Bar operator+(const Bar &, const Bar &);

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

