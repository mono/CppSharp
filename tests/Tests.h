#pragma once

#if defined(_MSC_VER)
#if DLL_EXPORT
#define DLL_API __declspec(dllexport)
#define DLL_API2 __declspec(dllexport)
#else
#define DLL_API
#define DLL_API2 __declspec(dllimport)
#endif

#ifndef STDCALL
#define STDCALL __stdcall
#endif

#ifndef CDECL
#define CDECL __cdecl
#endif
#else
#define DLL_API __attribute__ ((visibility ("default")))

#ifndef STDCALL
#define STDCALL __attribute__((stdcall))
#endif

#ifndef CDECL
#define CDECL __attribute__((cdecl))
#endif
#endif

#define CS_OUT
#define CS_IN_OUT
#define CS_VALUETYPE
