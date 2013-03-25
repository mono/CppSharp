#if defined(_MSC_VER)
#define CXXI_API __declspec(dllexport)
#else
#define CXXI_API 
#endif

class CXXI_API Hello
{
public:
	Hello ();

	void PrintHello(const char* s);
	bool test1(int i, float f);
};
