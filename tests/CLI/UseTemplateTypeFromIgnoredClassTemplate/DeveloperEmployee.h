#pragma once

#include "../../Tests.h"

#include "Employee.h"

class DLL_API DeveloperEmployee : public Employee
{
public:
    std::string GetName() override;
};