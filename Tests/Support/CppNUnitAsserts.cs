//
// CppNUnitAsserts.cs: Exposes NUnit Assert methods to C++
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Mono.VisualC.Interop;

namespace Tests.Support {
        public static class CppNUnitAsserts {

                private interface IAssert : ICppClassOverridable<Assert> {
                        [Virtual] void Fail (CppInstancePtr @this, string message);
                        [Virtual] void Fail (CppInstancePtr @this);
                        [Virtual] void IsTrue (CppInstancePtr @this, bool condition, string message);
                        [Virtual] void IsTrue (CppInstancePtr @this, bool condition);
                        [Virtual] void IsFalse (CppInstancePtr @this, bool condition, string message);
                        [Virtual] void IsFalse (CppInstancePtr @this, bool condition);
                        [Virtual] void IsEmpty (CppInstancePtr @this, string aString, string message);
                        [Virtual] void IsEmpty (CppInstancePtr @this, string aString);
                        [Virtual] void IsNotEmpty (CppInstancePtr @this, string aString, string message);
                        [Virtual] void IsNotEmpty (CppInstancePtr @this, string aString);
                        [Virtual] void AreEqual (CppInstancePtr @this, int expected, int actual, string message);
                        [Virtual] void AreEqual (CppInstancePtr @this, int expected, int actual);
                        [Virtual] void AreEqual (CppInstancePtr @this, long expected, long actual, string message);
                        [Virtual] void AreEqual (CppInstancePtr @this, long expected, long actual);
                        [Virtual] void AreEqual (CppInstancePtr @this, uint expected, uint actual, string message);
                        [Virtual] void AreEqual (CppInstancePtr @this, uint expected, uint actual);
                        [Virtual] void AreEqual (CppInstancePtr @this, ulong expected, ulong actual, string message);
                        [Virtual] void AreEqual (CppInstancePtr @this, ulong expected, ulong actual);
                        [Virtual] void AreEqual (CppInstancePtr @this, double expected, double actual, double delta, string message);
                        [Virtual] void AreEqual (CppInstancePtr @this, double expected, double actual, double delta);
                        [Virtual] void AreEqual (CppInstancePtr @this, float expected, float actual, float delta, string message);
                        [Virtual] void AreEqual (CppInstancePtr @this, float expected, float actual, float delta);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, int expected, int actual, string message);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, int expected, int actual);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, long expected, long actual, string message);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, long expected, long actual);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, uint expected, uint actual, string message);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, uint expected, uint actual);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, ulong expected, ulong actual, string message);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, ulong expected, ulong actual);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, double expected, double actual, string message);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, double expected, double actual);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, float expected, float actual, string message);
                        [Virtual] void AreNotEqual (CppInstancePtr @this, float expected, float actual);
                        [Virtual] void Greater (CppInstancePtr @this, int expected, int actual, string message);
                        [Virtual] void Greater (CppInstancePtr @this, int expected, int actual);
                        [Virtual] void Greater (CppInstancePtr @this, long expected, long actual, string message);
                        [Virtual] void Greater (CppInstancePtr @this, long expected, long actual);
                        [Virtual] void Greater (CppInstancePtr @this, uint expected, uint actual, string message);
                        [Virtual] void Greater (CppInstancePtr @this, uint expected, uint actual);
                        [Virtual] void Greater (CppInstancePtr @this, ulong expected, ulong actual, string message);
                        [Virtual] void Greater (CppInstancePtr @this, ulong expected, ulong actual);
                        [Virtual] void Greater (CppInstancePtr @this, double expected, double actual, string message);
                        [Virtual] void Greater (CppInstancePtr @this, double expected, double actual);
                        [Virtual] void Greater (CppInstancePtr @this, float expected, float actual, string message);
                        [Virtual] void Greater (CppInstancePtr @this, float expected, float actual);
                        [Virtual] void Less (CppInstancePtr @this, int expected, int actual, string message);
                        [Virtual] void Less (CppInstancePtr @this, int expected, int actual);
                        [Virtual] void Less (CppInstancePtr @this, long expected, long actual, string message);
                        [Virtual] void Less (CppInstancePtr @this, long expected, long actual);
                        [Virtual] void Less (CppInstancePtr @this, uint expected, uint actual, string message);
                        [Virtual] void Less (CppInstancePtr @this, uint expected, uint actual);
                        [Virtual] void Less (CppInstancePtr @this, ulong expected, ulong actual, string message);
                        [Virtual] void Less (CppInstancePtr @this, ulong expected, ulong actual);
                        [Virtual] void Less (CppInstancePtr @this, double expected, double actual, string message);
                        [Virtual] void Less (CppInstancePtr @this, double expected, double actual);
                        [Virtual] void Less (CppInstancePtr @this, float expected, float actual, string message);
                        [Virtual] void Less (CppInstancePtr @this, float expected, float actual);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, int expected, int actual, string message);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, int expected, int actual);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, long expected, long actual, string message);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, long expected, long actual);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, uint expected, uint actual, string message);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, uint expected, uint actual);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, ulong expected, ulong actual, string message);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, ulong expected, ulong actual);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, double expected, double actual, string message);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, double expected, double actual);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, float expected, float actual, string message);
                        [Virtual] void GreaterOrEqual (CppInstancePtr @this, float expected, float actual);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, int expected, int actual, string message);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, int expected, int actual);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, long expected, long actual, string message);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, long expected, long actual);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, uint expected, uint actual, string message);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, uint expected, uint actual);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, ulong expected, ulong actual, string message);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, ulong expected, ulong actual);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, double expected, double actual, string message);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, double expected, double actual);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, float expected, float actual, string message);
                        [Virtual] void LessOrEqual (CppInstancePtr @this, float expected, float actual);
                }

                private static CppInstancePtr? nunitInterface;
                public static void Init ()
                {
                        if (!nunitInterface.HasValue)
                                nunitInterface = CppInstancePtr.ForManagedObject<IAssert,Assert> (null);

                        SetNUnitInterface ((IntPtr)nunitInterface.Value);
                }

                [DllImport("CPPTestLib")]
                private static extern void SetNUnitInterface (IntPtr nunit);
        }
}

