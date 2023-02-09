#pragma once

#include "../Tests.h"
#include "AnotherUnit.h"

#include <string>
#include <map>

class DeriveProtectedDtor
{
protected:
    ~DeriveProtectedDtor() {}
};

class DLL_API QString
{
};

class DLL_API T1
{
};

class DLL_API T2
{
public:
    T2();
    T2(int f);
    virtual ~T2();
    int getField() const;
    void setField(int value);
private:
    int field;
};

class DLL_API Ignored
{
};

template <typename T>
class HasAbstractReturnPointer
{
public:
    HasAbstractReturnPointer();
    ~HasAbstractReturnPointer();
    virtual const T* abstractReturnPointer() = 0;
};

template <typename T>
HasAbstractReturnPointer<T>::HasAbstractReturnPointer()
{
}

template <typename T>
HasAbstractReturnPointer<T>::~HasAbstractReturnPointer()
{
}

template <typename T>
class ImplReturnPointer : public HasAbstractReturnPointer<T>
{
public:
    ImplReturnPointer();
    ~ImplReturnPointer();
    virtual const T* abstractReturnPointer() override;
};

template <typename T>
ImplReturnPointer<T>::ImplReturnPointer()
{
}

template <typename T>
ImplReturnPointer<T>::~ImplReturnPointer()
{
}

template <typename T>
const T* ImplReturnPointer<T>::abstractReturnPointer()
{
    return 0;
}

template <typename T>
class IndependentFields : public T1
{
    typedef T Type;
public:
    class Nested
    {
    private:
        T field;
    };
    IndependentFields();
    IndependentFields(const IndependentFields<T>& other);
    IndependentFields(const T& t);
    IndependentFields(T1* t1);
    IndependentFields(T2* t2);
    IndependentFields(float f);
    ~IndependentFields();
    explicit IndependentFields(const std::map<T, T> &other);
    float getIndependent();
    T getDependent(const T& t);
    Type property();
    static T staticDependent(const T& t);
    template <typename AdditionalDependentType>
    void usesAdditionalDependentType(AdditionalDependentType additionalDependentType);
    static const int independentConst;
private:
    float independent;
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
IndependentFields<T>::IndependentFields(float f)
{
    independent = f;
}

template <typename T>
IndependentFields<T>::IndependentFields(const std::map<T, T> &v) : independent(1)
{
}

template <typename T>
IndependentFields<T>::~IndependentFields()
{
}

template <typename T>
T IndependentFields<T>::getDependent(const T& t)
{
    return t;
}

template <typename T>
T IndependentFields<T>::property()
{
    return T();
}

template <typename T>
T IndependentFields<T>::staticDependent(const T& t)
{
    return t;
}

template <typename T>
template <typename AdditionalDependentType>
void IndependentFields<T>::usesAdditionalDependentType(AdditionalDependentType additionalDependentType)
{
}

template <typename T>
float IndependentFields<T>::getIndependent()
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
class Base
{
public:
    class Nested
    {
    public:
        void f(const T& t);
        friend void f(Nested& n) {}
    };
    void invokeFriend();
    typedef T* typedefT;
};

template <typename T>
void Base<T>::Nested::f(const T& t)
{
}

template <typename T>
void Base<T>::invokeFriend()
{
    f(Nested());
}

template <typename T>
struct TemplateUnionField
{
    union
    {
        struct
        {
            T x, y, z;
        };
        T v;
    };
};

struct TemplateUnionFieldInstantiation
{
    TemplateUnionField<int> tuf;
    TemplateUnionField<float> tuf1;
};

template <typename T>
class DependentValueFields : public Base<T>
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
    typename Base<T>::typedefT typedefT() { return 0; }
    const T* returnTakeDependentPointer(const T* p);
    const T* propertyReturnDependentPointer();
    void hasDefaultDependentParam(T* ptr, const T& refT = T());
    HasAbstractReturnPointer<T>* getAbstractReturnPointer();
    typedef void (*DependentFunctionPointer)(T);
    DependentFunctionPointer dependentFunctionPointerField;
private:
    T field{};
    union {
        int unionField;
    };
};

