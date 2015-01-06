#include "../Tests.h"

class DLL_API Foo
{
public:
    Foo();
    int method();
    int operator[](int i) const;
    int operator[](unsigned int i);
    int& operator[](int i);
    int A;

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


class DLL_API Qux
{
public:
    Qux();
    Qux(Foo foo);
    int farAwayFunc() const;
    int array[3];
    void obsolete();
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
    int method();
    const Foo& operator[](int i) const;
    Foo& operator[](int i);
    Bar operator*();
    const Bar& operator*(int m);
    const Bar& operator++();
    Bar operator++(int i);
    void* arrayOfPrimitivePointers[1];

private:
    int index;
    Foo m_foo;
    Foo foos[4];
};

Bar::Bar() {}

class DLL_API Baz : public Foo, public Bar
{
public:
    Baz();

    int takesQux(const Qux& qux);
    Qux returnQux();

    typedef bool (*FunctionTypedef)(const void *);
    FunctionTypedef functionTypedef;
};

Baz::Baz() {}

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
    virtual void setValue(int value) = 0;

    virtual long prop() = 0;
    virtual void setProp(long prop);

    virtual int parent();

protected:
    int m_value;
    long m_property;
};

class DLL_API Proprietor : public AbstractProprietor
{
public:
    Proprietor();
    virtual void setValue(int value);

    virtual long prop();
};

Proprietor::Proprietor() {}

template <typename T>
class DLL_API QFlags
{
    typedef int (*Zero);
public:
    QFlags(T t);
    QFlags(Zero);
    operator T();
private:
    T flag;
};

enum class TestFlag
{
    Flag1,
    Flag2
};

class DLL_API ComplexType
{
public:
    ComplexType();
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
    static void InitMarker() { Marker = 0; }
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

#define DEFAULT_INT (2 * 1000UL + 500UL)

namespace Qt
{
    enum GlobalColor {
        black,
        white,
    };
}

class QColor
{
public:
    QColor();
    QColor(Qt::GlobalColor color);
};

class DLL_API MethodsWithDefaultValues
{
public:
    MethodsWithDefaultValues(Foo foo = Foo());
    MethodsWithDefaultValues(int a);
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
    void defaultImplicitCtorInt(Quux arg = 0);
    void defaultImplicitCtorChar(Quux arg = 'a');
    void defaultImplicitCtorFoo(Quux arg = Foo());
    void defaultIntWithLongExpression(unsigned int i = DEFAULT_INT);
    void defaultRefTypeEnumImplicitCtor(const QColor &fillColor = Qt::white);
    void rotate4x4Matrix(float angle, float x, float y, float z = 0.0f);
    int getA();
private:
    Foo m_foo;
};

class DLL_API HasPrivateOverrideBase
{
public:
    virtual void privateOverride(int i = 5);
};

class DLL_API HasPrivateOverride : public HasPrivateOverrideBase
{
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
