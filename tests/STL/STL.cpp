#include "STL.h"

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