/*
 *  CPPTest.cpp
 *  CPPTest
 *
 *  Created by Alex Corrado on 3/14/09.
 *  Copyright 2009 __MyCompanyName__. All rights reserved.
 *
 */

#include <stdio.h>

/* The classes below are exported */
#pragma GCC visibility push(default)

class CSimpleClass {
public:
      int value;
      CSimpleClass (int value) : value (value)
      {
	      printf ("CSimpleClass(%d)\n", value);
	      this->value = value;
      }
      
      ~CSimpleClass ()
      {
	      printf ("~CSimpleClass\n");   
      }
      
      void M0 ()
      {
	      printf ("C++/CSimpleClass::M0()\n");
	      V0 (value, value + 1);
	      V1 (value);
      }
      
      virtual void V0 (int x, int y)
      {
	      printf ("C++/CSimpleClass::V0(%d, %d)\n", x, y);
      }
      
      void M1 (int x)
      {
	      printf ("C++/CSimpleClass::M1(%d)\n", x);
      }
      
      virtual void V1(int x)
      {
	      printf("C++/CSimpleClass::V1(%d)\n", x);   
      }
      
      void M2(int x, int y)
      {
	      printf ("C++/CSimpleClass::M2(%d, %d)\n", x, y);
      }
};

class CSimpleSubClass : CSimpleClass {
public:
        CSimpleSubClass (int value) : CSimpleClass (value)
        {
	    	printf("CSimpleSubClass(%d)\n", value);
        }
        
        virtual void V0 (int x, int y)
        {
	        printf ("C++/CSimpleSubClass::V0(%d, %d)\n", x, y);   
        }
        
        virtual void V1 (int x)
        {
	        printf("C++/CSimpleSubClass::V1(%d)\n", x);   
        }
};

extern "C" {
        CSimpleSubClass* CreateCSimpleSubClass (int value)
        {
                return new CSimpleSubClass(value);
        }
        void DestroyCSimpleSubClass (CSimpleSubClass* obj)
        {
                delete obj;
        }
}

#pragma GCC visibility pop
