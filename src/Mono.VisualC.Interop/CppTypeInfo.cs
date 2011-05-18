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
using System.Collections.ObjectModel;

using System.Runtime.InteropServices;

using Mono.VisualC.Interop.ABI;
using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop {

	// NOTE: As AddBase is called, properties change.
	//  TypeComplete indicates when the dust has settled.
	public class CppTypeInfo {

		public CppAbi Abi { get; private set; }
		public Type NativeLayout {get; private set; }

		public IList<PInvokeSignature> VirtualMethods { get; private set; } // read only version
		protected List<PInvokeSignature> virtual_methods;

		public IList<Type> VTableDelegateTypes { get; private set; } // read only version
		protected LazyGeneratedList<Type> vt_delegate_types;

		public IList<Delegate> VTableOverrides { get; private set; } // read only version
		protected LazyGeneratedList<Delegate> vt_overrides;

		public IList<CppTypeInfo> BaseClasses { get; private set; } // read only version
		protected List<CppTypeInfo> base_classes;

		// returns the number of vtable slots reserved for the
		//  base class(es)
		public int BaseVTableSlots { get; protected set; }

		public bool TypeComplete { get; private set; }

		protected int native_size;
		protected int field_offset_padding_without_vtptr;

		private VTable lazy_vtable;

		public CppTypeInfo (CppAbi abi, IEnumerable<PInvokeSignature> virtualMethods, Type nativeLayout)
		{
			Abi = abi;
			NativeLayout = nativeLayout;

			virtual_methods = new List<PInvokeSignature> (virtualMethods);
			VirtualMethods = new ReadOnlyCollection<PInvokeSignature> (virtual_methods);

			vt_delegate_types = new LazyGeneratedList<Type> (virtual_methods.Count, i => DelegateTypeCache.GetDelegateType (virtual_methods [i]));
			VTableDelegateTypes = new ReadOnlyCollection<Type> (vt_delegate_types);

			vt_overrides = new LazyGeneratedList<Delegate> (virtual_methods.Count, i => Abi.GetManagedOverrideTrampoline (this, i));
			VTableOverrides = new ReadOnlyCollection<Delegate> (vt_overrides);

			base_classes = new List<CppTypeInfo> ();
			BaseClasses = new ReadOnlyCollection<CppTypeInfo> (base_classes);

			BaseVTableSlots = 0;
			TypeComplete = false;

			native_size = Marshal.SizeOf (nativeLayout);
			field_offset_padding_without_vtptr = 0;

			lazy_vtable = null;
		}

		public virtual void AddBase (CppTypeInfo baseType)
		{

			// by default, do not add another vtable pointer for this new base class
			AddBase (baseType, false);
		}

		protected virtual void AddBase (CppTypeInfo baseType, bool addVTablePointer)
		{
			if (TypeComplete)
				return;

			base_classes.Add (baseType);

			if (!addVTablePointer) {
				// If we're not adding a vtptr, then all this base class's virtual methods go in primary vtable
				// Skew the offsets of this subclass's vmethods to account for the new base vmethods.

				int newVirtualMethodCount = baseType.virtual_methods.Count;
				for (int i = 0; i < newVirtualMethodCount; i++)
					virtual_methods.Insert (BaseVTableSlots + i, baseType.virtual_methods [i]);
	
				BaseVTableSlots += newVirtualMethodCount;
				vt_delegate_types.PrependLast (baseType.vt_delegate_types);
				vt_overrides.PrependLast (baseType.vt_overrides);
			}

			field_offset_padding_without_vtptr += baseType.native_size +
				(addVTablePointer? baseType.FieldOffsetPadding : baseType.field_offset_padding_without_vtptr);
		}

		public int CountBases (Func<CppTypeInfo, bool> predicate)
		{
			int count = 0;
			foreach (var baseClass in base_classes) {
				count += baseClass.CountBases (predicate);
				count += predicate (baseClass)? 1 : 0;
			}
			return count;
		}

		public virtual void CompleteType ()
		{
			if (TypeComplete)
				return;

			foreach (var baseClass in base_classes)
				baseClass.CompleteType ();

			TypeComplete = true;
			RemoveVTableDuplicates (ms => true);
		}

		protected virtual void RemoveVTableDuplicates (Predicate<MethodSignature> pred)
		{
			// check that any virtual methods overridden in a subclass are only included once
			var vsignatures = new HashSet<MethodSignature> ();

			for (int i = 0; i < virtual_methods.Count; i++) {
				var sig = virtual_methods [i];
				if (sig == null)
					continue;

				if (vsignatures.Contains (sig)) {
					if (pred (sig))
						virtual_methods.RemoveAt (i--);
				} else {
					vsignatures.Add (sig);
				}
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
				if (!virtual_methods.Any ())
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
			get { return field_offset_padding_without_vtptr + (virtual_methods.Any ()? Marshal.SizeOf (typeof (IntPtr)) : 0); }
		}

		// the padding in the data pointed to by the vtable pointer before the list of function pointers starts
		public virtual int VTableTopPadding {
			get { return 0; }
		}

		// the amount of extra room alloc'd after the function pointer list of the vtbl
		public virtual int VTableBottomPadding {
			get { return 0; }
		}
		
	}
}

