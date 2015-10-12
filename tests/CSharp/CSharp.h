#include "../Tests.h"
#include <cstdint>

class DLL_API Foo
{
public:
    Foo();
    Foo(int a, int p = 0);
    int method();
    int operator[](int i) const;
    int operator[](unsigned int i);
    int& operator[](int i);
    int A;
    int* (*functionPtrReturnsPtrParam)();
    int (STDCALL *attributedFunctionPtr)();
    bool isNoParams();
    void setNoParams();

    static const int rename = 5;

protected:
    int P;
};

class DLL_API Quux
{
public:
    Quux();
    Quux(int i);
    Quux(char c);
    Quux(Foo f);
private:
    int priv;
};

class Bar;

class DLL_API Qux
{
public:
    Qux();
    Qux(const Qux& other);
    Qux(Foo foo);
    Qux(Bar bar);
    int farAwayFunc() const;
    int array[3];
    void obsolete();
    Qux* getInterface();
    void setInterface(Qux* qux);
    virtual void v();
};

class DLL_API Bar : public Qux
{
public:
    enum Items
    {
        Item1,
        Item2
    };
    Bar();
    Bar(Qux qux);
    int method();
    const Foo& operator[](int i) const;
    Foo& operator[](int i);
    Bar operator*();
    const Bar& operator*(int m);
    const Bar& operator++();
    Bar operator++(int i);
    void* arrayOfPrimitivePointers[1];
    Foo foos[4];

private:
    int index;
    Foo m_foo;
};

Bar::Bar() {}

class DLL_API ForceCreationOfInterface : public Foo, public Bar
{
public:
    ForceCreationOfInterface();
};

class DLL_API Baz : public Foo, public Bar
{
public:
    class NestedBase1 {
        int f1;
        double f2;
        void* f3;
    };
    class NestedBase2 {};
    class NestedDerived : public NestedBase1, public NestedBase2 {};

    Baz();

    int P;

    int takesQux(const Qux& qux);
    Qux returnQux();
    void setMethod(int value);

    typedef bool (*FunctionTypedef)(const void *);
    FunctionTypedef functionTypedef;
};

Baz::Baz() : P(5) {}

struct QArrayData
{
};

typedef QArrayData QByteArrayData;

struct QByteArrayDataPtr
{
    QByteArrayData* ptr;
};

class DLL_API AbstractProprietor
{
public:
    virtual int getValue();
    virtual void setValue(int newValue) = 0;

    virtual long prop() = 0;
    virtual void setProp(long prop);

    virtual int parent();
    virtual int parent() const;

protected:
    AbstractProprietor();
    AbstractProprietor(int i);
    int m_value;
    long m_property;
};

class DLL_API Proprietor : public AbstractProprietor
{
public:
    Proprietor();
    Proprietor(int i);
    virtual void setValue(int value);

    virtual long prop();
};

Proprietor::Proprietor() {}

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

enum class TestFlag
{
    Flag1,
    Flag2
};

class DLL_API ComplexType
{
public:
    ComplexType();
    ComplexType(const QFlags<TestFlag> f);
    int check();
    QFlags<TestFlag> returnsQFlags();
    void takesQFlags(const QFlags<int> f);
private:
    QFlags<TestFlag> qFlags;
};

class DLL_API P : Proprietor
{
public:
    P(const Qux& qux);
    P(Qux* qux);

    virtual void setValue(int value);
    virtual long prop();

    ComplexType complexType();
    void setComplexType(const ComplexType& value);

    virtual void parent(int i);

    bool isTest();
    void setTest(bool value);

    void test();

    bool isBool();
    void setIsBool(bool value);

private:
    ComplexType m_complexType;
};

// Tests destructors
struct DLL_API TestDestructors
{
    static void InitMarker();
    static int Marker;

    TestDestructors();
    ~TestDestructors();
};

TestDestructors::TestDestructors() { Marker = 0xf00d; }
TestDestructors::~TestDestructors() { Marker = 0xcafe; }

