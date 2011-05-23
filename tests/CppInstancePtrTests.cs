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
        public class CppInstancePtrTests {

                static CppInstancePtr uninitialized;

                [Test]
                [ExpectedException (typeof (ObjectDisposedException))]
                public void TestUninitialized ()
                {
                        Assert.IsFalse (uninitialized.IsManagedAlloc, "#A1");
                        Assert.AreEqual (IntPtr.Zero, uninitialized.Native, "#A2");
                }

                [Test]
                public void TestForManagedObject ()
                {
                        CppInstancePtr cppip = CppInstancePtr.ForManagedObject<EmptyTestInterface,CppMockObject> (CppMockObject.Instance);
                        Assert.AreNotEqual (IntPtr.Zero, cppip.Native, "#A1");
                        Assert.IsTrue (cppip.IsManagedAlloc, "#A2");
                        cppip.Dispose ();
                }

                [Test]
                [ExpectedException (typeof(ArgumentNullException))]
                public void TestForNonStaticWrapperWithNull ()
                {
                        CppInstancePtr cppip = CppInstancePtr.ForManagedObject<CSimpleClass.ICSimpleClass,CSimpleClass> (null);
                        cppip.Dispose ();
                }

                [Test]
                [ExpectedException (typeof (ObjectDisposedException))]
                public void TestDisposed ()
                {
                        CppInstancePtr cppip = CppInstancePtr.ForManagedObject<EmptyTestInterface,CppMockObject> (CppMockObject.Instance);
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
                        Assert.IsFalse (cppip.IsManagedAlloc, "#A1");
                        cppip.Dispose ();
                        DestroyCSimpleSubClass (native);
                }

                [DllImport("CPPTestLib")]
                private static extern IntPtr CreateCSimpleSubClass (int x);

                [DllImport("CPPTestLib")]
                private static extern void DestroyCSimpleSubClass (IntPtr cppip);
        }
}

