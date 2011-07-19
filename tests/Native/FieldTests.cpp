
#include "FieldTests.h"

HasField::HasField (int number, HasField* other)
{
	this->number = number;
	this->other = other;
}