class DLL_API TestCopyConstructorVal
{
public:
    TestCopyConstructorVal();
    TestCopyConstructorVal(const TestCopyConstructorVal& other);
    int A;
    float B;
};

class DLL_API TestRenaming
{
public:
    void name();
    void Name();
};

enum class Flags
{
    Flag1 = 1,
    Flag2 = 2,
    Flag3 = 4
};

DLL_API Flags operator|(Flags lhs, Flags rhs);

enum UntypedFlags
{
    Flag1 = 1,
    Flag2 = 2,
    Flag3 = 4
};

UntypedFlags operator|(UntypedFlags lhs, UntypedFlags rhs);

struct QGenericArgument
{
public:
    QGenericArgument(const char* name = 0);
    void* fixedArrayInValueType[1];
private:
    const char* _name;
};

class TestObjectMapWithClassDerivedFromStruct : public QGenericArgument
{
};

#define DEFAULT_INT (2 * 1000UL + 500UL)

namespace Qt
{
    enum GlobalColor {
        black,
        white,
    };
}

class DLL_API QColor
{
public:
    QColor();
    QColor(Qt::GlobalColor color);
};

template <typename T>
class QList
{
};

class DLL_API QPoint
{
public:
    QPoint(int x, int y);
};

class DLL_API QSize
{
public:
    QSize(int w, int h);
};

class DLL_API QRect
{
public:
    QRect(QPoint p, QSize s);
};

class DLL_API MethodsWithDefaultValues : public Quux
{
public:
    class DLL_API QMargins
    {
    public:
        QMargins(int left, int top, int right, int bottom);
    };

    MethodsWithDefaultValues(Foo foo = Foo());
    MethodsWithDefaultValues(int a);
    MethodsWithDefaultValues(double d, QList<QColor> list = QList<QColor>());
    void defaultPointer(Foo* ptr = 0);
    void defaultVoidStar(void* ptr = 0);
    void defaultValueType(QGenericArgument valueType = QGenericArgument());
    void defaultChar(char c = 'a');
    void defaultEmptyChar(char c = 0);
    void defaultRefTypeBeforeOthers(Foo foo = Foo(), int i = 5, Bar::Items item = Bar::Item2);
    void defaultRefTypeAfterOthers(int i = 5, Bar::Items item = Bar::Item2, Foo foo = Foo());
    void defaultRefTypeBeforeAndAfterOthers(int i = 5, Foo foo = Foo(), Bar::Items item = Bar::Item2, Baz baz = Baz());
    void defaultIntAssignedAnEnum(int i = Bar::Item1);
    void defaultRefAssignedValue(const Foo& fooRef = Foo());
    void DefaultRefAssignedValue(const Foo& fooRef = Foo());
    void defaultEnumAssignedBitwiseOr(Flags flags = Flags::Flag1 | Flags::Flag2);
    void defaultEnumAssignedBitwiseOrShort(UntypedFlags flags = Flag1 | Flag2);
    void defaultNonEmptyCtor(QGenericArgument arg = QGenericArgument(0));
    void defaultMappedToEnum(QFlags<Flags> qFlags = Flags::Flag1);
    void defaultMappedToZeroEnum(QFlags<Flags> qFlags = 0);
    void defaultMappedToEnumAssignedWithCtor(QFlags<Flags> qFlags = QFlags<Flags>());
    void defaultImplicitCtorInt(Quux arg = 0);
    void defaultImplicitCtorChar(Quux arg = 'a');
    void defaultImplicitCtorFoo(Quux arg = Foo());
    void defaultIntWithLongExpression(unsigned int i = DEFAULT_INT);
    void defaultRefTypeEnumImplicitCtor(const QColor &fillColor = Qt::white);
    void rotate4x4Matrix(float angle, float x, float y, float z = 0.0f);
    void defaultPointerToValueType(QGenericArgument* pointer = 0);
    void defaultDoubleWithoutF(double d1 = 1.0, double d2 = 1.);
    void defaultIntExpressionWithEnum(int i = Qt::GlobalColor::black + 1);
    void defaultCtorWithMoreThanOneArg(QMargins m = QMargins(0, 0, 0, 0));
    void defaultWithComplexArgs(const QRect& rectangle = QRect(QPoint(0, 0), QSize(-1, -1)));
    void defaultWithRefManagedLong(long long* i = 0);
    int getA();
private:
    Foo m_foo;
};

