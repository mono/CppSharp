struct Primitives
{
  bool B;
  char C;
  unsigned char UC;
  short S;
  unsigned short US;
  int I;
  unsigned int UI;
  long L;
  unsigned long UL;
  long long LL;
  unsigned long long ULL;
  float F;
  double D;
};

struct Arrays
{
  float Array[2];
  Primitives Prim[1];
};

struct Pointers
{
  void * pv;
  char * pc;
  unsigned char * puc;
  const char * cpc;
  int * pi;
};

typedef int (*FnPtr)(double);
typedef void (*FnPtr2)(char, float);
typedef void (*FnPtr3)(void);

struct FunctionPointers
{
  FnPtr fn;
  FnPtr2 fn2;
  FnPtr3 fn3;
};

enum E { E1, E2 };

struct Tag
{
  Primitives p;
  E e;
};

