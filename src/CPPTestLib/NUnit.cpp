//
// NUnit.cpp: Bridges the NUnit Assert methods to C++
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

#include "NUnit.h"

NUnit* NUnit::Assert;

extern "C" {
	
	void SetNUnitInterface (NUnit* nunit)
	{
		NUnit::Assert = nunit;
	}
	
}
