//
// SharedAbiTests.cs: Test cases that are shared by all ABIs
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
        public class SharedAbiTests {

                protected CppLibrary test_lib { get; private set; }
                protected IVirtualMethodTestClass virtual_test_class { get; private set; }

                protected SharedAbiTests (CppAbi abi)
                {
                        this.test_lib = new CppLibrary ("CPPTestLib", abi);
                        this.virtual_test_class = test_lib.GetClass<IVirtualMethodTestClass> ("VirtualMethodTestClass");
                        CppNUnitAsserts.Init ();
                }

                [Test]
                public void TestVirtualMethods ()
                {
                        CppInstancePtr vmtc = VirtualMethodTestClass.Create ();

                        virtual_test_class.V0 (vmtc, 1, 2, 3);

                        VirtualMethodTestClass.Destroy (vmtc);
                }
                
        }
}

