void Overload0() {}

int Overload1() { return 1; }
int Overload1(int) { return 2; }

int Overload(int, int) { return 1; }
int Overload(int, float) { return 2; }
int Overload(float, int) { return 3; }

int DefaultParamsOverload() { return 0; }
int DefaultParamsOverload(int a, int b) { return 2; }
int DefaultParamsOverload(int a, float b = 2) { return 3; }

int DefaultParamsOverload2(int a = 0, int b = 0, int c = 0) { return 1; }
