#include "StandardLib.h"

TestVectors::TestVectors()
{
    IntVector.push_back(2);
    IntVector.push_back(3);
    IntVector.push_back(4);

    IntPtrVector.push_back(&IntVector[0]);
    IntPtrVector.push_back(&IntVector[1]);
    IntPtrVector.push_back(&IntVector[2]);
}

std::vector<int> TestVectors::GetIntVector()
{
    std::vector<int> vec;
    vec.push_back(2);
    vec.push_back(3);
    vec.push_back(4);

    return vec;
}

int TestVectors::SumIntVector(std::vector<int>& vec)
{
    int sum = 0;
    for (unsigned I = 0, E = vec.size(); I != E; ++I)
        sum += vec[I];

    return sum;
}