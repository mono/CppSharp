#pragma once

#include "../../Tests.h"

#include "ClassWithNestedEnum.h"

class DLL_API NestedEnumConsumer
{
public:
    ClassWithNestedEnum::NestedEnum GetPassedEnum(ClassWithNestedEnum::NestedEnum e);
};