
#include "TestFramework.h"

NUnit* NUnit::Assert;

extern "C" {
	
	void SetNUnitInterface (NUnit* nunit)
	{
		NUnit::Assert = nunit;
	}
	
}
