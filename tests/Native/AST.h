// Tests assignment of AST.Parameter properties
void TestParameterProperties(bool a, const short& b, int* c = nullptr) {};

// Tests various AST helper methods (like FindClass, FindOperator etc.)
namespace Math
{
    // Tests FindClass("Math::Complex")
    struct Complex {
        Complex(double r, double i) : re(r), im(i) {}
        Complex operator+(Complex &other);
    private:
        double re, im;
    };

    // Tests FindTypedef("Math::Single")
    typedef float Single;

    // Tests FindOperator method
    Complex Complex::operator+(Complex &other) {
        return Complex(re + other.re, im + other.im);
    }
}

// Tests Enum.ItemByName
enum TestASTEnumItemByName { TestItemByName };

// Tests class templates
template<typename T>
class TestTemplateClass
{
public:
    TestTemplateClass(T v);
    T Identity(T x);
    T value;
};

// Tests function templates
class TestTemplateFunctions
{
public:
    template<typename T> T Identity(T x) { return x; }
    template<int N> void Ignore() { };
    template<typename T, typename S> void MethodTemplateWithTwoTypeParameter(T t, S s) { };
    template<typename T> void Ignore(TestTemplateClass<T> v) { };
    template<typename T> T Valid(TestTemplateClass<int> v, T x) { return x; };
    template<typename T> T& Valid(TestTemplateClass<int> v, T x) const { return x; }
};

// Explicit instantiation
template class TestTemplateClass<int>;
// Implicit instantiation
typedef TestTemplateClass<int> TestTemplateClassInt;

// Now use the typedef
class TestTemplateClass2
{
public:
    TestTemplateClassInt* CreateIntTemplate();
};

namespace HidesClass
{
    class HiddenInNamespace
    {
    };
}

void testSignature();

#define MY_MACRO

class HasConstFunction
{
public:
    void testConstSignature() const;
    void testConstSignatureWithTrailingMacro() const MY_MACRO;
    const int& testConstRefSignature();
    static const int& testStaticConstRefSignature();
    friend inline const TestTemplateClass2 operator+(const TestTemplateClass2& f1, const TestTemplateClass2& f2);
};

void testImpl()
{
}

inline const TestTemplateClass2 operator+(const TestTemplateClass2& f1, const TestTemplateClass2& f2)
{
    return TestTemplateClass2();
}

class HasAmbiguousFunctions
{
public:
    void ambiguous();
    void ambiguous() const;
};

class Atomics
{
#if defined( __clang__ ) && defined( __has_extension )
# if __has_extension( __c_atomic__ )
    _Atomic(int) AtomicInt;
# endif
#endif
};