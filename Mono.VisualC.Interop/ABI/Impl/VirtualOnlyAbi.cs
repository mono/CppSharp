//
// Mono.VisualC.Interop.ABI.VirtualOnlyAbi.cs: A generalized C++ ABI that only supports virtual methods
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop.ABI {

        public class VirtualOnlyAbi : CppAbi {

                public VirtualOnlyAbi (MemberFilter vtableOverrideFilter)
                {
                        this.vtable_override_filter = vtableOverrideFilter;
                }
                public VirtualOnlyAbi () { }

                public override MethodType GetMethodType (MethodInfo imethod)
                {
                        MethodType defaultType = base.GetMethodType (imethod);
                        if (defaultType == MethodType.NativeCtor || defaultType == MethodType.NativeDtor)
                                return MethodType.NoOp;
                        return defaultType;
                }

                protected override string GetMangledMethodName (MethodInfo methodInfo)
                {
                        throw new NotSupportedException ("Name mangling is not supported by this class. All C++ interface methods must be declared virtual.");
                }

                public override CallingConvention? GetCallingConvention (MethodInfo methodInfo)
		{
			// Use platform default
                        return null;
                }
        }
}

