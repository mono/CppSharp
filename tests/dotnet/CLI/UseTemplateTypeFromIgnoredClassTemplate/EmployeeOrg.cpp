 #include "EmployeeOrg.h"

#include "Employee.h"

EmployeeTypedef EmployeeOrg::GetEmployee()
{
    return IgnoredClassTemplateForEmployee<Employee>(new Employee());
}

void EmployeeOrg::DoSomething()
{

}