//
// Mono.VisualC.Interop.CppTypeInfo.cs: Type metadata for C++ types
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using System.Runtime.InteropServices;

using Mono.VisualC.Interop.ABI;
using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop {

	// NOTE: As AddBase is called, properties change.
	//  TypeComplete indicates when the dust has settled.
	public class CppTypeInfo {

		public CppAbi Abi { get; private set; }
		public Type NativeLayout {get; private set; }

		public IList<MethodInfo> VirtualMethods { get; private set; }
		public LazyGeneratedList<Type> VTableDelegateTypes { get; private set; }
		public LazyGeneratedList<Delegate> VTableOverrides { get; private set; }

		public IList<CppTypeInfo> BaseClasses { get; private set; }

		// returns the number of vtable slots reserved for the
		//  base class(es)
		public int BaseVTableSlots { get; protected set; }

		public bool TypeComplete { get; private set; }

		protected int native_size;
		protected int field_offset_padding;

		private VTable lazy_vtable;

		public CppTypeInfo (CppAbi abi, IEnumerable<MethodInfo> virtualMethods, Type nativeLayout)
		{
			Abi = abi;
			NativeLayout = nativeLayout;

			VirtualMethods = new List<MethodInfo> (virtualMethods);
			VTableDelegateTypes = new LazyGeneratedList<Type> (VirtualMethods.Count, VTableDelegateTypeGenerator);
			VTableOverrides = new LazyGeneratedList<Delegate> (VirtualMethods.Count, VTableOverrideGenerator);

			BaseClasses = new List<CppTypeInfo> ();
			BaseVTableSlots = 0;
			TypeComplete = false;

			native_size = Marshal.SizeOf (nativeLayout);
			field_offset_padding = 0;

			lazy_vtable = null;
		}

		public virtual void AddBase (CppTypeInfo baseType)
		{
			if (TypeComplete)
				return;

			BaseClasses.Add (baseType);
			int newVirtualMethodCount = baseType.VirtualMethods.Count;

			for (int i = 0; i < newVirtualMethodCount; i++)
				VirtualMethods.Insert (BaseVTableSlots + i, baseType.VirtualMethods [i]);

			BaseVTableSlots += newVirtualMethodCount;
			VTableDelegateTypes.Skew (newVirtualMethodCount);
			VTableOverrides.Skew (newVirtualMethodCount);

			field_offset_padding += Marshal.SizeOf (baseType.NativeLayout) + baseType.field_offset_padding;
		}

		public int CountBases (Func<CppTypeInfo, bool> predicate)
		{
			int count = 0;
			foreach (var baseClass in BaseClasses) {
				count += baseClass.CountBases (predicate);
				count += predicate (baseClass)? 1 : 0;
			}
			return count;
		}

		public virtual void CompleteType ()
		{
			if (TypeComplete) return;
			foreach (var baseClass in BaseClasses)
				baseClass.CompleteType ();

			TypeComplete = true;

			// check that any virtual methods overridden in a subclass are only included once
			HashSet<MethodSignature> vsignatures = new HashSet<MethodSignature> ();
			for (int i = 0; i < VirtualMethods.Count; i++) {
				MethodSignature sig = GetVTableMethodSignature (i);

				if (vsignatures.Contains (sig))
					VirtualMethods.RemoveAt (i--);
				else
					vsignatures.Add (sig);
			}
		}

		public virtual T GetAdjustedVirtualCall<T> (IntPtr native, int derivedVirtualMethodIndex) where T : class /* Delegate */
		{
			return VTable.GetVirtualCallDelegate<T> (native, BaseVTableSlots + derivedVirtualMethodIndex);
		}

		//public virtual long GetFieldOffset (

		public virtual VTable VTable {
			get {
				CompleteType ();
				if (!VirtualMethods.Any ())
					return null;

				if (lazy_vtable == null)
					lazy_vtable = VTable.DefaultImplementation (this);

				return lazy_vtable;
			}
		}

		public virtual int NativeSize {
                        get {
				CompleteType ();
				return native_size + FieldOffsetPadding;
                        }
                }

		// the extra padding to allocate at the top of the class before the fields begin
		//  (by default, just the vtable pointer)
		public virtual int FieldOffsetPadding {
                        get { return field_offset_padding + (VirtualMethods.Any ()? Marshal.SizeOf (typeof (IntPtr)) : 0); }
                }

		// the padding in the data pointed to by the vtable pointer before the list of function pointers starts
		public virtual int VTableTopPadding {
			get { return 0; }
		}

		// the amount of extra room alloc'd after the function pointer list of the vtbl
		public virtual int VTableBottomPadding {
			get { return 0; }
		}

		public virtual MethodSignature GetVTableMethodSignature (int index)
		{
			MethodInfo method = VirtualMethods [index];
			return new MethodSignature () { Name = method.Name,
			                                Type = Abi.GetMethodType (method),
			                                Signature = GetVTableDelegateSignature (index) };
		}

		public virtual DelegateSignature GetVTableDelegateSignature (int index)
		{
			MethodInfo method = VirtualMethods [index];
			return new DelegateSignature () { ParameterTypes = Abi.GetParameterTypesForPInvoke (method),
			                                  ReturnType = method.ReturnType,
			                                  CallingConvention = Abi.GetCallingConvention (method) };
		}

		private Type VTableDelegateTypeGenerator (int index)
		{
			return DelegateTypeCache.GetDelegateType (GetVTableDelegateSignature (index));
		}

		private Delegate VTableOverrideGenerator (int index)
		{
			return Abi.GetManagedOverrideTrampoline (this, index);
		}
		
	}
}

