#include "../Tests.h"
#include "../NamespacesBase/NamespacesBase.h"

class DLL_API Derived : public Base
{
public:
    Derived();

    Base component;

private:
    int d;
};