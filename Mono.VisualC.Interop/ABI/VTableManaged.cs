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

namespace Mono.VisualC.Interop.ABI {
	public class VTableManaged : VTable {

		public VTableManaged (Delegate[] entries) : base(entries)
                {
		}

                public override int EntrySize {
                        get { return Marshal.SizeOf (typeof (IntPtr)); }
                }

		public override T GetDelegateForNative<T> (IntPtr native, int index)
                {
			IntPtr vtable = Marshal.ReadIntPtr (native);
			if (vtable == vtPtr) // do not return managed overrides
				vtable = basePtr;

			IntPtr ftnptr = Marshal.ReadIntPtr (vtable, index * EntrySize);
			if (ftnptr == IntPtr.Zero)
				return null;

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
