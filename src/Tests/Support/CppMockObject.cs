//
// CPPTestLibBase.cs: Base class for supporting tests using CPPTestLib
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

namespace Tests.Support {
        public class CppMockObject : ICppObject {

                public static CppMockObject Instance = new CppMockObject ();

                protected CppMockObject ()
                {
                }

                public IntPtr Native {
                        get {
                                throw new System.NotImplementedException ();
                        }
                }

                public int NativeSize {
                        get {
                                throw new NotImplementedException ();
                        }
                }

                public void Dispose ()
                {
                        throw new System.NotImplementedException ();
                }
                

        }

        public interface EmptyTestInterface : ICppClassOverridable<CppMockObject> {
        }

        public struct EmptyTestStruct {
        }
}

