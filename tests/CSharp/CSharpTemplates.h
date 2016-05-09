#include "../Tests.h"

class DLL_API T1
{
};

class DLL_API T2
{
};

template <typename T>
class DLL_API IndependentFields : public T1
{
private:
    int field;
};

template <typename T>
class DLL_API DependentValueFields
{
private:
    T field;
};

template <typename T>
class DLL_API DependentPointerFields
{
private:
    T* field;
};

template <typename K, typename V>
class TwoTemplateArgs
{
private:
    K key;
    V value;
};

template <typename T, typename D = IndependentFields<T>>
class HasDefaultTemplateArgument
{
    T field;
};

class DLL_API TemplateSpecializer
{
public:
    TemplateSpecializer();
private:
    IndependentFields<int> independentFields;
    DependentValueFields<int> dependentValueFields;
    DependentPointerFields<int> dependentPointerFields;
    HasDefaultTemplateArgument<int> hasDefaultTemplateArgument;
    DependentValueFields<T1> dependentPointerFieldsT1;
    DependentValueFields<T2> dependentPointerFieldsT2;
    void completeSpecializationInParameter(DependentValueFields<float> p1,
                                           DependentValueFields<int*> p2,
                                           DependentValueFields<float*> p3);
    void completeSpecializationInParameter(TwoTemplateArgs<int*, int*> p1,
                                           TwoTemplateArgs<int*, int> p2,
                                           TwoTemplateArgs<int*, float> p3);
};
