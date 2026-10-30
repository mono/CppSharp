#include "ForwardRefWithNestedNamespace.h"

ForwardRefWithNestedNamespace::ForwardRefWithNestedNamespace()
    : m_forwardDeclaredClass(new ForwardDeclaredClass())
{
}

ForwardRefWithNestedNamespace::~ForwardRefWithNestedNamespace()
{
    delete m_forwardDeclaredClass;
}

ForwardDeclaredClass* ForwardRefWithNestedNamespace::GetForwardDeclaredClass()
{
    return m_forwardDeclaredClass;
}
