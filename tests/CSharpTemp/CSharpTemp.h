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

protected:
    int P;
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
class QFlags
{
public:
    QFlags() {}
};

class DLL_API ComplexType
{
public:
    int check();
    QFlags<int> returnsQFlags();
    void takesQFlags(const QFlags<int> f);
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

struct DLL_API ValueType
{
};

class DLL_API MethodsWithDefaultValues
{
public:
    void DefaultPointer(Foo* ptr = 0);
    void DefaultValueType(ValueType bar = ValueType());
    void DefaultChar(char c = 'a');
    void DefaultRefTypeBeforeOthers(Foo foo = Foo(), int i = 5, Bar::Items item = Bar::Item2);
    void DefaultRefTypeAfterOthers(int i = 5, Bar::Items item = Bar::Item2, Foo foo = Foo());
    void DefaultRefTypeBeforeAndAfterOthers(int i = 5, Foo foo = Foo(), Bar::Items item = Bar::Item2, Baz baz = Baz());
    void DefaultIntAssignedAnEnum(int i = Bar::Item1);
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
