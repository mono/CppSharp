using System;
using NUnit.Framework;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Tests {
        public class CPPTestLibBase {

                protected CppLibrary CPPTestLib { get; private set; }

                protected CPPTestLibBase (CppAbi abi)
                {
                        this.CPPTestLib = new CppLibrary ("CPPTestLib", abi);
                }
        }
}

