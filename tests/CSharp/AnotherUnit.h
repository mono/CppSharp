#include "../Tests.h"
//#include <string>

void DLL_API functionInAnotherUnit();

struct DLL_API ForwardDeclaredStruct;

struct DLL_API DuplicateDeclaredStruct;

template <typename T>
class TemplateInAnotherUnit
{
    T field;
};

class ForwardInOtherUnitButSameModule
{
};

namespace HasFreeConstant
{
    extern const int DLL_API FREE_CONSTANT_IN_NAMESPACE;
//    extern const std::string DLL_API STD_STRING_CONSTANT;
}
