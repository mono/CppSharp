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

    void function();
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
    class NestedInTemplate
    {
    };
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
    typename TestTemplateClass<T>::NestedInTemplate n;
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

    //------------------------------------------------------------------
    /// Attach to a process by name.
    ///
    /// This function implies that a future call to SBTarget::Attach(...)
    /// will be synchronous.
    ///
    /// @param[in] path
    ///     A full or partial name for the process to attach to.
    ///
    /// @param[in] wait_for
    ///     If \b false, attach to an existing process whose name matches.
    ///     If \b true, then wait for the next process whose name matches.
    //------------------------------------------------------------------
    int SBAttachInfo(const char *path, bool wait_for);

    /*! @brief Destroys the specified window and its context.
    *
    *  This function destroys the specified window and its context.  On calling
    *  this function, no further callbacks will be called for that window.
    *
    *  If the context of the specified window is current on the main thread, it is
    *  detached before being destroyed.
    *
    *  @param[in] window The window to destroy.
    *
    *  @note The context of the specified window must not be current on any other
    *  thread when this function is called.
    *
    *  @reentrancy This function must not be called from a callback.
    *
    *  @thread_safety This function must only be called from the main thread.
    *
    *  @since Added in version 3.0.  Replaces `glfwCloseWindow`.
    */
    void glfwDestroyWindow(int* window);

    /**
     * <sip:alice@example.net>
     */
    class LinphoneAddress {};
};

template <typename T>
class ForwardedTemplate;

typedef ForwardedTemplate<int> i;
typedef ForwardedTemplate<long> l;
typedef TestTemplateClass<double> forceInSpecializations;

template class TestSpecializationArguments<const TestASTEnumItemByName>;

constexpr void constExpr();
void noExcept() noexcept;
void noExceptTrue() noexcept(true);
void noExceptFalse() noexcept(false);

template <typename T1, typename T2>
bool functionWithSpecInfo(const T1& t11, const T1& t12, const T2& t2);

template<>
bool functionWithSpecInfo(const float& t11, const float& t12, const float& t2);

void functionWithSpecializationArg(const TestTemplateClass<int>);

void testInlineAssembly()
{
#ifdef _WIN32
#ifndef _WIN64
    __asm mov eax, 1
#endif
#elif __linux__
    asm("mov eax, 1");
#endif
}

// Tests redeclarations
class ClassA {};

class ClassB {};
class ClassB;

class ClassC {};
class ClassC;
class ClassC;

class HasPrivateCCtorCopyAssignment
{
private:
    HasPrivateCCtorCopyAssignment() = delete;
    HasPrivateCCtorCopyAssignment(const HasPrivateCCtorCopyAssignment&) = delete;
    HasPrivateCCtorCopyAssignment& operator=(const HasPrivateCCtorCopyAssignment&) = delete;
};

__attribute__((deprecated)) int deprecated_func(int num);
int non_deprecated_func(int num);

TestTemplateClass<double> returnIncompleteTemplateSpecialization();

#define MACRO(x, y, z) x##y##z