template <typename T>
DependentValueFields<T>::DependentValueFields() : unionField(0), dependentFunctionPointerField(0)
{
}

template <typename T>
DependentValueFields<T>::~DependentValueFields()
{
}

template <typename T>
DependentValueFields<T>::DependentValueFields(IndependentFields<T> i) : DependentValueFields()
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
const T* DependentValueFields<T>::returnTakeDependentPointer(const T* p)
{
    return p;
}

template <typename T>
const T* DependentValueFields<T>::propertyReturnDependentPointer()
{
    return 0;
}

template <typename T>
void DependentValueFields<T>::hasDefaultDependentParam(T* ptr, const T& refT)
{
}

template <typename T>
HasAbstractReturnPointer<T>* DependentValueFields<T>::getAbstractReturnPointer()
{
    return new ImplReturnPointer<T>();
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
    return DependentValueFields<T>();
}

class DLL_API DerivedFromSpecializationOfUnsupportedTemplate : public DependentValueFields<int>
{
};

template <typename T>
class DependentPointerFields
{
public:
    DependentPointerFields(T* t = 0);
    ~DependentPointerFields();
    T property();
    T takeField(T t);
    T* field;
};

template <typename T>
DependentPointerFields<T>::DependentPointerFields(T* t) : field(t)
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
public:
    class iterator
    {
    public:
        iterator();
        ~iterator();
    };
    void takeDependentPtrToFirstTemplateArg(iterator i, const K& k);
    void takeDependentPtrToSecondTemplateArg(const V& v);
private:
    K key;
    V value;
};

template <typename K, typename V>
TwoTemplateArgs<K, V>::iterator::iterator()
{
}

template <typename K, typename V>
TwoTemplateArgs<K, V>::iterator::~iterator()
{
}

template <typename K, typename V>
void TwoTemplateArgs<K, V>::takeDependentPtrToFirstTemplateArg(iterator i, const K& k)
{
}

