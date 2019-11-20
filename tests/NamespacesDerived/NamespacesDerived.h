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
    TemplateClass<Derived> d;
};

class DLL_API HasVirtualInDependency : public HasVirtualInCore
{
public:
    HasVirtualInDependency();
    HasVirtualInDependency* managedObject;
    int callManagedOverride();
};

class DLL_API DerivedFromExternalSpecialization : public TemplateWithIndependentFields<Derived>
{
public:
    DerivedFromExternalSpecialization(int i,
                                      TemplateWithIndependentFields<HasVirtualInDependency> defaultExternalSpecialization =
                                          TemplateWithIndependentFields<HasVirtualInDependency>());
    ~DerivedFromExternalSpecialization();
    TemplateWithIndependentFields<Base3> returnExternalSpecialization();
};

class DLL_API DerivedFromSecondaryBaseInDependency : public Derived, public SecondaryBase
{
public:
    DerivedFromSecondaryBaseInDependency();
    ~DerivedFromSecondaryBaseInDependency();
};

DLL_API bool operator<<(const Base& b, const char* str);

namespace NamespacesBase
{
    class DLL_API ClassInNamespaceNamedAfterDependency
    {
    private:
        Base base;
    };
}
