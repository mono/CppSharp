#include "NamespacesBase.h"


Base::Base(int i)
{
    b = i;
}


Base::Base()
{
}

int Base::parent()
{
    return 0;
}

Base2::Base2() : Base()
{
}

Base2::Base2(int i) : Base(i)
{
}

void Base2::parent(int i)
{
}

HasVirtualInCore::HasVirtualInCore()
{
}

int HasVirtualInCore::virtualInCore(int parameter)
{
    return 1;
}
