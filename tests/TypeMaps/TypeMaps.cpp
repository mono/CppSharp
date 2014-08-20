#include "TypeMaps.h"
#include <string.h>

QList::QList() : i(5)
{
}

QList HasQList::getList()
{
    return list;
}
