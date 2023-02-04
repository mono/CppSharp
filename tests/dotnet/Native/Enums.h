enum
{
    UnnamedEnumA1,
    EnumUnnamedA2
};

// This line will make sure that a visitor won't enumerate all enums across
// different translation units at once.
struct TestUniqueNames {};