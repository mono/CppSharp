enum class Enum0
{
    Item0,
    Item1,
    Item2 = 5
};

Enum0 ReturnsEnum() { return Enum0::Item0; }
Enum0 PassAndReturnsEnum(Enum0 e) { return e; }