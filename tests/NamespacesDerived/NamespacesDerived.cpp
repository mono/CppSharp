#include "NamespacesDerived.h"


Derived::Derived() : Base(10), baseComponent(5), nestedNSComponent(), color(OverlappingNamespace::blue)
{
}


OverlappingNamespace::InDerivedLib::InDerivedLib() : parentNSComponent(), color(black)
{

}
