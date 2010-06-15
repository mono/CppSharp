//
// CppInstancePtrTests.cs: Test cases to exercise the CppInstancePtr
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
using Tests.Support;

namespace Tests {
        [TestFixture]
        public class CppInstancePtrTests : CPPTestLibBase {

                static CppInstancePtr uninitialized;

                [Test]
                [ExpectedException (typeof (ObjectDisposedException))]
                public void TestUninitialized ()
                {
                        Assert.IsFalse (uninitialized.IsManagedAlloc, "cppip.IsManagedAlloc");
                        Assert.AreEqual (IntPtr.Zero, uninitialized.Native, "cppip.Native wasn't null pointer");
                }

                [Test]
                public void TestForManagedObject ()
                {
                        CppInstancePtr cppip = CppInstancePtr.ForManagedObject<EmptyTestInterface,CPPTestLibBase> (this);
                        Assert.AreNotEqual (IntPtr.Zero, cppip.Native, "cppip.Native was null pointer");
                        Assert.IsTrue (cppip.IsManagedAlloc, "cppip.IsManagedAlloc was not true");
                        cppip.Dispose ();
                }

                [Test]
                [ExpectedException (typeof (ObjectDisposedException))]
                public void TestDisposed ()
                {
                        CppInstancePtr cppip = CppInstancePtr.ForManagedObject<EmptyTestInterface,CPPTestLibBase> (this);
                        cppip.Dispose ();
                        // should throw
                        Console.WriteLine (cppip.Native);
                }

                [Test]
                public void TestFromNativePtr ()
                {
                        IntPtr native = CreateCSimpleSubClass (0);
                        CppInstancePtr cppip = new CppInstancePtr (native);
                        Assert.AreEqual (native, cppip.Native);
                        Assert.IsFalse (cppip.IsManagedAlloc, "cppip.IsManagedAlloc was not false");
                        cppip.Dispose ();
                        DestroyCSimpleSubClass (native);
                }

                [DllImport("CPPTestLib")]
                private static extern IntPtr CreateCSimpleSubClass (int x);

                [DllImport("CPPTestLib")]
                private static extern void DestroyCSimpleSubClass (IntPtr cppip);
        }
}

