#include "../Tests.h"

class DLL_API QList
{
public:
    QList();
    int i;
};

class DLL_API HasQList
{
public:
    QList getList();
private:
    QList list;
};
