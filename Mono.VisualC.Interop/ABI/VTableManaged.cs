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

                public static MakeVTableDelegate Implementation = (types, overrides) => { return new VTableManaged (types, overrides); };
		private VTableManaged (IList<Type> types, Delegate [] overrides) : base(types, overrides)
                {
                        this.vtPtr = Marshal.AllocHGlobal (EntryCount * EntrySize);

                        WriteOverrides (0);
		}

                public override MethodInfo PrepareVirtualCall (MethodInfo target, CallingConvention? callingConvention, ILGenerator callsite,
		                                               LocalBuilder nativePtr, FieldInfo vtableField, int vtableIndex)
                {
                        Type delegateType = delegate_types [vtableIndex];
                        MethodInfo getDelegate = typeof (VTableManaged).GetMethod ("GetDelegateForNative").MakeGenericMethod (delegateType);

                        // load this._vtable
                        callsite.Emit (OpCodes.Ldarg_0);
                        callsite.Emit (OpCodes.Ldfld, vtableField);
                        // call this._vtable.GetDelegateForNative(IntPtr native, int vtableIndex)
                        callsite.Emit (OpCodes.Ldloc_S, nativePtr);
                        callsite.Emit (OpCodes.Ldc_I4, vtableIndex);
                        callsite.Emit (OpCodes.Callvirt, getDelegate);
                        // check for null
                        Label notNull = callsite.DefineLabel ();
                        callsite.Emit (OpCodes.Dup);
                        callsite.Emit (OpCodes.Brtrue_S, notNull);
                        callsite.Emit (OpCodes.Pop);
                        // mono bug makes it crash here instead of throwing a NullReferenceException--so do it ourselves
                        callsite.Emit (OpCodes.Ldstr, "Native VTable contains null...possible abstract class???");
                        callsite.Emit (OpCodes.Newobj, typeof (NullReferenceException).GetConstructor (new Type[] {typeof (string)}));
                        callsite.Emit (OpCodes.Throw);
                        callsite.MarkLabel (notNull);

                        return Util.GetMethodInfoForDelegate (delegateType);

                }


		public virtual T GetDelegateForNative<T> (IntPtr native, int index) where T : class /*Delegate*/
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
