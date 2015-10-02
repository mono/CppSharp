#include "NamespacesDerived.h"


OverlappingNamespace::InDerivedLib::InDerivedLib() : parentNSComponent(), color(black)
{
}

Derived::Derived() : Base2(10), baseComponent(5), nestedNSComponent(), color(OverlappingNamespace::blue)
{
}

Base Derived::getBase()
{
    return baseComponent;
}

void Derived::setBase(Base b)
{
    baseComponent = b;
}

void Derived::parent(int i)
{
}

OverlappingNamespace::InBaseLib Derived::getNestedNSComponent()
{
    return nestedNSComponent;
}

void Derived::setNestedNSComponent(OverlappingNamespace::InBaseLib c)
{
    nestedNSComponent = c;
}

Base3 Derived2::getBase()
{
    return baseComponent;
}

void Derived2::setBase(Base3 b)
{
    baseComponent = b;
}

OverlappingNamespace::InDerivedLib Derived2::getNestedNSComponent()
{
    return nestedNSComponent;
}

void Derived2::setNestedNSComponent(OverlappingNamespace::InDerivedLib c)
{
    nestedNSComponent = c;
}

void Derived2::defaultEnumValueFromDependency(OverlappingNamespace::ColorsEnum c)
{
}

Abstract* Derived2::getAbstract()
{
    return 0;
}

HasVirtualInDependency::HasVirtualInDependency()
{
}

int HasVirtualInDependency::callManagedOverride()
{
    return managedObject->virtualInCore(0);
}
