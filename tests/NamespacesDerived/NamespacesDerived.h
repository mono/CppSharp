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
class DLL_API Derived : public Base
{
public:
    Derived();

    Base baseComponent;
    Base getBase();
    void setBase(Base);

    OverlappingNamespace::InBaseLib nestedNSComponent;
    OverlappingNamespace::InBaseLib getNestedNSComponent();
    void setNestedNSComponent(OverlappingNamespace::InBaseLib);

    OverlappingNamespace::ColorsEnum color;

private:
    int d;
};


// For reference: using a type derived in the same library
class Base2
{
public:
    Base2();
};

template <typename T> class TemplateClass;

class Derived2 : public Base2
{
public:
    Derived2();

    Base2 baseComponent;
    Base2 getBase();
    void setBase(Base2);

    OverlappingNamespace::InDerivedLib nestedNSComponent;
    OverlappingNamespace::InDerivedLib getNestedNSComponent();
    void setNestedNSComponent(OverlappingNamespace::InDerivedLib);
    void defaultEnumValueFromDependency(OverlappingNamespace::ColorsEnum c = OverlappingNamespace::ColorsEnum::black);

    Abstract* getAbstract();
private:
    TemplateClass<int> t;
};
