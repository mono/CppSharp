#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS
#define _LIBCPP_HIDE_FROM_ABI

#include <string>
#include <new>

template __declspec(dllexport) std::allocator<char>::allocator() noexcept;
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::basic_string() noexcept(true);
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>::~basic_string() noexcept;
template __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>> &std::basic_string<char, std::char_traits<char>, std::allocator<char>>::assign(const char *const);
template __declspec(dllexport) const char *std::basic_string<char, std::char_traits<char>, std::allocator<char>>::data() const noexcept;
template __declspec(dllexport) char *std::basic_string<char, std::char_traits<char>, std::allocator<char>>::data() noexcept;

using sv = std::basic_string_view<char, std::char_traits<char>>;

template __declspec(dllexport) sv::basic_string_view(sv::const_pointer data, sv::size_type size) noexcept(true);
template __declspec(dllexport) sv::basic_string_view() noexcept(true);
template __declspec(dllexport) const char *sv::data() const noexcept;