#include <iostream>
#include "Hello.h"

using namespace std;

Hello::Hello ()
{
}

void Hello::PrintHello(const char* s)
{
	cout << "PrintHello: " << s << "\n";
}

bool Hello::test1(int i, float f)
{
    return i == f;
}
