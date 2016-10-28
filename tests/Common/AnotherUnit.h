#include "../Tests.h"

// Verifies the header is included when the delegate is defined in a different file
typedef void (*DelegateInAnotherUnit)();

// Tests automatic generation of anonymous delegates in different translation units
namespace DelegateNamespace
{
    namespace Nested
    {
        void DLL_API f3(void (*)());
    }

    void DLL_API f4(void (*)());
}

namespace AnotherUnit
{
    void DLL_API f();
}
