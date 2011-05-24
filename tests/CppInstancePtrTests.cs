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
                [ExpectedException (typeof (ObjectDisposedException))]
                public void TestDisposed ()
                {
                        CppInstancePtr cppip = CppInstancePtr.ForManagedObject<EmptyTestInterface,CppMockObject> (CppMockObject.Instance);
                        cppip.Dispose ();
                        // should throw
                        Assert.Fail ();
                }

        }
}

