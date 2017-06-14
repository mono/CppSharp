#include <string>

template std::allocator<char>::allocator();
template std::basic_string<char, std::char_traits<char>, std::allocator<char>>::basic_string(const char*, const std::allocator<char>&);
template std::basic_string<char, std::char_traits<char>, std::allocator<char>>::~basic_string() noexcept;
template const char* std::basic_string<char, std::char_traits<char>, std::allocator<char>>::c_str() const noexcept;
