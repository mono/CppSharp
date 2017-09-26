template<typename T, T Value>
struct integral_constant
{
    static constexpr T value = Value;
};

template<bool Value>
using bool_constant = integral_constant<bool, Value>;

template<class T>
struct is_integral : integral_constant<bool, false>
{ };

template<typename T>
struct is_arithmetic : bool_constant<is_integral<T>::value>
{ };
