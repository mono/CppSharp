/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license.
*
************************************************************************/

#pragma once

#include <cstdint>
#include <vector>
#include <map>
#include <string>

#define CS_FLAGS
#define CS_IGNORE

#if defined(_MSC_VER) && !defined(__clang__)
#define CS_API __declspec(dllexport)
#else
#define CS_API 
#endif

#define CS_ABSTRACT
#define CS_VALUE_TYPE


/** We use these macros to workaround the lack of good standard C++
 * containers/string support in the C# binding backend. */

#define VECTOR(type, name) \
    std::vector<type> name; \
    type get##name (unsigned i); \
    void add##name (type& s); \
    unsigned get##name##Count (); \
    void clear##name();

#define DEF_VECTOR(klass, type, name) \
    type klass::get##name (unsigned i) { return name[i]; } \
    void klass::add##name (type& s) { return name.push_back(s); } \
    unsigned klass::get##name##Count () { return name.size(); } \
    void klass::clear##name() { name.clear(); }

#define VECTOR_STRING(name) \
    std::vector<std::string> name; \
    const char* get##name (unsigned i); \
    void add##name (const char* s); \
    unsigned get##name##Count (); \
    void clear##name();

#define DEF_VECTOR_STRING(klass, name) \
    const char* klass::get##name (unsigned i) { return name[i].c_str(); } \
    void klass::add##name (const char* s) { return name.push_back(std::string(s)); } \
    unsigned klass::get##name##Count () { return name.size(); } \
    void klass::clear##name() { name.clear(); }

#define STRING(name) \
    std::string name; \
    const char* get##name(); \
    void set##name(const char* s);

#define DEF_STRING(klass, name) \
    const char* klass::get##name() { return name.c_str(); } \
    void klass::set##name(const char* s) { name = s; }

