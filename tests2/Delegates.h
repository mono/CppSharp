#include <FastDelegates.h>

using namespace fastdelegate;

class ClassWithDelegate
{
public:
    FastDelegate<int(int)> OnEvent0;
    void FireEvent0(int value) { if (OnEvent0) OnEvent0(value); }
};

class ClassInheritsDelegate : public ClassWithDelegate
{
};
