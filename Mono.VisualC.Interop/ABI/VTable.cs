//
// Mono.VisualC.Interop.ABI.VTable.cs: abstract vtable
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
        public delegate VTable MakeVTableDelegate (IList<Type> delegateTypes, IList<Delegate> overrides, int paddingTop, int paddingBottom);

        // TODO: RTTI .. support virtual inheritance
        public abstract class VTable : IDisposable {

                public static MakeVTableDelegate DefaultImplementation = VTableManaged.Implementation;

		protected int paddingTop, paddingBottom;
                protected IntPtr basePtr, vtPtr;

		// FIXME: Either make this a lazy list that only generates the delegate type if
		//  it is needed, and/or cache generated delegate types (see FIXME in CppAbi::DefineVTableDelegate)
		protected IList<Type> delegate_types;

                // The Length of the array will be equal to the number of entries in the vtable;
		// entries that aren't overridden will be NULL.
                protected IList<Delegate> overrides;

                public virtual int EntryCount {
                        get { return overrides.Count; }
                }
                public virtual int EntrySize {
                        get { return Marshal.SizeOf (typeof (IntPtr)); }
                }

                public abstract MethodInfo PrepareVirtualCall (MethodInfo target, CallingConvention? callingConvention, ILGenerator callsite,
		                                               LocalBuilder nativePtr, FieldInfo vtableField, int vtableIndex);

                // Subclasses should allocate vtPtr and then call WriteOverrides
                public VTable (IList<Type> delegateTypes, IList<Delegate> overrides, int paddingTop, int paddingBottom)
                {
			this.delegate_types = delegateTypes;
                        this.overrides = overrides;
                        this.basePtr = IntPtr.Zero;
                        this.vtPtr = IntPtr.Zero;
			this.paddingTop = paddingTop;
			this.paddingBottom = paddingBottom;
                }

                protected virtual void WriteOverrides ()
                {
                        IntPtr vtEntryPtr;
			int currentOffset = paddingTop;
                        for (int i = 0; i < EntryCount; i++) {

                                if (overrides [i] != null) // managed override
                                        vtEntryPtr = Marshal.GetFunctionPointerForDelegate (overrides [i]);
                                else
                                        vtEntryPtr = IntPtr.Zero;

                                Marshal.WriteIntPtr (vtPtr, currentOffset, vtEntryPtr);
                                currentOffset += EntrySize;
                        }
                }

                public virtual void InitInstance (IntPtr instance)
                {
                       if (basePtr == IntPtr.Zero) {
                                basePtr = Marshal.ReadIntPtr (instance);
                                if ((basePtr != IntPtr.Zero) && (basePtr != vtPtr)) {
					// FIXME: This could probably be a more efficient memcpy
                                        for(int i = 0; i < paddingTop; i++)
                                                Marshal.WriteByte(vtPtr, i, Marshal.ReadByte(basePtr, i));

                                        int currentOffset = paddingTop;
                                        for (int i = 0; i < EntryCount; i++) {

                                                if (overrides [i] == null)
                                                        Marshal.WriteIntPtr (vtPtr, currentOffset, Marshal.ReadIntPtr (basePtr, currentOffset));

                                                currentOffset += EntrySize;
                                        }

					// FIXME: This could probably be a more efficient memcpy
                                        for(int i = 0; i < paddingBottom; i++)
                                                Marshal.WriteByte(vtPtr, currentOffset + i, Marshal.ReadByte(basePtr, currentOffset + i));
				}
                        }

                        Marshal.WriteIntPtr (instance, vtPtr);
                }

                public virtual void ResetInstance (IntPtr instance)
                {
                        Marshal.WriteIntPtr (instance, basePtr);
                }

                public IntPtr Pointer {
                        get { return vtPtr; }
                }

                protected virtual void Dispose (bool disposing)
                {
                        if (vtPtr != IntPtr.Zero) {
                                Marshal.FreeHGlobal (vtPtr);
                                vtPtr = IntPtr.Zero;
                        }
                }

                // TODO: This WON'T usually be called because VTables are associated with classes
                //  (not instances) and managed C++ class wrappers are staticly held?
                public void Dispose ()
                {
                        Dispose (true);
                        GC.SuppressFinalize (this);
                }

                ~VTable ()
                {
                        Dispose (false);
                }

                public static bool BindToSignatureAndAttribute (MemberInfo member, object obj)
                {
                        bool result = BindToSignature (member, obj);
                        if (member.GetCustomAttributes (typeof (OverrideNativeAttribute), true).Length != 1)
                                return false;

                        return result;
                }

                public static bool BindToSignature (MemberInfo member, object obj)
                {
                        MethodInfo imethod = (MethodInfo) obj;
                        MethodInfo candidate = (MethodInfo) member;

                        if (!candidate.Name.Equals (imethod.Name))
                                return false;

                        ParameterInfo[] invokeParams = imethod.GetParameters ();
                        ParameterInfo[] methodParams = candidate.GetParameters ();

                        if (invokeParams.Length == methodParams.Length) {
                                for (int i = 0; i < invokeParams.Length; i++) {
                                        if (!invokeParams [i].ParameterType.IsAssignableFrom (methodParams [i].ParameterType))
                                                return false;
                                }
                        } else if (invokeParams.Length == methodParams.Length + 1) {
                                for (int i = 1; i < invokeParams.Length; i++) {
                                        if (!invokeParams [i].ParameterType.IsAssignableFrom (methodParams [i - 1].ParameterType))
                                                return false;
                                }
                        } else
                                return false;

                        return true;
                }
        }
}
