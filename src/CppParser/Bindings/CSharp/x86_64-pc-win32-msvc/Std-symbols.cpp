#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS
#define _LIBCPP_HIDE_FROM_ABI

#include <string>
#include <new>

template __declspec(dllexport) std::allocator<char>::allocator() noexcept;
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::basic_string() noexcept(true);
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::~basic_string() noexcept;
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>& std::basic_string<char, std::char_traits<char>, std::allocator<char>>::assign(const char* const);
template __declspec(dllexport) const char* std::basic_string<char, std::char_traits<char>, std::allocator<char>>::data() const noexcept;
