/*
 *  CPPTest.cpp
 *  CPPTest
 *
 *  Created by Alex Corrado on 3/14/09.
 *  Copyright 2009 __MyCompanyName__. All rights reserved.
 *
 */

#include <stdio.h>
#include "CPPTest.h"

CSimpleClass::CSimpleClass(int value) : value(value) {
	printf("CSimpleClass(%d)\n", value);
	this->value = value;
}

CSimpleSubClass::CSimpleSubClass(int value) : CSimpleClass(value) {
	printf("CSimpleSubClass(%d)\n", value);
}

CSimpleClass::~CSimpleClass() {
	printf("~CSimpleClass\n");
}

void CSimpleClass::M0() {
	printf("C++/CSimpleClass::M0()\n");
	V0(value, value + 1);
	V1(value);
	V2();
}

void CSimpleClass::V0(int x, int y) {
	printf("C++/CSimpleClass::V0(%d, %d)\n", x, y);
}

void CSimpleSubClass::V0(int x, int y) {
	printf("C++/CSimpleSubClass::V0(%d, %d)\n", x, y);
}

void CSimpleClass::M1(int x) {
	printf("C++/CSimpleClass::M1(%d)\n", x);
	V0(x, value);
	V1(x);
	V2();
}

void CSimpleClass::V1(int x) {
	printf("C++/CSimpleClass::V1(%d)\n", x);
}

void CSimpleSubClass::V1(int x) {
	printf("C++/CSimpleSubClass::V1(%d)\n", x);
}

void CSimpleClass::M2(int x, int y) {

}

void CSimpleClass::V2() {
	printf("C++/CSimpleClass::V2() - value: %d\n", value);
}

void CSimpleSubClass::V2() {
	printf("C++/CSimpleSubClass::V2() - value: %d\n", value);
}
