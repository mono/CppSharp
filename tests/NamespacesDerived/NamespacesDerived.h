#include "../Tests.h"
#include "../NamespacesBase/NamespacesBase.h"

namespace OverlappingNamespace
{

    class InDerivedLib
    {
    public:
        InDerivedLib();
        Base parentNSComponent;
        Colors color;
    };
}

class DLL_API Derived : public Base
{
public:
    Derived();

    Base baseComponent;

    OverlappingNamespace::InBaseLib nestedNSComponent;

    OverlappingNamespace::Colors color;

private:
    int d;
};
