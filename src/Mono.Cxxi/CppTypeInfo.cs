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
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Runtime.InteropServices;

using Mono.Cxxi.Abi;
using Mono.Cxxi.Util;

namespace Mono.Cxxi {

	// NOTE: As AddBase is called, properties change.
	//  TypeComplete indicates when the dust has settled.
	public class CppTypeInfo {

		public CppLibrary Library { get; private set; }

		public string TypeName { get; private set; }
		public bool IsPrimaryBase { get; protected set; } // < True by default. set to False in cases where it is cloned as a non-primary base

		// returns the number of vtable slots reserved for the
		//  base class(es) before this class's virtual methods start
		public int BaseVTableSlots { get; protected set; }

		public Type InterfaceType { get; private set; }
		public Type NativeLayout { get; private set; }
		public Type WrapperType { get; private set; }

		// read only versions:
		public IList<PInvokeSignature> VirtualMethods { get; private set; }
		public IList<Type> VTableDelegateTypes { get; private set; }
		public IList<Delegate> VTableOverrides { get; private set; }
		public IList<CppTypeInfo> BaseClasses { get; private set; }

		// backing lists:
		protected List<PInvokeSignature> virtual_methods;
		protected LazyGeneratedList<Type> vt_delegate_types;
		protected LazyGeneratedList<Delegate> vt_overrides;
		protected List<CppTypeInfo> base_classes;

		protected int native_size_without_padding; // <- this refers to the size of all the fields declared in the nativeLayout struct
		protected int field_offset_padding_without_vtptr;
		protected int gchandle_offset_delta;

		private VTable lazy_vtable;

		internal EmitInfo emit_info; // <- will be null when the type is done being emitted
		public bool TypeComplete { get { return emit_info == null; } }

		public CppTypeInfo (CppLibrary lib, string typeName, Type interfaceType, Type nativeLayout, Type/*?*/ wrapperType)
			: this ()
		{
			Library = lib;
			TypeName = typeName;

			InterfaceType = interfaceType;
			NativeLayout = nativeLayout;
			WrapperType = wrapperType;

			virtual_methods = new List<PInvokeSignature> (Library.Abi.GetVirtualMethodSlots (this, interfaceType));
			VirtualMethods = new ReadOnlyCollection<PInvokeSignature> (virtual_methods);

			vt_delegate_types = new LazyGeneratedList<Type> (virtual_methods.Count, i => DelegateTypeCache.GetDelegateType (virtual_methods [i]));
			VTableDelegateTypes = new ReadOnlyCollection<Type> (vt_delegate_types);

			vt_overrides = new LazyGeneratedList<Delegate> (virtual_methods.Count, i => Library.Abi.GetManagedOverrideTrampoline (this, i));
			VTableOverrides = new ReadOnlyCollection<Delegate> (vt_overrides);

			if (nativeLayout != null)
				native_size_without_padding = nativeLayout.GetFields ().Any ()? Marshal.SizeOf (nativeLayout) : 0;
		}

		protected CppTypeInfo ()
		{
			base_classes = new List<CppTypeInfo> ();
			BaseClasses = new ReadOnlyCollection<CppTypeInfo> (base_classes);

			field_offset_padding_without_vtptr = 0;
			gchandle_offset_delta = 0;
			IsPrimaryBase = true;
			BaseVTableSlots = 0;
			lazy_vtable = null;

			emit_info = new EmitInfo ();
		}

		// The contract for Clone is that, if TypeComplete, working with the clone *through the public
		//  interface* is guaranteed not to affect the original. Note that any subclassses
		// have access to protected stuff that is not covered by this guarantee.
		public virtual CppTypeInfo Clone ()
		{
			return this.MemberwiseClone () as CppTypeInfo;
		}

		#region Type Layout

		// the extra padding to allocate at the top of the class before the fields begin
		//  (by default, just the vtable pointer)
		public virtual int FieldOffsetPadding {
			get { return field_offset_padding_without_vtptr + (virtual_methods.Count != 0? IntPtr.Size : 0); }
		}

		public virtual int NativeSize {
			get { return native_size_without_padding + FieldOffsetPadding; }
		}

		public virtual int GCHandleOffset {
			get { return NativeSize + gchandle_offset_delta; }
		}


		public void AddBase (CppTypeInfo baseType)
		{
			// by default, only the primary base shares the subclass's primary vtable
			AddBase (baseType, base_classes.Count >= 1);
		}

