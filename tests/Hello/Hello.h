//#include <string>

#if defined(_MSC_VER)
#define CppSharp_API __declspec(dllexport)
#else
#define CppSharp_API
#endif

class CppSharp_API Foo
{
public:

    Foo();
    int A;
    float B;
};

class CppSharp_API Foo2 : public Foo
{
public:

    int C;
};

struct CppSharp_API Bar
{
    Bar();
    int A;
    float B;
};

struct CppSharp_API Bar2 : public Bar
{
    int C;
};

enum Enum
{
    A = 0, B = 2, C = 5
};

class CppSharp_API Hello
{
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
