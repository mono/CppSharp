#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

class DLL_API Foo
{
public:
    Foo();
    int method();
    int operator[](int i) const;
    int operator[](unsigned int i);
    int& operator[](int i);
    int A;

protected:
    int P;
};

class DLL_API Qux
{
public:
    int farAwayFunc() const;
};

class DLL_API Bar : public Qux
{
public:
    int method();
    const Foo& operator[](int i) const;
    Foo& operator[](int i);

private:
    Foo m_foo;
};

class DLL_API Baz : public Foo, public Bar
{
public:
    int takesQux(const Qux& qux);
};