class DLL_API HasOverridesWithChangedAccessBase
{
public:
    HasOverridesWithChangedAccessBase();
    virtual void privateOverride(int i = 5);
protected:
    virtual void publicOverride();
};

class DLL_API HasOverridesWithChangedAccess : public HasOverridesWithChangedAccessBase
{
public:
    HasOverridesWithChangedAccess();
    void publicOverride();
private:
    virtual void privateOverride(int i);
};

class DLL_API AbstractWithProperty
{
public:
    virtual int property() = 0;
};

template <typename T>
class DLL_API IgnoredType
{
};

class DLL_API IgnoredTypeInheritingNonIgnoredWithNoEmptyCtor : public P
{
};

class DLL_API PropertyWithIgnoredType
{
public:
    IgnoredType<int> ignoredType();
    void setIgnoredType(const IgnoredType<int>& value);
private:
    IgnoredType<int> _ignoredType;
};

// --- Multiple inheritance

struct DLL_API MI_A0
{
    MI_A0();
    int get();
    int F;
};

MI_A0::MI_A0() : F(50) {}
int MI_A0::get() { return F; };

struct DLL_API MI_A
{
    MI_A();
    virtual void v(int i = 5);
};

MI_A::MI_A() {}
void MI_A::v(int i) {}

struct DLL_API MI_B : public MI_A
{
    MI_B();
};

MI_B::MI_B() {}

struct DLL_API MI_C : public MI_A0, public MI_B
{
    MI_C();
};

MI_C::MI_C() {}

class DLL_API StructWithPrivateFields
{
public:
    StructWithPrivateFields(int simplePrivateField, Foo complexPrivateField);
    int getSimplePrivateField();
    Foo getComplexPrivateField();
protected:
    int protectedField;
private:
    int simplePrivateField;
    Foo complexPrivateField;
};


template <class Key, class T>
class QMap
{
    struct Node
    {
        Key key;
        T value;
    };

public:
    QMap(const QMap<Key, T> &other);

    class const_iterator;

    class iterator
    {
    public:
        int test() {
            return 1;
        }
        friend class const_iterator;
        friend class QMap<Key, T>;
    };
    friend class iterator;

    class const_iterator
    {
        friend class iterator;
        friend class QMap<Key, T>;
    };
    friend class const_iterator;
};

#define Q_PROCESSOR_WORDSIZE 8
template <int> struct QIntegerForSize;
template <> struct QIntegerForSize<1> { typedef uint8_t  Unsigned; typedef int8_t  Signed; };
template <> struct QIntegerForSize<2> { typedef uint16_t Unsigned; typedef int16_t Signed; };
template <> struct QIntegerForSize<4> { typedef uint32_t Unsigned; typedef int32_t Signed; };
template <> struct QIntegerForSize<8> { typedef uint64_t Unsigned; typedef int64_t Signed; };
typedef QIntegerForSize<Q_PROCESSOR_WORDSIZE>::Signed qregisterint;
typedef QIntegerForSize<Q_PROCESSOR_WORDSIZE>::Unsigned qregisteruint;

struct DLL_API TestPointers
{
    void TestDoubleCharPointers(const char** names);
    void TestTripleCharPointers(const char*** names);

    const char** Names;
};

void TestPointers::TestDoubleCharPointers(const char** names)
{

}

void TestPointers::TestTripleCharPointers(const char*** names)
{

}

class DLL_API HasVirtualDtor1
{
public:
    HasVirtualDtor1();
    virtual ~HasVirtualDtor1();
    int testField;
};

