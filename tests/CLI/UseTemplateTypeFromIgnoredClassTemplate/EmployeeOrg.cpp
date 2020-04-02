 #include "EmployeeOrg.h"

#include "Employee.h"
#include "DeveloperEmployee.h"

EmployeeTypedef EmployeeOrg::GetEmployee()
{
    return IgnoredClassTemplateForEmployee<Employee>(new Employee());
}

EmployeeTypedef EmployeeOrg::GetDeveloperEmployee()
{
    return IgnoredClassTemplateForEmployee<Employee>(new DeveloperEmployee());
}

void EmployeeOrg::DoSomething()
{

}