//
// Mono.VisualC.Interop.ABI.VTableManaged.cs: Managed vtable implementation
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
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop.ABI {
	public class VTableManaged : VTable {

		public static MakeVTableDelegate Implementation = metadata => { return new VTableManaged (metadata); };
		private VTableManaged (CppTypeInfo metadata) : base (metadata)
		{
			this.vtPtr = Marshal.AllocHGlobal ((EntryCount * EntrySize) + typeInfo.VTableTopPadding + typeInfo.VTableBottomPadding);
			WriteOverrides ();
		}

		public override T GetVirtualCallDelegate<T> (IntPtr native, int index)
		{
			IntPtr vtable = Marshal.ReadIntPtr (native);
			if (vtable == vtPtr) // do not return managed overrides
				vtable = basePtr;

			IntPtr ftnptr = Marshal.ReadIntPtr (vtable, (index * EntrySize) + typeInfo.VTableTopPadding);
			if (ftnptr == IntPtr.Zero)
				throw new NullReferenceException ("Native VTable contains null...possible abstract class???");

			Delegate del = Marshal.GetDelegateForFunctionPointer (ftnptr, typeof (T));
			return del as T;
		}

	}

	/*
	  protected static Type GetNativeLayoutType(MethodInfo thisMethod) {
	  ParameterInfo[] parameters = thisMethod.GetParameters();
	  if (parameters.Length < 1) return null;

	  Type nativeLayoutType = parameters[0].ParameterType.GetElementType();
	  return nativeLayoutType;
	  }
	*/
}
