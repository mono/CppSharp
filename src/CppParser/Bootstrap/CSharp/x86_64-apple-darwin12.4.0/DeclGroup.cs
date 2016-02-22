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
    }

    namespace clang
    {
        public unsafe partial class DeclGroup : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 8)]
            public partial struct Internal
            {
                [FieldOffset(0)]
                internal clang.DeclGroup._.Internal _0;

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang9DeclGroupC2ERKS0_")]
                internal static extern void cctor_2(global::System.IntPtr instance, global::System.IntPtr _0);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang9DeclGroup6CreateERNS_10ASTContextEPPNS_4DeclEj")]
                internal static extern global::System.IntPtr Create_0(global::System.IntPtr C, global::System.IntPtr Decls, uint NumDecls);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang9DeclGroup4sizeEv")]
                internal static extern uint size_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang9DeclGroupixEj")]
                internal static extern global::System.IntPtr OperatorSubscript_0(global::System.IntPtr instance, uint i);
            }

            internal unsafe partial struct _
            {
                [StructLayout(LayoutKind.Explicit, Size = 8)]
                public partial struct Internal
                {
                    [FieldOffset(0)]
                    public uint NumDecls;

                    [FieldOffset(0)]
                    public global::System.IntPtr Aligner;
                }
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DeclGroup> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DeclGroup>();

            private readonly bool __ownsNativeInstance;

            public static DeclGroup __CreateInstance(global::System.IntPtr native)
            {
                return new DeclGroup((DeclGroup.Internal*) native);
            }

            public static DeclGroup __CreateInstance(DeclGroup.Internal native)
            {
                return new DeclGroup(native);
            }

            private static DeclGroup.Internal* __CopyValue(DeclGroup.Internal native)
            {
                var ret = (DeclGroup.Internal*) Marshal.AllocHGlobal(8);
                *ret = native;
                return ret;
            }

            private DeclGroup(DeclGroup.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected DeclGroup(DeclGroup.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
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
                clang.DeclGroup __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }

            public uint size()
            {
                var __ret = Internal.size_0(__Instance);
                return __ret;
            }

            public static clang.DeclGroup Create(clang.ASTContext C, clang.Decl Decls, uint NumDecls)
            {
                var arg0 = ReferenceEquals(C, null) ? global::System.IntPtr.Zero : C.__Instance;
                var arg1 = ReferenceEquals(Decls, null) ? global::System.IntPtr.Zero : Decls.__Instance;
                var __ret = Internal.Create_0(arg0, arg1, NumDecls);
                clang.DeclGroup __result0;
                if (__ret == IntPtr.Zero) __result0 = null;
                else if (clang.DeclGroup.NativeToManagedMap.ContainsKey(__ret))
                    __result0 = (clang.DeclGroup) clang.DeclGroup.NativeToManagedMap[__ret];
                else __result0 = clang.DeclGroup.__CreateInstance(__ret);
                return __result0;
            }

            public clang.Decl this[uint i]
            {
                get
                {
                    var __ret = Internal.OperatorSubscript_0(__Instance, i);
                    clang.Decl __result0;
                    if (__ret == IntPtr.Zero) __result0 = null;
                    else if (clang.Decl.NativeToManagedMap.ContainsKey(__ret))
                        __result0 = (clang.Decl) clang.Decl.NativeToManagedMap[__ret];
                    else clang.Decl.NativeToManagedMap[__ret] = __result0 = (clang.Decl) clang.Decl.__CreateInstance(__ret);
                    return __result0;
                }

                set
                {
                    *(clang.Decl.Internal*) Internal.OperatorSubscript_0(__Instance, i) = *(clang.Decl.Internal*) value.__Instance;
                }
            }
        }

        public unsafe partial class DeclGroupRef : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 8)]
            public partial struct Internal
            {
                [FieldOffset(0)]
                public global::System.IntPtr D;

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRefC2Ev")]
                internal static extern void ctor_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRefC2EPNS_4DeclE")]
                internal static extern void ctor_1(global::System.IntPtr instance, global::System.IntPtr d);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRefC2EPNS_9DeclGroupE")]
                internal static extern void ctor_2(global::System.IntPtr instance, global::System.IntPtr dg);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRefC2ERKS0_")]
                internal static extern void cctor_3(global::System.IntPtr instance, global::System.IntPtr _0);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRef6CreateERNS_10ASTContextEPPNS_4DeclEj")]
                internal static extern clang.DeclGroupRef.Internal Create_0(global::System.IntPtr C, global::System.IntPtr Decls, uint NumDecls);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang12DeclGroupRef6isNullEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isNull_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang12DeclGroupRef12isSingleDeclEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isSingleDecl_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang12DeclGroupRef11isDeclGroupEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isDeclGroup_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRef13getSingleDeclEv")]
                internal static extern global::System.IntPtr getSingleDecl_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRef12getDeclGroupEv")]
                internal static extern global::System.IntPtr getDeclGroup_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRef5beginEv")]
                internal static extern global::System.IntPtr begin_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRef3endEv")]
                internal static extern global::System.IntPtr end_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang12DeclGroupRef14getAsOpaquePtrEv")]
                internal static extern global::System.IntPtr getAsOpaquePtr_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang12DeclGroupRef16getFromOpaquePtrEPv")]
                internal static extern clang.DeclGroupRef.Internal getFromOpaquePtr_0(global::System.IntPtr Ptr);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DeclGroupRef> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DeclGroupRef>();

            private readonly bool __ownsNativeInstance;

            public static DeclGroupRef __CreateInstance(global::System.IntPtr native)
            {
                return new DeclGroupRef((DeclGroupRef.Internal*) native);
            }

            public static DeclGroupRef __CreateInstance(DeclGroupRef.Internal native)
            {
                return new DeclGroupRef(native);
            }

            private static DeclGroupRef.Internal* __CopyValue(DeclGroupRef.Internal native)
            {
                var ret = (DeclGroupRef.Internal*) Marshal.AllocHGlobal(8);
                *ret = native;
                return ret;
            }

            private DeclGroupRef(DeclGroupRef.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected DeclGroupRef(DeclGroupRef.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public DeclGroupRef()
            {
                __Instance = Marshal.AllocHGlobal(8);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
                Internal.ctor_0(__Instance);
            }

            public DeclGroupRef(clang.Decl d)
            {
                __Instance = Marshal.AllocHGlobal(8);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
                var arg0 = ReferenceEquals(d, null) ? global::System.IntPtr.Zero : d.__Instance;
                Internal.ctor_1(__Instance, arg0);
            }

            public DeclGroupRef(clang.DeclGroup dg)
            {
                __Instance = Marshal.AllocHGlobal(8);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
                var arg0 = ReferenceEquals(dg, null) ? global::System.IntPtr.Zero : dg.__Instance;
                Internal.ctor_2(__Instance, arg0);
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
                clang.DeclGroupRef __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }

            public bool isNull()
            {
                var __ret = Internal.isNull_0(__Instance);
                return __ret;
            }

            public bool isSingleDecl()
            {
                var __ret = Internal.isSingleDecl_0(__Instance);
                return __ret;
            }

            public bool isDeclGroup()
            {
                var __ret = Internal.isDeclGroup_0(__Instance);
                return __ret;
            }

            public clang.Decl getSingleDecl()
            {
                var __ret = Internal.getSingleDecl_0(__Instance);
                clang.Decl __result0;
                if (__ret == IntPtr.Zero) __result0 = null;
                else if (clang.Decl.NativeToManagedMap.ContainsKey(__ret))
                    __result0 = (clang.Decl) clang.Decl.NativeToManagedMap[__ret];
                else clang.Decl.NativeToManagedMap[__ret] = __result0 = (clang.Decl) clang.Decl.__CreateInstance(__ret);
                return __result0;
            }

            public clang.DeclGroup getDeclGroup()
            {
                var __ret = Internal.getDeclGroup_0(__Instance);
                clang.DeclGroup __result0;
                if (__ret == IntPtr.Zero) __result0 = null;
                else if (clang.DeclGroup.NativeToManagedMap.ContainsKey(__ret))
                    __result0 = (clang.DeclGroup) clang.DeclGroup.NativeToManagedMap[__ret];
                else __result0 = clang.DeclGroup.__CreateInstance(__ret);
                return __result0;
            }

            public clang.Decl begin()
            {
                var __ret = Internal.begin_0(__Instance);
                clang.Decl __result0;
                if (__ret == IntPtr.Zero) __result0 = null;
                else if (clang.Decl.NativeToManagedMap.ContainsKey(__ret))
                    __result0 = (clang.Decl) clang.Decl.NativeToManagedMap[__ret];
                else clang.Decl.NativeToManagedMap[__ret] = __result0 = (clang.Decl) clang.Decl.__CreateInstance(__ret);
                return __result0;
            }

            public clang.Decl end()
            {
                var __ret = Internal.end_0(__Instance);
                clang.Decl __result0;
                if (__ret == IntPtr.Zero) __result0 = null;
                else if (clang.Decl.NativeToManagedMap.ContainsKey(__ret))
                    __result0 = (clang.Decl) clang.Decl.NativeToManagedMap[__ret];
                else clang.Decl.NativeToManagedMap[__ret] = __result0 = (clang.Decl) clang.Decl.__CreateInstance(__ret);
                return __result0;
            }

            public global::System.IntPtr getAsOpaquePtr()
            {
                var __ret = Internal.getAsOpaquePtr_0(__Instance);
                return __ret;
            }

            public static clang.DeclGroupRef Create(clang.ASTContext C, clang.Decl Decls, uint NumDecls)
            {
                var arg0 = ReferenceEquals(C, null) ? global::System.IntPtr.Zero : C.__Instance;
                var arg1 = ReferenceEquals(Decls, null) ? global::System.IntPtr.Zero : Decls.__Instance;
                var __ret = Internal.Create_0(arg0, arg1, NumDecls);
                return clang.DeclGroupRef.__CreateInstance(__ret);
            }

            public static clang.DeclGroupRef getFromOpaquePtr(global::System.IntPtr Ptr)
            {
                var arg0 = Ptr;
                var __ret = Internal.getFromOpaquePtr_0(arg0);
                return clang.DeclGroupRef.__CreateInstance(__ret);
            }
        }
    }
}
