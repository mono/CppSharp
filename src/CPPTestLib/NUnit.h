//
// NUnit.h: The NUnit C++ interface
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

#ifndef _CPPINTEROP_NUNIT_H_
#define _CPPINTEROP_NUNIT_H_

#ifdef __GNUC__
#define EXPORT
#elif defined(_MSC_VER)
#define EXPORT __declspec(dllexport)
#else
#error Unknown compiler!
#endif

typedef const char* string;
typedef unsigned int uint;
typedef unsigned long ulong;

class NUnit {
	public:
		static NUnit* Assert;
		
	    virtual void Fail (string message);
        virtual void Fail ();
        virtual void IsTrue (bool condition, string message);
        virtual void IsTrue (bool condition);
        virtual void IsFalse (bool condition, string message);
        virtual void IsFalse (bool condition);
        virtual void IsEmpty (string aString, string message);
        virtual void IsEmpty (string aString);
        virtual void IsNotEmpty (string aString, string message);
        virtual void IsNotEmpty (string aString);
        virtual void AreEqual (int expected, int actual, string message);
        virtual void AreEqual (int expected, int actual);
        virtual void AreEqual (long expected, long actual, string message);
        virtual void AreEqual (long expected, long actual);
        virtual void AreEqual (uint expected, uint actual, string message);
        virtual void AreEqual (uint expected, uint actual);
        virtual void AreEqual (ulong expected, ulong actual, string message);
        virtual void AreEqual (ulong expected, ulong actual);
        virtual void AreEqual (double expected, double actual, double delta, string message);
        virtual void AreEqual (double expected, double actual, double delta);
        virtual void AreEqual (float expected, float actual, float delta, string message);
        virtual void AreEqual (float expected, float actual, float delta);
        virtual void AreNotEqual (int expected, int actual, string message);
        virtual void AreNotEqual (int expected, int actual);
        virtual void AreNotEqual (long expected, long actual, string message);
        virtual void AreNotEqual (long expected, long actual);
        virtual void AreNotEqual (uint expected, uint actual, string message);
        virtual void AreNotEqual (uint expected, uint actual);
        virtual void AreNotEqual (ulong expected, ulong actual, string message);
        virtual void AreNotEqual (ulong expected, ulong actual);
        virtual void AreNotEqual (double expected, double actual, string message);
        virtual void AreNotEqual (double expected, double actual);
        virtual void AreNotEqual (float expected, float actual, string message);
        virtual void AreNotEqual (float expected, float actual);
        virtual void Greater (int expected, int actual, string message);
        virtual void Greater (int expected, int actual);
        virtual void Greater (long expected, long actual, string message);
        virtual void Greater (long expected, long actual);
        virtual void Greater (uint expected, uint actual, string message);
        virtual void Greater (uint expected, uint actual);
        virtual void Greater (ulong expected, ulong actual, string message);
        virtual void Greater (ulong expected, ulong actual);
        virtual void Greater (double expected, double actual, string message);
        virtual void Greater (double expected, double actual);
        virtual void Greater (float expected, float actual, string message);
        virtual void Greater (float expected, float actual);
        virtual void Less (int expected, int actual, string message);
        virtual void Less (int expected, int actual);
        virtual void Less (long expected, long actual, string message);
        virtual void Less (long expected, long actual);
        virtual void Less (uint expected, uint actual, string message);
        virtual void Less (uint expected, uint actual);
        virtual void Less (ulong expected, ulong actual, string message);
        virtual void Less (ulong expected, ulong actual);
        virtual void Less (double expected, double actual, string message);
        virtual void Less (double expected, double actual);
        virtual void Less (float expected, float actual, string message);
        virtual void Less (float expected, float actual);
        virtual void GreaterOrEqual (int expected, int actual, string message);
        virtual void GreaterOrEqual (int expected, int actual);
        virtual void GreaterOrEqual (long expected, long actual, string message);
        virtual void GreaterOrEqual (long expected, long actual);
        virtual void GreaterOrEqual (uint expected, uint actual, string message);
        virtual void GreaterOrEqual (uint expected, uint actual);
        virtual void GreaterOrEqual (ulong expected, ulong actual, string message);
        virtual void GreaterOrEqual (ulong expected, ulong actual);
        virtual void GreaterOrEqual (double expected, double actual, string message);
        virtual void GreaterOrEqual (double expected, double actual);
        virtual void GreaterOrEqual (float expected, float actual, string message);
        virtual void GreaterOrEqual (float expected, float actual);
        virtual void LessOrEqual (int expected, int actual, string message);
        virtual void LessOrEqual (int expected, int actual);
        virtual void LessOrEqual (long expected, long actual, string message);
        virtual void LessOrEqual (long expected, long actual);
        virtual void LessOrEqual (uint expected, uint actual, string message);
        virtual void LessOrEqual (uint expected, uint actual);
        virtual void LessOrEqual (ulong expected, ulong actual, string message);
        virtual void LessOrEqual (ulong expected, ulong actual);
        virtual void LessOrEqual (double expected, double actual, string message);
        virtual void LessOrEqual (double expected, double actual);
        virtual void LessOrEqual (float expected, float actual, string message);
        virtual void LessOrEqual (float expected, float actual);
};


#endif /* _CPPINTEROP_NUNIT_H_ */
