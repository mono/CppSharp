using System;
using NUnit.Framework;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Tests.Support {
        public class CPPTestLibBase : ICppObject {

                protected CppLibrary TestLib { get; private set; }

                protected CPPTestLibBase ()
                {
                }

                protected CPPTestLibBase (CppAbi abi)
                {
                        this.TestLib = new CppLibrary ("CPPTestLib", abi);
                }


                protected CSimpleClass.ICSimpleClass Klass {
                        get {
                                if (CSimpleClass._impl == null)
                                        CSimpleClass.Bind (TestLib);
                                return CSimpleClass._impl;
                        }
                }
                public IntPtr Native {
                        get {
                                throw new System.NotImplementedException ();
                        }
                }

                public void Dispose ()
                {
                        throw new System.NotImplementedException ();
                }
                

        }

        public interface EmptyTestInterface : ICppClassOverridable<CPPTestLibBase> {
        }

        public struct EmptyTestStruct {
        }
}

