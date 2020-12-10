void Overload0() {}

int Overload1() { return 1; }
int Overload1(int) { return 2; }

int Overload(int, int) { return 1; }
int Overload(int, float) { return 2; }
int Overload(float, int) { return 3; }
