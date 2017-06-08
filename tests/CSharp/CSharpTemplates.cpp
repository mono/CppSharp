#include "CSharpTemplates.h"

T1::T1()
{
}

T1::T1(int f)
{
    field = f;
}

T1::~T1()
{
}

int T1::getField() const
{
    return field;
}

HasDefaultTemplateArgument<bool, bool>::HasDefaultTemplateArgument()
{
}

HasDefaultTemplateArgument<bool, bool>::~HasDefaultTemplateArgument()
{
}

bool HasDefaultTemplateArgument<bool, bool>::property()
{
    return field;
}

void HasDefaultTemplateArgument<bool, bool>::setProperty(const bool& t)
{
    field = t;
}

bool HasDefaultTemplateArgument<bool, bool>::staticProperty()
{
    return staticField;
}

void HasDefaultTemplateArgument<bool, bool>::setStaticProperty(const bool& t)
{
    staticField = t;
}

bool HasDefaultTemplateArgument<bool, bool>::staticField;

HasVirtualTemplate::HasVirtualTemplate()
{
}

HasVirtualTemplate::~HasVirtualTemplate()
{
}

VirtualTemplate<int> HasVirtualTemplate::getVCopy()
{
    return *v;
}

void HasVirtualTemplate::setV(VirtualTemplate<int>* value)
{
    v = value;
}

int HasVirtualTemplate::function()
{
    return v->function();
}

TemplateSpecializer::TemplateSpecializer()
{
}

IndependentFields<bool> TemplateSpecializer::getIndependentFields()
{
    return IndependentFields<bool>();
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

void forceUseSpecializations(IndependentFields<int> _1, IndependentFields<bool> _2,
                             IndependentFields<T1> _3, IndependentFields<std::string> _4,
                             VirtualTemplate<int> _5, VirtualTemplate<bool> _6,
                             HasDefaultTemplateArgument<int, int> _7, std::string s)
{
}
