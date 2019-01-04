#include <string>


template __attribute__((visibility("default"))) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::basic_string();
template __attribute__((visibility("default"))) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::~basic_string() noexcept;
template __attribute__((visibility("default"))) std::basic_string<char, std::char_traits<char>, std::allocator<char>>& std::basic_string<char, std::char_traits<char>, std::allocator<char>>::assign(const std::basic_string<char, std::char_traits<char>, std::allocator<char>>::value_type*);
template __attribute__((visibility("default"))) const std::basic_string<char, std::char_traits<char>, std::allocator<char>>::value_type* std::basic_string<char, std::char_traits<char>, std::allocator<char>>::c_str() const noexcept;
