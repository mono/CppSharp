#include <iostream>
#include "Hello.h"

using namespace std;

void
Hello::PrintHello ()
{
	cout << "Hello, World!\n";
}

Hello::Hello ()
{
}

int
main ()
{
	Hello h;

	h.PrintHello ();

	return 0;
}
