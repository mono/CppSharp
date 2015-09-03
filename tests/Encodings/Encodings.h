#include "../Tests.h"

class DLL_API Foo
{
public:
    const char* Unicode;

	// TODO: VC++ does not support char16
	// char16 chr16;    

    // TODO: move this, it has nothing to do with Unicode, it's here only not to break the CLI branch
    int operator[](int i) const;
    int operator[](int i);
};

DLL_API int FooCallFoo(Foo* foo);
