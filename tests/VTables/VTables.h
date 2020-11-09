#include "../Tests.h"
#include <string>

class DLL_API Foo
{
public:
    class Vfoo
    {

    };

    Foo();
    ~Foo();
    virtual int vfoo();
    virtual int vbar();

    virtual int append();
    virtual int append(int a);
    int callVirtualWithParameter(int a);

    DISABLE_WARNING_ONCE(4251,
        std::string s;
    )
};

DLL_API int FooCallFoo(Foo* foo);

class DLL_API BaseClassVirtual
{
public:
    BaseClassVirtual();
    BaseClassVirtual(const BaseClassVirtual& other);
    static int virtualCallRetInt(BaseClassVirtual* base);
    virtual int retInt();
    static BaseClassVirtual getBase();
    static BaseClassVirtual* getBasePtr();
    static const char* getTypeName();
};

class DLL_API DerivedClassVirtual : public BaseClassVirtual
{
public:
    DerivedClassVirtual();
    virtual int retInt() override;
};
