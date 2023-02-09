#include "AST.h"
#include <vector>

// Tests class templates accross translation units
// Explicit instantiation
template class TestTemplateClass<Math::Complex>;
// Implicit instantiations
typedef TestTemplateClass<Math::Complex> TestTemplateClassComplex;
typedef TestTemplateClass<std::vector<Math::Complex>> TestTemplateClassMoreComplex;

template <typename T>
class ForwardedTemplate
{
};
