#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS
#define _LIBCPP_HIDE_FROM_ABI

#include <string>


template class __declspec(dllexport) std::char_traits<char>;
template class __declspec(dllexport) std::allocator<char>;
template class __declspec(dllexport) std::basic_string<char, std::char_traits<char>, std::allocator<char>>;
