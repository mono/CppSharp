#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS
#define _LIBCPP_HIDE_FROM_ABI

#include <string>
#include <optional>
#include <new>

template std::allocator<char>::allocator() noexcept;
template std::optional<unsigned int>::optional() noexcept;
template const unsigned int&& std::optional<unsigned int>::value() const&&;
