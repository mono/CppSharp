#include <string>


template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::basic_string(const char*, const std::allocator<char>&);
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::~basic_string() noexcept;
template __declspec(dllexport) const char* std::basic_string<char, std::char_traits<char>, std::allocator<char>>::c_str() const noexcept;
template __declspec(dllexport) std::allocator<char>::allocator() noexcept;
