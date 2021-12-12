#include "CSharpTemplates.h"

T2::T2() : field(0)
{
}

T2::T2(int f)
{
    field = f;
}

T2::~T2()
{
}

int T2::getField() const
{
    return field;
}

void T2::setField(int value)
{
    field = value;
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
                                                            TwoTemplateArgs<int *, float> p3,
                                                            TwoTemplateArgs<const char *, int> p4,
                                                            TwoTemplateArgs<QString, int> p5)
{
}

void TemplateSpecializer::completeSpecializationInParameter(TwoTemplateArgs<const char *, int>::iterator p6,
                                                            TwoTemplateArgs<QString, int>::iterator p7)
{
}

VirtualTemplate<void> TemplateSpecializer::returnSpecializedWithVoid()
{
    return VirtualTemplate<void>();
}

void RegularDynamic::virtualFunction()
{
}

int ImplementAbstractTemplate::property()
{
    return 55;
}

int ImplementAbstractTemplate::callFunction()
{
    return 65;
}

void forceUseSpecializations(IndependentFields<int> _1, IndependentFields<bool> _2,
                             IndependentFields<T1> _3, IndependentFields<std::string> _4,
                             DependentValueFields<int> _5,
                             VirtualTemplate<int> _6, VirtualTemplate<bool> _7,
                             HasDefaultTemplateArgument<int, int> _8, DerivedChangesTypeName<T1> _9,
                             TemplateWithIndexer<int> _10, TemplateWithIndexer<T1> _11,
                             TemplateWithIndexer<void*> _12, TemplateWithIndexer<UsedInTemplatedIndexer> _13,
                             TemplateDerivedFromRegularDynamic<RegularDynamic> _14,
                             IndependentFields<OnlySpecialisedInTypeArg<double>> _15,
                             DependentPointerFields<float> _16, IndependentFields<const T1&> _17,
                             TemplateWithIndexer<T2*> _18, IndependentFields<int(*)(int)> _19,
                             TemplateWithIndexer<const char*> _20, VirtualDependentValueFields<int> _21,
                             VirtualDependentValueFields<float> _22, VirtualDependentValueFields<const char*> _23,
                             std::string s)
{
}

void hasIgnoredParam(DependentValueFields<IndependentFields<Ignored>> ii, Base<void> _24)
{
}

std::map<int, int> usesValidSpecialisationOfIgnoredTemplate()
{
    return std::map<int, int>();
}

DependentValueFields<double> specialiseReturnOnly()
{
    return DependentValueFields<double>();
}
