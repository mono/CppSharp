enum FlagEnum
{
  A = 1 << 0,
  B = 1 << 1,
  C = 1 << 2,
  D = 1 << 3,
};

enum FlagEnum2
{
  A1 = 1 << 0,
  B1 = 3,
  C1 = 1 << 2,
  D1 = 1 << 4,
};

class Foo
{
    void toIgnore() { }
};
void FooStart(Foo*, int);

struct TestRename
{
  int lowerCaseMethod();
  int lowerCaseField;
};

struct TestReadOnlyProperties
{
    int readOnlyProperty;
    int getReadOnlyPropertyMethod() { return 0; }
    void setReadOnlyPropertyMethod(int value) { }
};

#define TEST_ENUM_ITEM_NAME_0 0
#define TEST_ENUM_ITEM_NAME_1 1
#define TEST_ENUM_ITEM_NAME_2 2

// TestStructInheritance
struct S1 { int F1, F2; };
struct S2 : S1 { int F3; };

// Tests unnamed enums
enum { Unnamed_Enum_1_A = 1, Unnamed_Enum_1_B = 2 };
enum { Unnamed_Enum_2_A = 3, Unnamed_Enum_2_B = 4 };

// Tests unique name for unnamed enums across translation units
#include "Enums.h"
enum
{
    UnnamedEnumB1,
    EnumUnnamedB2
};

struct TestCheckAmbiguousFunctionsPass
{
    // Tests removal of const method overloads
    int Method();
    int Method() const;
    int Method(int x);
    int Method(int x) const;
};

#define CS_INTERNAL
struct TestMethodAsInternal
{
    int CS_INTERNAL beInternal();
};
