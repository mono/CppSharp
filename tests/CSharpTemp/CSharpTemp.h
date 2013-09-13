#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

class DLL_API Foo
{
public:
    int operator[](int i) const;
    int operator[](int i);

protected:
    int P;
};
