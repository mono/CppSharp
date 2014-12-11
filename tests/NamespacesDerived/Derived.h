#include "../Tests.h"
#include "../NamespacesBase/Base.h"

class DLL_API Derived : public Base
{
public:
    Derived();

    Base component;

private:
    int d;
};