		protected virtual void AddBase (CppTypeInfo baseType, bool addVTable)
		{
			if (TypeComplete)
				return;

			bool nonPrimary = base_classes.Count >= 1;

			// by default, only the primary base shares the subclass's vtable ptr
			var addVTablePointer = addVTable || nonPrimary;
			var baseVMethodCount = baseType.virtual_methods.Count;

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

			if (nonPrimary) {
				// Create a base-in-derived type info w/ a new vtable object
				// if this is a non-primary base

				baseType = baseType.Clone ();
				baseType.IsPrimaryBase = false;

				// offset all previously added bases
				foreach (var previousBase in base_classes) {
					previousBase.gchandle_offset_delta += baseType.NativeSize;
				}

				// offset derived (this) type's gchandle
				gchandle_offset_delta += baseType.GCHandleOffset;

				baseType.gchandle_offset_delta += native_size_without_padding + CountBases (b => !b.IsPrimaryBase) * IntPtr.Size;
				baseType.vt_overrides = baseType.vt_overrides.Clone (); // managed override tramps will be regenerated with correct gchandle offset
			}

			base_classes.Add (baseType);

			field_offset_padding_without_vtptr += baseType.native_size_without_padding +
				(addVTablePointer? baseType.FieldOffsetPadding : baseType.field_offset_padding_without_vtptr);
		}

		public virtual void CompleteType ()
		{
			if (emit_info == null)
				return;

			foreach (var baseClass in base_classes)
				baseClass.CompleteType ();

			emit_info = null;

			RemoveVTableDuplicates ();
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

		#endregion

		#region Casting

		protected virtual CppTypeInfo GetCastInfo (Type sourceType, Type targetType, out int offset)
		{
			offset = 0;

			if (WrapperType.Equals (targetType)) {
				// check for downcast (base type -> this type)

				foreach (var baseClass in base_classes) {
					if (baseClass.WrapperType.Equals (sourceType)) {
						return baseClass;
					}
					offset -= baseClass.NativeSize;
				}


			} else if (WrapperType.IsAssignableFrom (sourceType)) {
				// check for upcast (this type -> base type)

				foreach (var baseClass in base_classes) {
					if (baseClass.WrapperType.Equals (targetType)) {
						return baseClass;
					}
					offset += baseClass.NativeSize;
				}

			} else {
				throw new ArgumentException ("Either source type or target type must be equal to this wrapper type.");
			}

			throw new InvalidCastException ("Cannot cast an instance of " + sourceType + " to " + targetType);
		}

		public virtual CppInstancePtr Cast (ICppObject instance, Type targetType)
		{
			int offset;
			var baseTypeInfo = GetCastInfo (instance.GetType (), targetType, out offset);
			var result = new CppInstancePtr (instance.Native, offset);

			if (offset > 0 && instance.Native.IsManagedAlloc && baseTypeInfo.VirtualMethods.Count != 0) {
				// we might need to paste the managed base-in-derived vtptr here --also inits native_vtptr
				baseTypeInfo.VTable.InitInstance (ref result);
			}

			return result;
		}

		public virtual TTarget Cast<TTarget> (ICppObject instance) where TTarget : class
		{
			TTarget result;
			var ptr = Cast (instance, typeof (TTarget));

			// Check for existing instance based on vtable ptr
			result = CppInstancePtr.ToManaged<TTarget> (ptr.Native);

			// Create a new wrapper if necessary
			if (result == null)
				result = Activator.CreateInstance (typeof (TTarget), ptr) as TTarget;

			return result;
		}

		public virtual void InitNonPrimaryBase (ICppObject baseInDerived, ICppObject derived, Type baseType)
		{
			int offset;
			var baseTypeInfo = GetCastInfo (derived.GetType (), baseType, out offset);

			Marshal.WriteIntPtr (baseInDerived.Native.Native, baseTypeInfo.GCHandleOffset, CppInstancePtr.MakeGCHandle (baseInDerived));
		}

		#endregion

		#region V-Table

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

		// the padding in the data pointed to by the vtable pointer before the list of function pointers starts
		public virtual int VTableTopPadding {
			get { return 0; }
		}

		// the amount of extra room alloc'd after the function pointer list of the vtbl
		public virtual int VTableBottomPadding {
			get { return 0; }
		}

		public virtual T GetAdjustedVirtualCall<T> (CppInstancePtr instance, int derivedVirtualMethodIndex)
			where T : class /* Delegate */
		{
			return VTable.GetVirtualCallDelegate<T> (instance, BaseVTableSlots + derivedVirtualMethodIndex);
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

		#endregion
	}

	// This is used internally by CppAbi:
	internal class DummyCppTypeInfo : CppTypeInfo {

		public CppTypeInfo BaseTypeInfo { get; set; }

		protected override void AddBase (CppTypeInfo baseType, bool addVT)
		{
			BaseTypeInfo = baseType;
		}
	}
}

