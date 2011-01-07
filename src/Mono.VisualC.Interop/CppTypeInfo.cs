//
// Mono.VisualC.Interop.CppTypeInfo.cs: Type metadata for C++ types
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
		protected int field_offset_padding_without_vtptr;

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
			field_offset_padding_without_vtptr = 0;

			lazy_vtable = null;
		}

		public virtual void AddBase (CppTypeInfo baseType)
		{
			if (TypeComplete)
				return;

			// by default, do not add another vtable pointer for this new base class
			AddBase (baseType, false);
		}

		protected virtual void AddBase (CppTypeInfo baseType, bool addVTablePointer)
		{
			BaseClasses.Add (baseType);

			if (!addVTablePointer) {
				// If we're not adding a vtptr, then all this base class's virtual methods go in primary vtable
				// Skew the offsets of this subclass's vmethods to account for the new base vmethods.

				int newVirtualMethodCount = baseType.VirtualMethods.Count;
				for (int i = 0; i < newVirtualMethodCount; i++)
					VirtualMethods.Insert (BaseVTableSlots + i, baseType.VirtualMethods [i]);
	
				BaseVTableSlots += newVirtualMethodCount;
				VTableDelegateTypes.PrependLast (baseType.VTableDelegateTypes);
				VTableOverrides.PrependLast (baseType.VTableOverrides);
			}

			field_offset_padding_without_vtptr += baseType.native_size +
				(addVTablePointer? baseType.FieldOffsetPadding : baseType.field_offset_padding_without_vtptr);
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

		// FIXME: Make this thread safe?
		public virtual void CompleteType ()
		{
			if (TypeComplete)
				return;

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
			get { return field_offset_padding_without_vtptr + (VirtualMethods.Any ()? Marshal.SizeOf (typeof (IntPtr)) : 0); }
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
			var psig = Abi.GetPInvokeSignature (this, method);
			return new DelegateSignature () { ParameterTypes = psig.ParameterTypes,
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

