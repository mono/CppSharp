#pragma once

#include "../Tests.h"
#include "AnotherUnit.h"

#include <string>
#include <map>

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

class DLL_API Ignored
{
};

template <typename T>
class DLL_API IndependentFields : public T1
{
public:
    IndependentFields();
    IndependentFields(const IndependentFields<T>& other);
    IndependentFields(const T& t);
    IndependentFields(T1* t1);
    IndependentFields(T2* t2);
    IndependentFields(int i);
    ~IndependentFields();
    explicit IndependentFields(const std::map<T, T> &other);
    int getIndependent();
    const T* returnTakeDependentPointer(const T* p);
    T getDependent(const T& t);
    const T* propertyReturnDependentPointer();
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
IndependentFields<T>::IndependentFields(T1* t1) : independent(1)
{
}

template <typename T>
IndependentFields<T>::IndependentFields(T2* t2) : independent(1)
{
}

template <typename T>
IndependentFields<T>::IndependentFields(int i)
{
    independent = i;
}

template <typename T>
IndependentFields<T>::IndependentFields(const std::map<T, T> &v)
{
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
const T* IndependentFields<T>::propertyReturnDependentPointer()
{
    return 0;
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
class DLL_API DependentValueFieldForArray
{
private:
    T field{};
};

template <typename T>
class DLL_API Base
{
};

template <typename T>
class DLL_API DependentValueFields : public Base<T>
{
public:
    class Nested;
    class Nested
    {
        T field;
    };
    DependentValueFields();
    DependentValueFields(IndependentFields<T> i);
    ~DependentValueFields();
    DependentValueFields& returnInjectedClass();
    DependentValueFields returnValue();
    DependentValueFields operator+(const DependentValueFields& other);
    T getDependentValue();
    void setDependentValue(const T& value);
    IndependentFields<Nested> returnNestedInTemplate();
    typedef void (*DependentFunctionPointer)(T);
    DependentFunctionPointer dependentFunctionPointerField;
private:
    T field{};
    union {
        int unionField;
    };
};

template <typename T>
DependentValueFields<T>::DependentValueFields()
{
}

template <typename T>
DependentValueFields<T>::~DependentValueFields()
{
}

template <typename T>
DependentValueFields<T>::DependentValueFields(IndependentFields<T> i)
{
}

template <typename T>
T DependentValueFields<T>::getDependentValue()
{
    return field;
}

template <typename T>
void DependentValueFields<T>::setDependentValue(const T& value)
{
    field = value;
}

template <typename T>
IndependentFields<typename DependentValueFields<T>::Nested> DependentValueFields<T>::returnNestedInTemplate()
{
    return DependentValueFields<T>::Nested();
}

template <typename T>
DependentValueFields<T>& DependentValueFields<T>::returnInjectedClass()
{
    return *this;
}

template <typename T>
DependentValueFields<T> DependentValueFields<T>::returnValue()
{
    return *this;
}

template <typename T>
DependentValueFields<T> DependentValueFields<T>::operator+(const DependentValueFields& other)
{
    DependentValueFields<T> sum;
    sum.field = field + other.field;
    return sum;
}

class DLL_API DerivedFromSpecializationOfUnsupportedTemplate : public DependentValueFields<int>
{
public:
    DerivedFromSpecializationOfUnsupportedTemplate();
    ~DerivedFromSpecializationOfUnsupportedTemplate();
};

template <typename T>
class DLL_API DependentPointerFields
{
public:
    DependentPointerFields();
    ~DependentPointerFields();
    T property();
    T takeField(T t);
    T* field;
};

template <typename T>
DependentPointerFields<T>::DependentPointerFields()
{
}

template <typename T>
DependentPointerFields<T>::~DependentPointerFields()
{
}

template <typename T>
T DependentPointerFields<T>::property()
{
    return *field;
}

template <typename T>
T DependentPointerFields<T>::takeField(T t)
{
    return *field;
}

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
    bool operator==(const HasDefaultTemplateArgument& other);
    DependentValueFields<D> returnTemplateWithRenamedTypeArg(const DependentValueFields<D>& value);
    DependentValueFields<D> propertyReturnsTemplateWithRenamedTypeArg();
private:
    T field{};
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
bool HasDefaultTemplateArgument<T, D>::operator==(const HasDefaultTemplateArgument& other)
{
    return field == other.field;
}

template <typename T, typename D>
DependentValueFields<D> HasDefaultTemplateArgument<T, D>::returnTemplateWithRenamedTypeArg(const DependentValueFields<D>& value)
{
    return value;
}

template <typename T, typename D>
DependentValueFields<D> HasDefaultTemplateArgument<T, D>::propertyReturnsTemplateWithRenamedTypeArg()
{
    return DependentValueFields<D>();
}

template <typename T, typename D>
T HasDefaultTemplateArgument<T, D>::staticField;

template <typename T, typename D>
class DerivesFromTemplateWithExplicitSpecialization : public HasDefaultTemplateArgument<T, D>
{
public:
    DerivesFromTemplateWithExplicitSpecialization();
    ~DerivesFromTemplateWithExplicitSpecialization();
};

template <typename T, typename D>
DerivesFromTemplateWithExplicitSpecialization<T, D>::DerivesFromTemplateWithExplicitSpecialization()
{
}

template <typename T, typename D>
DerivesFromTemplateWithExplicitSpecialization<T, D>::~DerivesFromTemplateWithExplicitSpecialization()
{
}

class DerivesFromExplicitSpecialization : public DerivesFromTemplateWithExplicitSpecialization<bool, bool>
{
public:
    DerivesFromExplicitSpecialization();
    ~DerivesFromExplicitSpecialization();
};

template <typename T>
class InternalWithExtension
{
public:
    const T* extension();
};

template <typename T>
const T* InternalWithExtension<T>::extension()
{
    return 0;
}

template <typename T>
class TemplateWithIndexer
{
public:
    TemplateWithIndexer();
    T& operator[](int i);
    T& operator[](const T& key);
    T& operator[](const char* string);
private:
    T t[1];
    HasDefaultTemplateArgument<char> h;
    InternalWithExtension<char> i;
    HasDefaultTemplateArgument<T, double> specializationAsFieldOfAnother;
};

template <typename T>
TemplateWithIndexer<T>::TemplateWithIndexer()
{
}

template <typename T>
T& TemplateWithIndexer<T>::operator[](int i)
{
    return t[0];
}

template <typename T>
T& TemplateWithIndexer<T>::operator[](const char* string)
{
    return t[0];
}

template <typename T1 = void, typename T2 = void, typename T3 = void, typename T4 = void,
          typename T5 = void, typename T6 = void, typename T7 = void, typename T8 = void>
class OptionalTemplateArgs
{
};

template <typename T>
class VirtualTemplate
{
public:
    VirtualTemplate();
    VirtualTemplate(IndependentFields<int> i);
    VirtualTemplate(OptionalTemplateArgs<T> optionalTemplateArgs);
    virtual ~VirtualTemplate();
    virtual int function();
    virtual T* function(T* t);
    DependentValueFields<float> fieldWithSpecializationType;
};

template <typename T>
VirtualTemplate<T>::VirtualTemplate()
{
}

template <typename T>
VirtualTemplate<T>::VirtualTemplate(IndependentFields<int> i)
{
}

template <typename T>
VirtualTemplate<T>::VirtualTemplate(OptionalTemplateArgs<T> optionalTemplateArgs)
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

template <typename T>
T* VirtualTemplate<T>::function(T* t)
{
    return t;
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

class DLL_API SpecializedInterfaceForMap : InternalWithExtension<char>
{
public:
    SpecializedInterfaceForMap();
    ~SpecializedInterfaceForMap();
};

class DLL_API HasSpecializationForSecondaryBase : T1, DependentValueFields<int>, IndependentFields<int>,
                                                  InternalWithExtension<float>, DependentPointerFields<DependentValueFields<int>>
{
public:
    HasSpecializationForSecondaryBase();
    ~HasSpecializationForSecondaryBase();
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
    void completeSpecializationInParameter(DependentValueFields<float> p1,
                                           DependentValueFields<int*> p2,
                                           DependentValueFields<float*> p3);
    void completeSpecializationInParameter(TwoTemplateArgs<int*, int*> p1,
                                           TwoTemplateArgs<int*, int> p2,
                                           TwoTemplateArgs<int*, float> p3);
    VirtualTemplate<void> returnSpecializedWithVoid();
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
    DependentValueFieldForArray<char[3]> dependentFieldArray;
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

class RegularDynamic
{
public:
    RegularDynamic();
    ~RegularDynamic();
    virtual void virtualFunction();
};

template<typename T>
class TemplateDerivedFromRegularDynamic : public RegularDynamic
{
public:
    TemplateDerivedFromRegularDynamic();
    ~TemplateDerivedFromRegularDynamic();
};

template<typename T>
TemplateDerivedFromRegularDynamic<T>::TemplateDerivedFromRegularDynamic()
{
}

template<typename T>
TemplateDerivedFromRegularDynamic<T>::~TemplateDerivedFromRegularDynamic()
{
}

template <typename T>
class OnlySpecialisedInTypeArg
{
public:
    DependentValueFields<OnlySpecialisedInTypeArg<T>> returnSelfSpecialization();
};

template <typename T>
DependentValueFields<OnlySpecialisedInTypeArg<T>> OnlySpecialisedInTypeArg<T>::returnSelfSpecialization()
{
}

enum class UsedInTemplatedIndexer
{
    Item1,
    Item2
};

template <typename T>
class DLL_API QFlags
{
    typedef int Int;
    typedef int (*Zero);
public:
    QFlags(T t);
    QFlags(Zero = 0);
    operator Int();
private:
    int flag;
};

template <typename T>
QFlags<T>::QFlags(T t) : flag(Int(t))
{
}

template <typename T>
QFlags<T>::QFlags(Zero) : flag(Int(0))
{
}

template <typename T>
QFlags<T>::operator Int()
{
    return flag;
}

template <typename T>
class HasCtorWithMappedToEnum
{
public:
    HasCtorWithMappedToEnum(QFlags<T> t);
    HasCtorWithMappedToEnum(T t);
};

template <typename T>
HasCtorWithMappedToEnum<T>::HasCtorWithMappedToEnum(QFlags<T> t)
{
}

template <typename T>
HasCtorWithMappedToEnum<T>::HasCtorWithMappedToEnum(T t)
{
}

enum class TestFlag
{
    Flag1,
    Flag2
};

// we optimise specialisations so that only actually used ones are wrapped
void forceUseSpecializations(IndependentFields<int> _1, IndependentFields<bool> _2,
                             IndependentFields<T1> _3, IndependentFields<std::string> _4,
                             DependentValueFields<int> _5,
                             VirtualTemplate<int> _6, VirtualTemplate<bool> _7,
                             HasDefaultTemplateArgument<int, int> _8, DerivedChangesTypeName<T1> _9,
                             TemplateWithIndexer<int> _10, TemplateWithIndexer<T1> _11,
                             TemplateWithIndexer<T2*> _12, TemplateWithIndexer<UsedInTemplatedIndexer> _13,
                             TemplateDerivedFromRegularDynamic<RegularDynamic> _14,
                             IndependentFields<OnlySpecialisedInTypeArg<double>> _15,
                             DependentPointerFields<float> _16, std::string s);

void hasIgnoredParam(DependentValueFields<IndependentFields<Ignored>> ii);

std::map<int, int> usesValidSpecialisationOfIgnoredTemplate();

DLL_API DependentValueFields<double> specialiseReturnOnly();

template <int Size> void* qbswap(const void *source, size_t count, void *dest) noexcept;
template<> inline void* qbswap<1>(const void *source, size_t count, void *dest) noexcept
{
    return 0;
}

// force the symbols for the template instantiations because we do not have the auto-compilation for the generated C++ source
template class DLL_API IndependentFields<int>;
template class DLL_API IndependentFields<bool>;
template class DLL_API IndependentFields<T1>;
template class DLL_API IndependentFields<std::string>;
template class DLL_API Base<int>;
template class DLL_API DependentValueFields<int>;
template class DLL_API DependentValueFields<float>;
template class DLL_API DependentPointerFields<float>;
template class DLL_API VirtualTemplate<int>;
template class DLL_API VirtualTemplate<bool>;
template class DLL_API HasDefaultTemplateArgument<int, int>;
template class DLL_API DerivedChangesTypeName<T1>;
template class DLL_API TemplateWithIndexer<int>;
template class DLL_API TemplateWithIndexer<UsedInTemplatedIndexer>;
template class DLL_API TemplateWithIndexer<T1>;
template class DLL_API TemplateWithIndexer<T2*>;
template class DLL_API TemplateWithIndexer<float>;
template class DLL_API TemplateDerivedFromRegularDynamic<RegularDynamic>;
template class DLL_API HasCtorWithMappedToEnum<TestFlag>;

class TestForwardedClassInAnotherUnit;

// Forward declaration of class as friend
template<class T> class ForwardTemplateFriendClassContainer;
template<class T> class ForwardTemplateFriendClass;

template<class T>
class ForwardTemplateFriendClassContainer
{
    template<class K> friend class ForwardTemplateFriendClass;
};

template<class T>
class ForwardTemplateFriendClass
{
protected:
    ForwardTemplateFriendClass() { }
};

class ForwardTemplateFriendClassUser : public ForwardTemplateFriendClass<ForwardTemplateFriendClassUser>
{ };

template<int I>
class ClassWithNonTypeTemplateArgument
{
public:
    ClassWithNonTypeTemplateArgument() { }
};

class SpecializationOfClassWithNonTypeTemplateArgument : public ClassWithNonTypeTemplateArgument<0>
{ };
