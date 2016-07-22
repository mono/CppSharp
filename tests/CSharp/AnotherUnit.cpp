#include "AnotherUnit.h"

void functionInAnotherUnit()
{
}

namespace HasFreeConstant
{
    extern const int DLL_API FREE_CONSTANT_IN_NAMESPACE = 5;
//    extern const std::string DLL_API STD_STRING_CONSTANT = "test";
}
