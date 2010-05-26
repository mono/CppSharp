/*
 *  CPPTest.h
 *  CPPTest
 *
 *  Created by Alex Corrado on 3/14/09.
 *  Copyright 2009 __MyCompanyName__. All rights reserved.
 *
 */

#ifndef CPPTest_
#define CPPTest_

/* The classes below are exported */
#pragma GCC visibility push(default)

class CSimpleClass {
public:
      int value;
      CSimpleClass(int value);
      ~CSimpleClass();
	  void M0();
	  virtual void V0(int x, int y);
	  void M1(int x);
      virtual void V1(int x);
	  void M2(int x, int y);
      virtual void V2();
};

class CSimpleSubClass : CSimpleClass {
public:
	CSimpleSubClass(int value);
	virtual void V0(int x, int y);
	virtual void V1(int x);
	virtual void V2();
};

extern "C" {
	CSimpleSubClass* CreateCSimpleSubClass(int value) {
		return new CSimpleSubClass(value);
	}
	void DestroyCSimpleSubClass(CSimpleSubClass* obj) {
		delete obj;
	}
}

#pragma GCC visibility pop
#endif
