//
// Mono.VisualC.Interop.CppInstancePtr.cs: Represents a pointer to a native C++ instance
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop {
        public struct CppInstancePtr : ICppObject {
                private IntPtr ptr;
                private bool manageMemory;

                // Alloc a new C++ instance
                internal CppInstancePtr (int nativeSize, object managedWrapper)
                {
                        // Under the hood, we're secretly subclassing this C++ class to store a
                        // handle to the managed wrapper.
                        ptr = Marshal.AllocHGlobal (nativeSize + Marshal.SizeOf (typeof (IntPtr)));

                        IntPtr handlePtr = GetGCHandle (managedWrapper);
                        Marshal.WriteIntPtr (ptr, nativeSize, handlePtr);

                        manageMemory = true;
                }

                // Alloc a new C++ instance when there is no managed wrapper.
                internal CppInstancePtr (int nativeSize)
                {
                        ptr = Marshal.AllocHGlobal (nativeSize);
                        manageMemory = true;
                }

                // Get a CppInstancePtr for an existing C++ instance from an IntPtr
                public CppInstancePtr (IntPtr native)
                {
                        if (native == IntPtr.Zero)
                                throw new ArgumentOutOfRangeException ("native cannot be null pointer");

                        ptr = native;
                        manageMemory = false;
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

                public bool IsManagedAlloc {
                        get { return manageMemory; }
                }

                internal static IntPtr GetGCHandle (object managedWrapper)
                {
                        // TODO: Dispose() should probably be called at some point on this GCHandle.
                        GCHandle handle = GCHandle.Alloc (managedWrapper, GCHandleType.Normal);
                        return GCHandle.ToIntPtr (handle);
                }

                // WARNING! This method is not safe. DO NOT call
                // if we do not KNOW that this instance is managed.
                internal static T GetManaged<T> (IntPtr native, int nativeSize) where T : class
                {

                        IntPtr handlePtr = Marshal.ReadIntPtr (native, nativeSize);
                        GCHandle handle = GCHandle.FromIntPtr (handlePtr);

                        return handle.Target as T;
                }

                // TODO: Free GCHandle?
                public void Dispose ()
                {
                        if (manageMemory && ptr != IntPtr.Zero)
                                Marshal.FreeHGlobal (ptr);

                        ptr = IntPtr.Zero;
                        manageMemory = false;
                }
        }
}
