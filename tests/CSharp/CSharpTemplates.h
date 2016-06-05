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
    union {
        int unionField;
    };
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
    template <typename T>
    class NestedTemplate
    {
    };
private:
    IndependentFields<int> independentFields;
    DependentValueFields<bool> dependentValueFields;
    DependentPointerFields<int> dependentPointerFields;
    HasDefaultTemplateArgument<int> hasDefaultTemplateArgument;
    DependentValueFields<T1> dependentPointerFieldsT1;
    DependentValueFields<T2> dependentPointerFieldsT2;
    DependentValueFields<IndependentFields<int>> specializeWithSpecialization;
    DependentValueFields<IndependentFields<bool>> specializeWithSameSpecialization;
    NestedTemplate<int> nestedTemplate;
    void completeSpecializationInParameter(DependentValueFields<float> p1,
                                           DependentValueFields<int*> p2,
                                           DependentValueFields<float*> p3);
    void completeSpecializationInParameter(TwoTemplateArgs<int*, int*> p1,
                                           TwoTemplateArgs<int*, int> p2,
                                           TwoTemplateArgs<int*, float> p3);
};

template <typename Key, typename T>
class PartiallySpecialized
{
};

template <typename Key>
class PartiallySpecialized<Key, int>
{
    union
    {
        int i;
        float f;
    };
};
