#include "../Tests.h"
#include <vector>

struct DLL_API TestVectors
{
    std::vector<int> GetIntVector();
    int SumIntVector(std::vector<int>& vec);

    std::vector<int> IntVector;
};
