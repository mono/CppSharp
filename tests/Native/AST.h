// Tests assignment of AST.Parameter properties
void TestParameterProperties(bool a, const short& b, int* c = nullptr) {};

// Tests various AST helper methods (like FindClass, FindOperator etc.)
namespace Math
{
    struct Complex {
        Complex(double r, double i) : re(r), im(i) {}
        Complex operator+(Complex &other);
    private:
        double re, im;
    };

    // Operator overloaded using a member function
    Complex Complex::operator+(Complex &other) {
        return Complex(re + other.re, im + other.im);
    }
}