//
// Mono.VisualC.Interop.ABI.MsvcAbi.cs: An implementation of the Microsoft Visual C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop.ABI {
        public class MsvcAbi : CppAbi {
                public MsvcAbi ()
                {
                }

                public override CallingConvention DefaultCallingConvention {
                        get {
                                throw new System.NotImplementedException ();
                        }
                }

                public override string GetMangledMethodName (MethodInfo methodInfo)
                {
                        throw new System.NotImplementedException ();
                }

        }
}

