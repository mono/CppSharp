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

class DLL_API Abstract
{
public:
    virtual void abstractFunction() = 0;
};

template <typename T>
class TemplateClass
{
};
