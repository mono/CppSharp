class Class
{
public:
    void ReturnsVoid() {}
    int ReturnsInt() { return 0; }
};

class ClassWithSingleInheritance : public Class
{
public:
    int ChildMethod() { return 2; }
};
