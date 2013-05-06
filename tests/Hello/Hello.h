//#include <string>

#if defined(_MSC_VER)
#define CXXI_API __declspec(dllexport)
#else
#define CXXI_API
#endif

class CXXI_API Foo
{
public:

    Foo();
    int A;
    float B;
};

class CXXI_API Foo2 : public Foo
{
public:

    int C;
};

struct CXXI_API Bar
{
    Bar();
    int A;
    float B;
};

struct CXXI_API Bar2 : public Bar
{
    int C;
};

enum class Enum
{
    A = 0, B = 2, C = 5
};

class CXXI_API Hello
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
