//
// CppLibraryTests.cs: Test cases to exercise CppLibrary
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using NUnit.Framework;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

using Tests.Support;

namespace Tests {
        [TestFixture]
        public class CppLibraryTests {

                [Test]
                [ExpectedException (typeof (ArgumentNullException))]
                public void TestCreateNullAbi ()
                {
                        CppLibrary cppl = new CppLibrary ("foo", null);
                }

                [Test]
                [ExpectedException (typeof (ArgumentNullException))]
                public void TestCreateNullLibName ()
                {
                        CppLibrary cppl = new CppLibrary (null, new VirtualOnlyAbi ());
                }

                [Test]
                public void TestProperties ()
                {
                        VirtualOnlyAbi abi = new VirtualOnlyAbi ();
                        CppLibrary cppl = new CppLibrary ("FooLib", abi);
                        Assert.AreEqual ("FooLib", cppl.Name, "#A1");
                        Assert.AreSame (abi, cppl.Abi, "#A2");
                }

                [Test]
                public void TestGetClass ()
                {
                        VirtualOnlyAbi abi = new VirtualOnlyAbi ();
                        CppLibrary cppl = new CppLibrary ("FooLib", abi);
                        EmptyTestInterface klass = cppl.GetClass<EmptyTestInterface,EmptyTestStruct,CppMockObject> ("FooClass");
                        Assert.IsNotNull (klass, "#A1");
                }
        }
}

