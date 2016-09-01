#define Q_SIGNALS protected

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
Q_SIGNALS:
};

class Atomics
{
#if defined( __clang__ ) && defined( __has_extension )
# if __has_extension( __c_atomic__ )
    _Atomic(int) AtomicInt;
# endif
#endif
};

class ImplicitCtor
{
};

template <typename T>
class TestSpecializationArguments
{
public:
    TestSpecializationArguments() {}
};

void instantiatesTemplate(TestSpecializationArguments<int> i, TestSpecializationArguments<float> f)
{
}

/// Hash set/map base class.
/** Note that to prevent extra memory use due to vtable pointer, %HashBase intentionally does not declare a virtual destructor
and therefore %HashBase pointers should never be used.
*/
class TestComments
{
public:
    //----------------------------------------------------------------------
    /// Get the string that needs to be written to the debugger stdin file
    /// handle when a control character is typed.
    ///
    /// Some GUI programs will intercept "control + char" sequences and want
    /// to have them do what normally would happen when using a real
    /// terminal, so this function allows GUI programs to emulate this
    /// functionality.
    ///
    /// @param[in] ch
    ///     The character that was typed along with the control key
    ///
    /// @return
    ///     The string that should be written into the file handle that is
    ///     feeding the input stream for the debugger, or NULL if there is
    ///     no string for this control key.
    //----------------------------------------------------------------------
    const char * GetIOHandlerControlSequence(char ch);
};

template <typename T>
class ForwardedTemplate;

typedef ForwardedTemplate<int> i;
typedef ForwardedTemplate<long> l;

template class TestSpecializationArguments<const TestASTEnumItemByName>;
