// Just try out that system include headers can be included - no need for them in this code.
#include <iostream>
#include <fstream>

#ifndef WIN32
    #error "Generator should understand defines as well"
#endif

class Hello
{
public:
	Hello ();

	void PrintHello(const char* s);
	bool test1(int i, float f);
};
