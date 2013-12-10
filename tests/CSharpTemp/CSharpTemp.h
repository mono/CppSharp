#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

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
    int method();
    const Foo& operator[](int i) const;
    Foo& operator[](int i);
    Bar operator*();
    const Bar& operator*(int m);
    const Bar& operator++();
    Bar operator++(int i);

private:
    int index;
    Foo m_foo;
};

class DLL_API Baz : public Foo, public Bar
{
public:

    int takesQux(const Qux& qux);
    Qux returnQux();
};

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
    virtual void setValue(int value);

    virtual long prop();
};

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

    bool isBool();
    void setIsBool(bool value);

private:
    ComplexType m_complexType;
};
