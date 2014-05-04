#pragma once

#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#define STDCALL __stdcall
#define CDECL __cdecl
#else
#define DLL_API __attribute__ ((visibility ("default")))
#define STDCALL __attribute__((stdcall))
#define CDECL __attribute__((stdcall))
#endif

#define CS_OUT