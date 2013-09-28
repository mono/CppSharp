#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

class DLL_API Foo
{
public:
    class Vfoo
    {

    };

    Foo();
    virtual int vfoo();
    virtual int vbar();
};

DLL_API int FooCallFoo(Foo* foo);
