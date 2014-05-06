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