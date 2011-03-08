//
// Mono.VisualC.Interop.ABI.VirtualOnlyAbi.cs: A generalized C++ ABI that only supports virtual methods
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010 Alexander Corrado
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

