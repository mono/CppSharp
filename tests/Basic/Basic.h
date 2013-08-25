#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

class DLL_API Foo
{
public:

    Foo();
    int A;
    float B;
};

class DLL_API Foo2 : public Foo
{
    struct Copy {
        Foo A;
    }* copy;

public:

    int C;

    Foo2& operator<<(signed int i);
    Foo2& operator<<(signed long l);
};

struct DLL_API Bar
{
    Bar();
    int A;
    float B;
};

struct DLL_API Bar2 : public Bar
{
    int C;
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
    union {
        int i;
        float b;
    };

public:
    Hello ();

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
};

DLL_API Bar operator-(const Bar &);
DLL_API Bar operator+(const Bar &, const Bar &);

int DLL_API unsafeFunction(const Bar& ret, char* testForString, void (*foo)(int));

DLL_API Bar indirectReturn();
