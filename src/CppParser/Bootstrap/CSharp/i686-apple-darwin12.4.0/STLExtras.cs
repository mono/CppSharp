//----------------------------------------------------------------------------
// This is autogenerated code by CppSharp.
// Do not edit this file or all your changes will be lost after re-generation.
//----------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CppSharp
{
    namespace llvm
    {
        [UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public unsafe delegate int __AnonymousDelegate0(global::System.IntPtr _0, global::System.IntPtr _1);

        [UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]
        public unsafe delegate int __AnonymousDelegate1(global::System.IntPtr _0, global::System.IntPtr _1);

        public unsafe static partial class identity
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        public unsafe static partial class less_ptr
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        public unsafe static partial class greater_ptr
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        public unsafe partial class mapped_iterator
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        /// <summary>
        /// <para>Represents a compile-time sequence of integers.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Represents a compile-time sequence of integers.</para>
        /// </remarks>
        public unsafe static partial class integer_sequence
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        /// <summary>
        /// <para>Alias for the common case of a sequence of size_ts.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Alias for the common case of a sequence of
        /// size_ts.</para>
        /// </remarks>
        public unsafe static partial class index_sequence
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        public unsafe static partial class build_index_impl
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        /// <summary>
        /// <para>Creates a compile-time integer sequence for a parameter
        /// pack.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Creates a compile-time integer sequence for a
        /// parameter pack.</para>
        /// </remarks>
        public unsafe static partial class index_sequence_for
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        public unsafe static partial class pair_hash
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        /// <summary>
        /// <para>Binary functor that adapts to any other binary functor after
        /// dereferencing operands.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// Binary functor that adapts to any other binary functor after
        /// dereferencing</para>
        /// <para>/// operands.</para>
        /// </remarks>
        public unsafe partial class deref
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
            }
        }

        /// <summary>
        /// <para>Function object to check whether the first component of a
        /// std::pair compares less than the first component of another
        /// std::pair.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Function object to check whether the first component
        /// of a std::pair</para>
        /// <para>/// compares less than the first component of another
        /// std::pair.</para>
        /// </remarks>
        public unsafe partial class less_first : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN4llvm10less_firstC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, less_first> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, less_first>();

            private readonly bool __ownsNativeInstance;

            public static less_first __CreateInstance(global::System.IntPtr native)
            {
                return new less_first((less_first.Internal*) native);
            }

            public static less_first __CreateInstance(less_first.Internal native)
            {
                return new less_first(native);
            }

            private static less_first.Internal* __CopyValue(less_first.Internal native)
            {
                var ret = (less_first.Internal*) Marshal.AllocHGlobal(0);
                *ret = native;
                return ret;
            }

            private less_first(less_first.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected less_first(less_first.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public less_first()
            {
                __Instance = Marshal.AllocHGlobal(0);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
            }

            protected virtual void Dispose(bool disposing)
            {
                DestroyNativeInstance(false);
            }

            public virtual void DestroyNativeInstance()
            {
                DestroyNativeInstance(true);
            }

            private void DestroyNativeInstance(bool force)
            {
                llvm.less_first __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }
        }

        /// <summary>
        /// <para>Function object to check whether the second component of a
        /// std::pair compares less than the second component of another
        /// std::pair.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Function object to check whether the second component
        /// of a std::pair</para>
        /// <para>/// compares less than the second component of another
        /// std::pair.</para>
        /// </remarks>
        public unsafe partial class less_second : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN4llvm11less_secondC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, less_second> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, less_second>();

            private readonly bool __ownsNativeInstance;

            public static less_second __CreateInstance(global::System.IntPtr native)
            {
                return new less_second((less_second.Internal*) native);
            }

            public static less_second __CreateInstance(less_second.Internal native)
            {
                return new less_second(native);
            }

            private static less_second.Internal* __CopyValue(less_second.Internal native)
            {
                var ret = (less_second.Internal*) Marshal.AllocHGlobal(0);
                *ret = native;
                return ret;
            }

            private less_second(less_second.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected less_second(less_second.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public less_second()
            {
                __Instance = Marshal.AllocHGlobal(0);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
            }

            protected virtual void Dispose(bool disposing)
            {
                DestroyNativeInstance(false);
            }

            public virtual void DestroyNativeInstance()
            {
                DestroyNativeInstance(true);
            }

            private void DestroyNativeInstance(bool force)
            {
                llvm.less_second __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }
        }

        public unsafe partial class FreeDeleter : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN4llvm11FreeDeleterC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, FreeDeleter> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, FreeDeleter>();

            private readonly bool __ownsNativeInstance;

            public static FreeDeleter __CreateInstance(global::System.IntPtr native)
            {
                return new FreeDeleter((FreeDeleter.Internal*) native);
            }

            public static FreeDeleter __CreateInstance(FreeDeleter.Internal native)
            {
                return new FreeDeleter(native);
            }

            private static FreeDeleter.Internal* __CopyValue(FreeDeleter.Internal native)
            {
                var ret = (FreeDeleter.Internal*) Marshal.AllocHGlobal(0);
                *ret = native;
                return ret;
            }

            private FreeDeleter(FreeDeleter.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected FreeDeleter(FreeDeleter.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public FreeDeleter()
            {
                __Instance = Marshal.AllocHGlobal(0);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
            }

            protected virtual void Dispose(bool disposing)
            {
                DestroyNativeInstance(false);
            }

            public virtual void DestroyNativeInstance()
            {
                DestroyNativeInstance(true);
            }

            private void DestroyNativeInstance(bool force)
            {
                llvm.FreeDeleter __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }
        }

        /// <summary>
        /// <para>A functor like C++14's std::less&lt;void&gt; in its
        /// absence.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// A functor like C++14's std::less&lt;void&gt; in its
        /// absence.</para>
        /// </remarks>
        public unsafe partial class less : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN4llvm4lessC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, less> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, less>();

            private readonly bool __ownsNativeInstance;

            public static less __CreateInstance(global::System.IntPtr native)
            {
                return new less((less.Internal*) native);
            }

            public static less __CreateInstance(less.Internal native)
            {
                return new less(native);
            }

            private static less.Internal* __CopyValue(less.Internal native)
            {
                var ret = (less.Internal*) Marshal.AllocHGlobal(0);
                *ret = native;
                return ret;
            }

            private less(less.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected less(less.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public less()
            {
                __Instance = Marshal.AllocHGlobal(0);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
            }

            protected virtual void Dispose(bool disposing)
            {
                DestroyNativeInstance(false);
            }

            public virtual void DestroyNativeInstance()
            {
                DestroyNativeInstance(true);
            }

            private void DestroyNativeInstance(bool force)
            {
                llvm.less __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }
        }

        /// <summary>
        /// <para>A functor like C++14's std::equal&lt;void&gt; in its
        /// absence.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// A functor like C++14's std::equal&lt;void&gt; in its
        /// absence.</para>
        /// </remarks>
        public unsafe partial class equal : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN4llvm5equalC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, equal> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, equal>();

            private readonly bool __ownsNativeInstance;

            public static equal __CreateInstance(global::System.IntPtr native)
            {
                return new equal((equal.Internal*) native);
            }

            public static equal __CreateInstance(equal.Internal native)
            {
                return new equal(native);
            }

            private static equal.Internal* __CopyValue(equal.Internal native)
            {
                var ret = (equal.Internal*) Marshal.AllocHGlobal(0);
                *ret = native;
                return ret;
            }

            private equal(equal.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected equal(equal.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public equal()
            {
                __Instance = Marshal.AllocHGlobal(0);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
            }

            protected virtual void Dispose(bool disposing)
            {
                DestroyNativeInstance(false);
            }

            public virtual void DestroyNativeInstance()
            {
                DestroyNativeInstance(true);
            }

            private void DestroyNativeInstance(bool force)
            {
                llvm.equal __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }
        }
    }
}
