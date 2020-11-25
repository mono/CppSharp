#pragma once

#if defined(_MSC_VER)

#if defined(DLL_EXPORT)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API __declspec(dllimport)
#endif

#ifndef STDCALL
#define STDCALL __stdcall
#endif

#ifndef CDECL
#define CDECL __cdecl
#endif

#ifndef THISCALL
#define THISCALL __thiscall
#endif

// HACK: work around https://developercommunity.visualstudio.com/content/problem/1269158/c4251-shown-for-any-not-explicitly-exported-templa.html
// harmless and requires exporting all template specializations
#pragma warning (disable : 4251 )

#else
#define DLL_API __attribute__ ((visibility ("default")))

#ifndef STDCALL
#if defined(WINDOWS)
#define STDCALL __attribute__((stdcall))
#else
// warning: calling convention 'stdcall' ignored for this target [-Wignored-attributes]
#define STDCALL
#endif
#endif

#ifndef CDECL
#define CDECL __attribute__((cdecl))
#endif

#ifndef THISCALL
#define THISCALL
#endif

#endif

#define CS_OUT
#define CS_IN_OUT
#define CS_VALUE_TYPE
