
#include "MarshalingTests.h"

ClassWithCopyCtor::ClassWithCopyCtor(const ClassWithCopyCtor& f) {
	x = f.x;
}

ClassWithCopyCtor
ClassWithCopyCtor::Return (int x) {
	return ClassWithCopyCtor (x);
}

int
ClassWithCopyCtor::GetX () {
	return x;
}

ClassWithDtor
ClassWithDtor::Return (int x) {
	return ClassWithDtor (x);
}

int
ClassWithDtor::GetX () {
	return x;
}

ClassWithoutCopyCtor
ClassWithoutCopyCtor::Return (int x) {
	return ClassWithoutCopyCtor (x);
}

int
ClassWithoutCopyCtor::GetX () {
	return x;
}
