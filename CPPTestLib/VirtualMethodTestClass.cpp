
#include "NUnit.h"

class EXPORT VirtualMethodTestClass {
	
	virtual void V0 (int a1, int a2, int a3)
	{
		NUnit::Assert->AreEqual (1, a1, "V0 #A1");
		NUnit::Assert->AreEqual (2, a2, "V0 #A2");
		NUnit::Assert->AreEqual (3, a3, "V0 #A3");
	}
	
};

extern "C" {
	VirtualMethodTestClass* CreateVirtualMethodTestClass ()
	{
		return new VirtualMethodTestClass ();
	}
	
	void DestroyVirtualMethodTestClass (VirtualMethodTestClass* vmtc)
	{
		delete vmtc;
	}
}
