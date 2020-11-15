#include "test-native.h"

extern "C" int plus(int a, int b)
{
    return a + b;
}