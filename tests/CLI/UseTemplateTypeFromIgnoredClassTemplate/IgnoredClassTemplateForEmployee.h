#pragma once

template<typename EmployeeT>
class IgnoredClassTemplateForEmployee
{
public:
    IgnoredClassTemplateForEmployee<EmployeeT>(EmployeeT* employee)
        : m_employee(employee)
    {       
    }

    EmployeeT* m_employee;
};