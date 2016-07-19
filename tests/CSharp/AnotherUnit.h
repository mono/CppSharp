#include "../Tests.h"

void DLL_API functionInAnotherUnit();

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
}
