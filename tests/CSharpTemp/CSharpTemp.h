#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

class DLL_API Foo
{
public:
    Foo();
    int operator[](int i) const;
    int operator[](unsigned int i);
    int& operator[](int i);
    int A;

protected:
    int P;
};

class DLL_API Bar
{
public:
    const Foo& operator[](int i) const;
    Foo& operator[](int i);

private:
    Foo m_foo;
};
