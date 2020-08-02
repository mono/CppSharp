#include "CSharpTemplates.h"

T1::T1()
{
}

T1::T1(const T1& other) : field(other.field)
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

T2::T2()
{
}

DerivedFromSpecializationOfUnsupportedTemplate::DerivedFromSpecializationOfUnsupportedTemplate()
{
}

DerivedFromSpecializationOfUnsupportedTemplate::~DerivedFromSpecializationOfUnsupportedTemplate()
{
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

DerivesFromExplicitSpecialization::DerivesFromExplicitSpecialization()
{
}

DerivesFromExplicitSpecialization::~DerivesFromExplicitSpecialization()
{
}

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

SpecializedInterfaceForMap::SpecializedInterfaceForMap()
{
}

SpecializedInterfaceForMap::~SpecializedInterfaceForMap()
{
}

HasSpecializationForSecondaryBase::HasSpecializationForSecondaryBase()
{
}

HasSpecializationForSecondaryBase::~HasSpecializationForSecondaryBase()
{
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
                                                            TwoTemplateArgs<int *, float> p3,
                                                            TwoTemplateArgs<const char *, int> p4,
                                                            TwoTemplateArgs<QString, int> p5,
                                                            TwoTemplateArgs<const char *, int>::iterator p6,
                                                            TwoTemplateArgs<QString, int>::iterator p7)
{
}

VirtualTemplate<void> TemplateSpecializer::returnSpecializedWithVoid()
{
    return VirtualTemplate<void>();
}

RegularDynamic::RegularDynamic()
{
}

RegularDynamic::~RegularDynamic()
{
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
                             TemplateWithIndexer<T2*> _12, TemplateDerivedFromRegularDynamic<RegularDynamic> _13,
                             IndependentFields<OnlySpecialisedInTypeArg<double> > _14, std::string s)
{
}

void hasIgnoredParam(DependentValueFields<IndependentFields<Ignored>> ii)
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