template <typename K, typename V>
void TwoTemplateArgs<K, V>::takeDependentPtrToSecondTemplateArg(const V& v)
{
}

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
class DLL_API HasDefaultTemplateArgument<bool, bool>
{
public:
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

class DLL_API DerivesFromExplicitSpecialization : public DerivesFromTemplateWithExplicitSpecialization<bool, bool>
{
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
    ~TemplateWithIndexer();
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
TemplateWithIndexer<T>::~TemplateWithIndexer<T>()
{
}

template <typename T>
T& TemplateWithIndexer<T>::operator[](int i)
{
    return t[0];
}

template <typename T>
T& TemplateWithIndexer<T>::operator[](const T& key)
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

template <typename T>
class VirtualDependentValueFields
{
public:
    VirtualDependentValueFields();
    virtual ~VirtualDependentValueFields();

private:
    T t;
};

template <typename T>
VirtualDependentValueFields<T>::VirtualDependentValueFields()
{
}

template <typename T>
VirtualDependentValueFields<T>::~VirtualDependentValueFields()
{
}

class DLL_API HasVirtualTemplate
{
public:
    VirtualTemplate<int> getVCopy();
    void setV(VirtualTemplate<int>* value);
    int function();
private:
    VirtualTemplate<int>* v;
    HasDefaultTemplateArgument<bool, bool> explicitSpecialization;
};

class DLL_API SpecializedInterfaceForMap : public InternalWithExtension<char>
{
};

class DLL_API HasSpecializationForSecondaryBase : public DependentValueFields<int>,
                                                  public IndependentFields<int>,
                                                  public InternalWithExtension<float>
{
};

template <typename T>
class TemplateInAnotherUnit;

class DLL_API TemplateSpecializer
{
public:
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
                                           TwoTemplateArgs<int*, float> p3,
                                           TwoTemplateArgs<const char*, int> p4,
                                           TwoTemplateArgs<QString, int> p5);
    void completeSpecializationInParameter(TwoTemplateArgs<const char*, int>::iterator p6,
                                           TwoTemplateArgs<QString, int>::iterator p7);
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

class DLL_API RegularDynamic
{
public:
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
    return DependentValueFields<OnlySpecialisedInTypeArg<T>>();
}

enum class UsedInTemplatedIndexer
{
    Item1,
    Item2
};

template <typename T>
class QFlags
{
    typedef int Int;
    struct Private;
    typedef int (Private::*Zero);
public:
    QFlags(T t);
    QFlags(Zero = nullptr);
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
    HasCtorWithMappedToEnum(T t);
    HasCtorWithMappedToEnum(QFlags<T> t);
};

template <typename T>
HasCtorWithMappedToEnum<T>::HasCtorWithMappedToEnum(T t)
{
}

template <typename T>
HasCtorWithMappedToEnum<T>::HasCtorWithMappedToEnum(QFlags<T> t)
{
}

template <typename T>
class AbstractTemplate
{
public:
    virtual int property() = 0;
    virtual int callFunction() = 0;
};

class DLL_API ImplementAbstractTemplate : public AbstractTemplate<int>
{
public:
    int property() override;
    int callFunction() override;
};

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
                             TemplateWithIndexer<void*> _12, TemplateWithIndexer<UsedInTemplatedIndexer> _13,
                             TemplateDerivedFromRegularDynamic<RegularDynamic> _14,
                             IndependentFields<OnlySpecialisedInTypeArg<double>> _15,
                             DependentPointerFields<float> _16, IndependentFields<const T1&> _17,
                             TemplateWithIndexer<T2*> _18, IndependentFields<int(*)(int)> _19,
                             TemplateWithIndexer<const char*> _20, VirtualDependentValueFields<int> _21,
                             VirtualDependentValueFields<float> _22, VirtualDependentValueFields<const char*> _23,
                             std::string s);

void hasIgnoredParam(DependentValueFields<IndependentFields<Ignored>> ii, Base<void> _24);

std::map<int, int> usesValidSpecialisationOfIgnoredTemplate();

DLL_API DependentValueFields<double> specialiseReturnOnly();

template <int Size> void* qbswap(const void *source, size_t count, void *dest) noexcept;
template<> inline void* qbswap<1>(const void *source, size_t count, void *dest) noexcept
{
    return 0;
}

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
private:
    union
    {
        int i;
        DependentValueFields<TestFlag> d;
    };
};

class SpecializationOfClassWithNonTypeTemplateArgument : public ClassWithNonTypeTemplateArgument<0>
{ };
template<std::size_t N>
class DLL_API FloatArrayF
{
public:
    template<typename... V, class = typename std::enable_if_t<sizeof...(V) == N>>
    FloatArrayF(V... x)  { }

};
const FloatArrayF<6> I6{ 1., 1., 1., 0., 0., 0. };

template <typename T>
DLL_API inline T FunctionTemplate(T value) {
    if (std::is_same<T, double>::value)
        return 4.2 + value;
    else if (std::is_same<T, float>::value)
        return 4.1 + value;
    return 4 + value;
}

inline void FunctionTemplateInstantiation()
{
    FunctionTemplate<double>({});
    FunctionTemplate<float>({});
    FunctionTemplate<int>({});
}

// KEEP ORDER OTHERWISE TEST WONT WORK
namespace IncompleteClassTemplatesTests
{
    template <size_t Size>
    struct StructSizeT {};

    template <typename T>
    struct StructT
    {
        template<typename U>
        struct Inc { };
    };

    struct Instantiation
    {
        StructT<StructSizeT<4000>> st;
    };
}
