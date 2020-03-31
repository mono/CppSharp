#include "../Tests.h"
#include <vector>
#include <ostream>

struct DLL_API IntWrapper
{
    int Value;
};

struct DLL_API IntWrapperValueType
{
    int Value;
};

typedef std::vector<IntWrapperValueType> VectorTypedef;

struct DLL_API TestVectors
{
    TestVectors();
    std::vector<int> GetIntVector();
    int SumIntVector(std::vector<int>& vec);
    
    // Should get mapped to List<int>
    std::vector<int> IntVector;
    // Should get mapped to List<IntPtr>
    std::vector<int*> IntPtrVector;
    // Should get mapped to List<IntWrapper>
    std::vector<IntWrapper> IntWrapperVector;
    // Should get mapped to List<IntWrapper>
    std::vector<IntWrapper*> IntWrapperPtrVector;
    // Should get mapped to List<IntWrapperValueType>
    std::vector<IntWrapperValueType> IntWrapperValueTypeVector;
    // Should get mapped to List<IntWrapperValueType>
    VectorTypedef IntWrapperValueTypeVectorTypedef;
};

struct DLL_API OStreamTest
{
    static void WriteToOStream(std::ostream& stream, const char* s)
    {
        stream << s;
    };

    static void WriteToOStreamPtr(std::ostream* stream, const char* s)
    {
        *stream << s;
    };
};
