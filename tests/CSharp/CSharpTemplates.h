#include "../Tests.h"
#include "AnotherUnit.h"

#include <string>

class DLL_API T1
{
public:
    T1();
    T1(int f);
    ~T1();
    int getField() const;
private:
    int field;
};

class DLL_API T2
{
};

template <typename T>
class DLL_API IndependentFields : public T1
{
public:
    IndependentFields();
    IndependentFields(const IndependentFields<T>& other);
    IndependentFields(const T& t);
    IndependentFields(int i);
    ~IndependentFields();
    int getIndependent();
    const T* returnTakeDependentPointer(const T* p);
    T getDependent(const T& t);
    static T staticDependent(const T& t);
    template <typename AdditionalDependentType>
    void usesAdditionalDependentType(AdditionalDependentType additionalDependentType);
    static const int independentConst;
private:
    int independent;
};

template <typename T>
const int IndependentFields<T>::independentConst = 15;

template <typename T>
IndependentFields<T>::IndependentFields() : independent(1)
{
}

template <typename T>
IndependentFields<T>::IndependentFields(const IndependentFields<T>& other)
{
    independent = other.independent;
}

template <typename T>
IndependentFields<T>::IndependentFields(const T& t) : independent(1)
{
}

template <typename T>
IndependentFields<T>::IndependentFields(int i)
{
    independent = i;
}

template <typename T>
IndependentFields<T>::~IndependentFields()
{
}

template <typename T>
const T* IndependentFields<T>::returnTakeDependentPointer(const T* p)
{
    return p;
}

template <typename T>
T IndependentFields<T>::getDependent(const T& t)
{
    return t;
}

template <typename T>
T IndependentFields<T>::staticDependent(const T& t)
{
    return t;
}

template <typename T>
int IndependentFields<T>::getIndependent()
{
    return independent;
}

template <typename X>
class DerivedChangesTypeName : public IndependentFields<X>
{
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
public:
    HasDefaultTemplateArgument();
    ~HasDefaultTemplateArgument();
    T property();
    void setProperty(const T& t);
    static T staticProperty();
    static void setStaticProperty(const T& t);
private:
    T field;
    static T staticField;
};

template <>
class HasDefaultTemplateArgument<bool, bool>
{
public:
    HasDefaultTemplateArgument();
    ~HasDefaultTemplateArgument();
    bool property();
    void setProperty(const bool& t);
    static bool staticProperty();
    static void setStaticProperty(const bool& t);
private:
    bool field;
    static bool staticField;
};

template <typename T, typename D>
HasDefaultTemplateArgument<T, D>::HasDefaultTemplateArgument()
{
}

template <typename T, typename D>
HasDefaultTemplateArgument<T, D>::~HasDefaultTemplateArgument()
{
}

template <typename T, typename D>
T HasDefaultTemplateArgument<T, D>::property()
{
    return field;
}

template <typename T, typename D>
void HasDefaultTemplateArgument<T, D>::setProperty(const T& t)
{
    field = t;
}

template <typename T, typename D>
T HasDefaultTemplateArgument<T, D>::staticProperty()
{
    return staticField;
}

template <typename T, typename D>
void HasDefaultTemplateArgument<T, D>::setStaticProperty(const T& t)
{
    staticField = t;
}

template <typename T, typename D>
T HasDefaultTemplateArgument<T, D>::staticField;

template <typename T>
class VirtualTemplate
{
public:
    VirtualTemplate();
    virtual ~VirtualTemplate();
    virtual int function();
};

template <typename T>
VirtualTemplate<T>::VirtualTemplate()
{
}

template <typename T>
VirtualTemplate<T>::~VirtualTemplate()
{
}

template <typename T>
int VirtualTemplate<T>::function()
{
    return 5;
}

class DLL_API HasVirtualTemplate
{
public:
    HasVirtualTemplate();
    ~HasVirtualTemplate();
    VirtualTemplate<int> getVCopy();
    void setV(VirtualTemplate<int>* value);
    int function();
private:
    VirtualTemplate<int>* v;
    HasDefaultTemplateArgument<bool, bool> explicitSpecialization;
};

template <typename T>
class TemplateInAnotherUnit;

class DLL_API TemplateSpecializer
{
public:
    TemplateSpecializer();
    template <typename T>
    class NestedTemplate
    {
    };
    IndependentFields<bool> getIndependentFields();
private:
    IndependentFields<int> independentFields;
    DependentValueFields<bool> dependentValueFields;
    DependentPointerFields<int> dependentPointerFields;
    HasDefaultTemplateArgument<int> hasDefaultTemplateArgument;
    DependentValueFields<T1> dependentPointerFieldsT1;
    DependentValueFields<T2> dependentPointerFieldsT2;
    TemplateInAnotherUnit<float> templateInAnotherUnit;
    DependentValueFields<IndependentFields<int>> specializeWithSpecialization;
    DependentValueFields<IndependentFields<bool>> specializeWithSameSpecialization;
    NestedTemplate<int> nestedTemplate;
    DependentValueFields<DependentValueFields<int*>> nestedDependentPointer1;
    DependentValueFields<DependentValueFields<char*>> nestedDependentPointer2;
    DependentValueFields<char[3]> dependentFieldArray;
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

template<class T>
class HasResultType {
    typedef char Yes;
    typedef void *No;
    template<typename U> static Yes test(int, const typename U::result_type * = 0);
    template<typename U> static No test(double);
public:
    enum { Value = (sizeof(test<T>(0)) == sizeof(Yes)) };
};

template <typename Functor, bool foo = HasResultType<Functor>::Value>
struct LazyResultType { typedef typename Functor::result_type Type; };
template <typename Functor>
struct LazyResultType<Functor, false> { typedef void Type; };

template <class InputSequence, class MapFunctor>
struct MapResultType
{
    typedef typename LazyResultType<MapFunctor>::Type ResultType;
};

template <template <typename> class InputSequence, typename MapFunctor, typename T>
struct MapResultType<InputSequence<T>, MapFunctor>
{
    typedef InputSequence<typename LazyResultType<MapFunctor>::Type> ResultType;
};

// we optimise specialisations so that only actually used ones are wrapped
void forceUseSpecializations(IndependentFields<int> _1, IndependentFields<bool> _2,
                             IndependentFields<T1> _3, IndependentFields<std::string> _4,
                             VirtualTemplate<int> _5, VirtualTemplate<bool> _6,
                             HasDefaultTemplateArgument<int, int> _7, DerivedChangesTypeName<T1> _8, std::string s);

// force the symbols for the template instantiations because we do not have the auto-compilation for the generated C++ source
template class DLL_API IndependentFields<int>;
template class DLL_API IndependentFields<bool>;
template class DLL_API IndependentFields<T1>;
template class DLL_API IndependentFields<std::string>;
template class DLL_API VirtualTemplate<int>;
template class DLL_API VirtualTemplate<bool>;
template class DLL_API HasDefaultTemplateArgument<int, int>;
template class DLL_API DerivedChangesTypeName<T1>;
