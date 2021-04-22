#include "Encodings.h"

std::string Foo::StringVariable = "";

// TODO: move this, it has nothing to do with Unicode, it's here only not to break the CLI branch
int Foo::operator[](int i) const
{
    return 5;
}

int Foo::operator[](int i)
{
    return 5;
}
