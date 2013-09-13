#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

class DLL_API Foo
{
public:
    const char* Unicode;

    // TODO: move this, it has nothing to do with Unicode, it's here only not to break the CLI branch
    int operator[](int i) const;
    char* operator[](int i);
};

DLL_API int FooCallFoo(Foo* foo);
