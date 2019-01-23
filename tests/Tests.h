#pragma once

#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)

#ifndef STDCALL
#define STDCALL __stdcall
#endif

#ifndef CDECL
#define CDECL __cdecl
#endif
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
#endif

#define CS_OUT
#define CS_IN_OUT
#define CS_VALUE_TYPE
