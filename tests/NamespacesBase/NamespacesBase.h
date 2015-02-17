#include "../Tests.h"


namespace OverlappingNamespace
{
    enum ColorsEnum {
        white,
        black,
        red,
        blue,
        green,
    };

    class InBaseLib
    {
    public:
        InBaseLib()
        {
        
        };
    };
}



class DLL_API Base
{
public:
    Base(int i);
    Base();

private:
    int b;
};
