#include "CSharpTemplates.h"

TemplateSpecializer::TemplateSpecializer()
{
}

void TemplateSpecializer::completeSpecializationInParameter(DependentValueFields<float> p1,
                                                            DependentValueFields<int*> p2,
                                                            DependentValueFields<float*> p3)
{
}

void TemplateSpecializer::completeSpecializationInParameter(TwoTemplateArgs<int *, int *> p1,
                                                            TwoTemplateArgs<int *, int> p2,
                                                            TwoTemplateArgs<int *, float> p3)
{
}
