//
// Mono.Cxxi.CppTypeInfo.cs: Type metadata for C++ types
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010-2011 Alexander Corrado
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

using Mono.Cxxi.Abi;
using Mono.Cxxi.Util;

namespace Mono.Cxxi {

	// NOTE: As AddBase is called, properties change.
	//  TypeComplete indicates when the dust has settled.
	public class CppTypeInfo {

		public CppAbi Abi { get; private set; }
		public Type NativeLayout { get; private set; }
		public Type WrapperType { get; private set; }

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

		public CppTypeInfo (CppAbi abi, IEnumerable<PInvokeSignature> virtualMethods, Type nativeLayout, Type/*?*/ wrapperType)
		{
			Abi = abi;
			NativeLayout = nativeLayout;
			WrapperType = wrapperType;

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

			native_size = nativeLayout.GetFields ().Any ()? Marshal.SizeOf (nativeLayout) : 0;
			field_offset_padding_without_vtptr = 0;

			lazy_vtable = null;
		}

		protected CppTypeInfo ()
		{
		}

		public virtual void AddBase (CppTypeInfo baseType)
		{
			// by default, if we already have base class(es), this base's virtual methods are not included before the derived class's
			var addVTable = base_classes.Count >= 1;
			AddBase (baseType, addVTable);
		}

		protected virtual void AddBase (CppTypeInfo baseType, bool addVTable)
		{
			if (TypeComplete)
				return;

			base_classes.Add (baseType);
			bool addVTablePointer = addVTable || base_classes.Count > 1;
			int  baseVMethodCount = baseType.virtual_methods.Count;

			if (addVTable) {
				// If we are adding a new vtable, don't skew the offsets of the of this subclass's methods.
				//  Instead append the new virtual methods to the end.

				for (int i = 0; i < baseVMethodCount; i++)
					virtual_methods.Add (baseType.virtual_methods [i]);

				vt_delegate_types.Add (baseVMethodCount);
				vt_overrides.Add (baseVMethodCount);

			} else {

				// If we're not adding a new vtable, then all this base class's virtual methods go in primary vtable
				// Skew the offsets of this subclass's vmethods to account for the new base vmethods.

				for (int i = 0; i < baseVMethodCount; i++)
					virtual_methods.Insert (BaseVTableSlots + i, baseType.virtual_methods [i]);
	
				BaseVTableSlots += baseVMethodCount;
				vt_delegate_types.Add (baseVMethodCount);
				vt_overrides.Add (baseVMethodCount);
			}

			field_offset_padding_without_vtptr += baseType.native_size +
				(addVTablePointer? baseType.FieldOffsetPadding : baseType.field_offset_padding_without_vtptr);
		}

		public virtual CppInstancePtr Cast (ICppObject instance, Type targetType)
		{
			var found = false;
			int offset = 0;

			foreach (var baseClass in base_classes) {
				if (baseClass.WrapperType.Equals (targetType)) {
					found = true;
					break;
				}

				offset += baseClass.NativeSize;
			}

			if (!found)
				throw new InvalidCastException ("Cannot cast an instance of " + instance.GetType () + " to " + targetType);

			// Construct a new targetType wrapper, passing in our offset this ptr.
			// FIXME: If the object was alloc'd by managed code, the allocating wrapper (i.e. "instance") will still free the memory when
			//  it is Disposed, even if wrappers created here still exist and are pointing to it. :/
			// The casted wrapper created here may be Disposed safely though.
			// FIXME: On NET_4_0 use IntPtr.Add
			return new CppInstancePtr (new IntPtr (instance.Native.Native.ToInt64 () + offset));
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

			RemoveVTableDuplicates ();
		}

		protected virtual void RemoveVTableDuplicates ()
		{
			// check that any virtual methods overridden in a subclass are only included once
			var vsignatures = new Dictionary<MethodSignature,PInvokeSignature> (MethodSignature.EqualityComparer);

			for (int i = 0; i < virtual_methods.Count; i++) {
				var sig = virtual_methods [i];
				if (sig == null)
					continue;

				PInvokeSignature existing;
				if (vsignatures.TryGetValue (sig, out existing)) {
					OnVTableDuplicate (ref i, sig, existing);
				} else {
					vsignatures.Add (sig, sig);
				}
			}
		}

		protected virtual bool OnVTableDuplicate (ref int iter, PInvokeSignature sig, PInvokeSignature dup)
		{
			// This predicate ensures that duplicates are only removed
			// if declared in different classes (i.e. overridden methods).
			// We usually want to allow the same exact virtual methods to appear
			// multiple times, in the case of nonvirtual diamond inheritance, for example.
			if (!sig.OrigMethod.Equals (dup.OrigMethod)) {
				virtual_methods.RemoveAt (iter--);
				vt_overrides.Remove (1);
				vt_delegate_types.Remove (1);
				return true;
			}

			return false;
		}

		public virtual T GetAdjustedVirtualCall<T> (CppInstancePtr instance, int derivedVirtualMethodIndex)
			where T : class /* Delegate */
		{
			return VTable.GetVirtualCallDelegate<T> (instance, BaseVTableSlots + derivedVirtualMethodIndex);
		}

		public virtual VTable VTable {
			get {
				CompleteType ();
				if (!virtual_methods.Any ())
					return null;

				if (lazy_vtable == null)
					lazy_vtable = new VTable (this);

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

	// This is used internally by CppAbi:
	internal class DummyCppTypeInfo : CppTypeInfo {

		public CppTypeInfo BaseTypeInfo { get; set; }

		public override void AddBase (CppTypeInfo baseType)
		{
			BaseTypeInfo = baseType;
		}
	}
}