class DLL_API HasVirtualDtor2
{
public:
    HasVirtualDtor2();
    virtual ~HasVirtualDtor2();
    HasVirtualDtor1* getHasVirtualDtor1();
    virtual void virtualFunction(const HasVirtualDtor1& param1, const HasVirtualDtor1& param2);
private:
    HasVirtualDtor1* hasVirtualDtor1;
};

class DLL_API TestNativeToManagedMap
{
public:
    TestNativeToManagedMap();
    virtual ~TestNativeToManagedMap();
    HasVirtualDtor2* getHasVirtualDtor2();
    Bar* propertyWithNoVirtualDtor() const;
    void setPropertyWithNoVirtualDtor(Bar* bar);
private:
    HasVirtualDtor2* hasVirtualDtor2;
    Bar* bar;
};

class DLL_API CallDtorVirtually : public HasVirtualDtor1
{
public:
    CallDtorVirtually();
    ~CallDtorVirtually();
    static bool Destroyed;
    static HasVirtualDtor1* getHasVirtualDtor1(HasVirtualDtor1* returned);
};

class HasProtectedNestedAnonymousType
{
protected:
    union
    {
        int i;
        double d;
    } u;
};

class DLL_API SecondaryBase
{
public:
    enum Property
    {
        P1,
        P2
    };
    enum Function
    {
        M1,
        M2
    };

    virtual void VirtualMember();
    int property();
    void setProperty(int value);
    void function(bool* ok = 0);
protected:
    void protectedFunction();
    int protectedProperty();
    void setProtectedProperty(int value);
};

class DLL_API TestOverrideFromSecondaryBase : public Foo, public SecondaryBase
{
public:
    TestOverrideFromSecondaryBase();
    void VirtualMember();
    void setProperty(int value);
};

class DLL_API TestParamToInterfacePassBaseOne
{
};

class DLL_API TestParamToInterfacePassBaseTwo
{
	int m;
public:
	int getM();
	void setM(int n);
	const TestParamToInterfacePassBaseTwo& operator++();
	TestParamToInterfacePassBaseTwo();
	TestParamToInterfacePassBaseTwo(int n);
};

class DLL_API TestParamToInterfacePass : public TestParamToInterfacePassBaseOne, public TestParamToInterfacePassBaseTwo
{
public:
	TestParamToInterfacePassBaseTwo addM(TestParamToInterfacePassBaseTwo b);
	TestParamToInterfacePassBaseTwo operator+(TestParamToInterfacePassBaseTwo b);
	TestParamToInterfacePass(TestParamToInterfacePassBaseTwo b);
	TestParamToInterfacePass();
};

class DLL_API HasProtectedVirtual
{
public:
    HasProtectedVirtual();
protected:
    virtual void protectedVirtual();
};

class DLL_API InheritanceBuffer : public Foo, public HasProtectedVirtual
{
public:
    InheritanceBuffer();
};

class DLL_API InheritsProtectedVirtualFromSecondaryBase : public InheritanceBuffer
{
public:
    InheritsProtectedVirtualFromSecondaryBase();
protected:
    void protectedVirtual();
};

void DLL_API freeFunctionWithUnsupportedDefaultArg(Foo foo = Foo());

class DLL_API TypeMappedWithOperator
{
public:
    TypeMappedWithOperator();
    int operator |(int i);
};

class HasOverrideOfHasPropertyWithDerivedType;

class DLL_API HasPropertyWithDerivedType
{
public:
    HasPropertyWithDerivedType();
    HasOverrideOfHasPropertyWithDerivedType* hasPropertyWithDerivedTypeSubclass;
    virtual void causeRenamingError();
};

class DLL_API HasOverrideOfHasPropertyWithDerivedType : public HasPropertyWithDerivedType
{
public:
    HasOverrideOfHasPropertyWithDerivedType();
    virtual void causeRenamingError();
};

