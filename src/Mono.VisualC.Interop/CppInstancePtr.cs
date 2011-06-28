//
// Mono.VisualC.Interop.CppInstancePtr.cs: Represents a pointer to a native C++ instance
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
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Mono.VisualC.Interop.ABI;

namespace Mono.VisualC.Interop {
	public struct CppInstancePtr : ICppObject {

		private IntPtr ptr, native_vtptr;
		private bool manage_memory;

		private static Dictionary<Type,object> implCache = null;
		private static Dictionary<IntPtr,int> managed_vtptr_to_gchandle_offset = null;

		// TODO: the managed instance argument may only be NULL if all methods in TWrapper
		//  that correspond to the virtual methods in Iface are static.
		public static CppInstancePtr ForManagedObject<Iface,TWrapper> (TWrapper managed)
			where Iface : ICppClassOverridable<TWrapper>
		{
			object cachedImpl;
			Iface impl;

			if (implCache == null)
				implCache = new Dictionary<Type,object> ();

			if (!implCache.TryGetValue (typeof (Iface), out cachedImpl))
			{
				VirtualOnlyAbi virtualABI = new VirtualOnlyAbi (VTable.BindToSignature);
				impl = virtualABI.ImplementClass<Iface> (typeof (TWrapper), new CppLibrary (string.Empty), string.Empty);
				implCache.Add (typeof (Iface), impl);
			}
			else
				impl = (Iface)cachedImpl;

			CppInstancePtr instance = impl.Alloc (managed);
			impl.TypeInfo.VTable.InitInstance (ref instance);

			return instance;
		}

		// Alloc a new C++ instance
		// NOTE: native_vtptr will be set later after native ctor is called
		internal CppInstancePtr (int nativeSize, object managedWrapper)
		{
			// Under the hood, we're secretly subclassing this C++ class to store a
			// handle to the managed wrapper.
			int allocSize = nativeSize + Marshal.SizeOf (typeof (IntPtr));
			ptr = Marshal.AllocHGlobal (allocSize);

			// zero memory for sanity
			// FIXME: This should be an initblk
			byte[] zeroArray = new byte [allocSize];
			Marshal.Copy (zeroArray, 0, ptr, allocSize);

			IntPtr handlePtr = MakeGCHandle (managedWrapper);
			Marshal.WriteIntPtr (ptr, nativeSize, handlePtr);

			manage_memory = true;
		}

		// Alloc a new C++ instance when there is no managed wrapper.
		internal CppInstancePtr (int nativeSize)
		{
			ptr = Marshal.AllocHGlobal (nativeSize);
			manage_memory = true;
		}

		// Get a CppInstancePtr for an existing C++ instance from an IntPtr
		public CppInstancePtr (IntPtr native)
		{
			if (native == IntPtr.Zero)
				throw new ArgumentOutOfRangeException ("native cannot be null pointer");

			ptr = native;
			manage_memory = false;
		}

		// Fulfills ICppObject requirement
		public CppInstancePtr (CppInstancePtr copy)
		{
			this.ptr = copy.ptr;
			this.native_vtptr = copy.native_vtptr;
			this.manage_memory = copy.manage_memory;
		}

		// Provide casts to/from IntPtr:
		public static implicit operator CppInstancePtr (IntPtr native)
		{
			return new CppInstancePtr (native);
		}

		// cast from CppInstancePtr -> IntPtr is explicit because we lose information
		public static explicit operator IntPtr (CppInstancePtr ip)
		{
			return ip.Native;
		}

		public IntPtr Native {
			get {
				if (ptr == IntPtr.Zero)
					throw new ObjectDisposedException ("CppInstancePtr");

				return ptr;
			}
		}

		// Internal for now to prevent attempts to read vtptr from non-virtual class
		internal IntPtr NativeVTable {
			get {

				if (native_vtptr == IntPtr.Zero) {
					// For pointers from native code...
					// Kludge! CppInstancePtr doesn't know whether this class is virtual or not, but we'll just assume that either
					//  way it's at least sizeof(void*) and read what would be the vtptr anyway. Supposedly, if it's not virtual,
					//  the wrappers won't use this field anyway...
					native_vtptr = Marshal.ReadIntPtr (ptr);
				}

				return native_vtptr;
			}
			set {
				native_vtptr = value;
			}
		}

		CppInstancePtr ICppObject.Native {
			get { return this; }
		}

		public bool IsManagedAlloc {
			get { return manage_memory; }
		}

		internal static void RegisterManagedVTable (VTable vtable)
		{
			if (managed_vtptr_to_gchandle_offset == null)
				managed_vtptr_to_gchandle_offset = new Dictionary<IntPtr, int> ();

			managed_vtptr_to_gchandle_offset [vtable.Pointer] = vtable.TypeInfo.NativeSize;
		}

		internal static IntPtr MakeGCHandle (object managedWrapper)
		{
			// TODO: Dispose() should probably be called at some point on this GCHandle.
			GCHandle handle = GCHandle.Alloc (managedWrapper, GCHandleType.Normal);
			return GCHandle.ToIntPtr (handle);
		}

		// This might be made public, but in this form it only works for classes with vtables.
		//  Returns null if the native ptr passed in does not appear to be a managed instance.
		//  (i.e. its vtable ptr is not in managed_vtptr_to_gchandle_offset)
		internal static T ToManaged<T> (IntPtr native) where T : class
		{
			if (managed_vtptr_to_gchandle_offset == null)
				return null;

			int gchOffset;
			if (!managed_vtptr_to_gchandle_offset.TryGetValue (Marshal.ReadIntPtr (native), out gchOffset))
				return null;

			return ToManaged<T> (native, gchOffset);
		}

		// WARNING! This method is not safe. DO NOT call
		// if we do not KNOW that this instance is managed.
		internal static T ToManaged<T> (IntPtr native, int nativeSize) where T : class
		{
			IntPtr handlePtr = Marshal.ReadIntPtr (native, nativeSize);
			GCHandle handle = GCHandle.FromIntPtr (handlePtr);

			return handle.Target as T;
		}

		// TODO: Free GCHandle?
		public void Dispose ()
		{
			if (manage_memory && ptr != IntPtr.Zero)
				Marshal.FreeHGlobal (ptr);

			ptr = IntPtr.Zero;
			manage_memory = false;
		}
	}
}
