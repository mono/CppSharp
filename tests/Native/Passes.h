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

class Foo { };
void FooStart(Foo*, int);

struct TestRename
{
  int lowerCaseMethod();
  int lowerCaseField;
};

#define TEST_ENUM_ITEM_NAME_0 0
#define TEST_ENUM_ITEM_NAME_1 1
#define TEST_ENUM_ITEM_NAME_2 2

// TestStructInheritance
struct S1 { int F1, F2; };
struct S2 : S1 { int F3; };