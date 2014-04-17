#pragma once

#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API __attribute__ ((visibility ("default")))
#endif

#define CS_OUT