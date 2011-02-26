//
// Mono.VisualC.Interop.ABI.VTableManaged.cs: Managed vtable implementation
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

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