class DLL_API MultiOverloadPtrToRef
{
    int * arr;
public:
    MultiOverloadPtrToRef(int* param);
    void funcPrimitivePtrToRef(int *pOne, char* pTwo, float* pThree, bool* pFour = 0);
    void funcPrimitivePtrToRefWithDefVal(int* pOne, char* pTwo, Foo* pThree, int* pFour = 0);
    virtual void funcPrimitivePtrToRefWithMultiOverload(int* pOne, char* pTwo, Foo* pThree, int* pFour = 0, long* pFive = 0);

    int* ReturnPrimTypePtr();
    void TakePrimTypePtr(int* ptr);
};

class DLL_API OverrideFromIndirectSecondaryBaseBase
{
public:
    OverrideFromIndirectSecondaryBaseBase();
    virtual int property();
};

class DLL_API OverrideFromDirectSecondaryBase : public Foo, public OverrideFromIndirectSecondaryBaseBase
{
public:
    OverrideFromDirectSecondaryBase();
};

class DLL_API OverrideFromIndirectSecondaryBase : public OverrideFromDirectSecondaryBase
{
public:
    OverrideFromIndirectSecondaryBase();
    int property();
};

class DLL_API TestVariableWithFixedArrayType
{
public:
    static Foo variableWithFixedArrayType[2];
};

class DLL_API TestOutTypeInterfaces
{
public:
    void funcTryInterfaceTypePtrOut(CS_OUT TestParamToInterfacePassBaseTwo* classTry);
    void funcTryInterfaceTypeOut(CS_OUT TestParamToInterfacePassBaseTwo classTry);
};

template <typename T>
class TemplateWithDependentField
{
public:
    TemplateWithDependentField();
    T t;
};

class DerivesFromTemplateInstantiation : public TemplateWithDependentField<int>
{
public:
    DerivesFromTemplateInstantiation();
};

DLL_API int PassConstantArrayRef(int(&arr)[2]);

class DLL_API TestComparison
{
public:
    TestComparison();
    int A;
    float B;
    bool operator ==(const TestComparison& other) const;
};

class DLL_API OverridePropertyFromIndirectPrimaryBaseBase
{
public:
    OverridePropertyFromIndirectPrimaryBaseBase();
    virtual int property() = 0;
    virtual void setProperty(int value) = 0;
};

class DLL_API OverridePropertyFromDirectPrimaryBase : public OverridePropertyFromIndirectPrimaryBaseBase
{
public:
    OverridePropertyFromDirectPrimaryBase();
    void setProperty(int value);
};

class DLL_API OverridePropertyFromIndirectPrimaryBase : public OverridePropertyFromDirectPrimaryBase
{
public:
    OverridePropertyFromIndirectPrimaryBase();
    int property();
};

class DLL_API QObject
{
public:
    QObject();
    virtual ~QObject();
    virtual void event();
};

class DLL_API QWidget : public QObject
{
public:
    QWidget();
    void event();
private:
    QObject child;
};

class DLL_API QApplication : public QObject
{
public:
    QApplication();
    static QApplication* instance;
    virtual void notify(QObject* receiver);
};

class DLL_API HasSamePropertyInDerivedAbstractType
{
public:
    HasSamePropertyInDerivedAbstractType();
    char* property();
};

class InheritsFromHasSamePropertyInDerivedAbstractType : public HasSamePropertyInDerivedAbstractType
{
public:
    InheritsFromHasSamePropertyInDerivedAbstractType();
    virtual int property() = 0;
};


class DLL_API MultipleInheritanceFieldOffsetsSecondaryBase
{
public:
    MultipleInheritanceFieldOffsetsSecondaryBase();
    int secondary;
};

class DLL_API MultipleInheritanceFieldOffsetsPrimaryBase
{
public:
    MultipleInheritanceFieldOffsetsPrimaryBase();
    int primary;
};

class DLL_API MultipleInheritanceFieldOffsets : public MultipleInheritanceFieldOffsetsPrimaryBase, public MultipleInheritanceFieldOffsetsSecondaryBase
{
public:
    MultipleInheritanceFieldOffsets();
    int own;
};
