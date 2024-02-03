#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS
#define _LIBCPP_HIDE_FROM_ABI

#include <string>
#include <new>

template std::allocator<char>::allocator() noexcept;
template std::allocator<char>::~allocator() noexcept;
template std::basic_string<char, std::char_traits<char>, std::allocator<char>>::basic_string() noexcept;
template std::basic_string<char, std::char_traits<char>, std::allocator<char>>::~basic_string() noexcept;
template std::basic_string<char, std::char_traits<char>, std::allocator<char>>& std::basic_string<char, std::char_traits<char>, std::allocator<char>>::assign(const char*);
template const char* std::basic_string<char, std::char_traits<char>, std::allocator<char>>::data() const noexcept;
