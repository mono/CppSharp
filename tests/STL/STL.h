#if defined(_MSC_VER)
#define DLL_API __declspec(dllexport)
#else
#define DLL_API
#endif

#include <vector>

struct DLL_API TestVectors
{
    std::vector<int> GetIntVector();
    int SumIntVector(std::vector<int>& vec);

    std::vector<int> IntVector;
};
