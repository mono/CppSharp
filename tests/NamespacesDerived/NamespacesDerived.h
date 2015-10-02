#include "../Tests.h"
#include "../NamespacesBase/NamespacesBase.h"

// Namespace clashes with NamespacesBase.OverlappingNamespace
// Test whether qualified names turn out right.
namespace OverlappingNamespace
{

    class InDerivedLib
    {
    public:
        InDerivedLib();
        Base parentNSComponent;
        ColorsEnum color;
    };
}


// Using a type imported from a different library.
class DLL_API Derived : public Base2
{
public:
    Derived();

    Base baseComponent;
    Base getBase();
    void setBase(Base);

    void parent(int i);

    OverlappingNamespace::InBaseLib nestedNSComponent;
    OverlappingNamespace::InBaseLib getNestedNSComponent();
    void setNestedNSComponent(OverlappingNamespace::InBaseLib);

    OverlappingNamespace::ColorsEnum color;

private:
    int d;
};

// For reference: using a type derived in the same library
class Base3
{
};

template <typename T> class TemplateClass;

class Derived2 : public Base3
{
public:
    Base3 baseComponent;
    Base3 getBase();
    void setBase(Base3);

    OverlappingNamespace::InDerivedLib nestedNSComponent;
    OverlappingNamespace::InDerivedLib getNestedNSComponent();
    void setNestedNSComponent(OverlappingNamespace::InDerivedLib);
    void defaultEnumValueFromDependency(OverlappingNamespace::ColorsEnum c = OverlappingNamespace::ColorsEnum::black);

    Abstract* getAbstract();
private:
    TemplateClass<int> t;
};

class DLL_API HasVirtualInDependency : public HasVirtualInCore
{
public:
    HasVirtualInDependency();
    HasVirtualInDependency* managedObject;
    int callManagedOverride();
};
