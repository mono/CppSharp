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

TemplateClass<int> Derived2::getTemplate()
{
    return t;
}

IndependentFields<int> Derived2::getIndependentSpecialization()
{
    return independentSpecialization;
}

Derived2::LocalTypedefSpecialization Derived2::getLocalTypedefSpecialization()
{
    return LocalTypedefSpecialization();
}

Abstract* Derived2::getAbstract()
{
    return 0;
}

DerivedFromExternalSpecialization::DerivedFromExternalSpecialization(int i,
                                                                     DependentFields<HasVirtualInDependency> defaultExternalSpecialization)
{
}

DependentFields<Base3> DerivedFromExternalSpecialization::returnExternalSpecialization()
{
    return DependentFields<Base3>();
}

int HasVirtualInDependency::callManagedOverride()
{
    return managedObject->virtualInCore(0);
}

bool operator<<(const Base& b, const char* str)
{
    return false;
}

const char* TestComments::GetIOHandlerControlSequence(char ch)
{
    return 0;
}

int TestComments::SBAttachInfo(const char* path, bool wait_for)
{
    return 0;
}

void TestComments::glfwDestroyWindow(int *window)
{
}

void forceUseSpecializations(ForwardedInIndependentHeader value)
{
}
