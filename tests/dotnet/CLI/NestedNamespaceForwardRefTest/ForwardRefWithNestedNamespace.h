#pragma once

#include "../../Tests.h"

#include "ForwardDeclaredClass.h"

// The generated header needs a forward declaration of ForwardDeclaredClass in the outer
// namespace and one of NestedNamespace::NestedClass in a single nested namespace. The outer
// namespace's forward declarations must not be lost when the nested namespace is written.
class DLL_API ForwardRefWithNestedNamespace
{
public:
    ForwardRefWithNestedNamespace();
    ~ForwardRefWithNestedNamespace();

    ForwardDeclaredClass* GetForwardDeclaredClass();

private:
    ForwardDeclaredClass* m_forwardDeclaredClass;
};

namespace NestedNamespace
{
    class DLL_API NestedClass
    {
    };
}
