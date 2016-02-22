//----------------------------------------------------------------------------
// This is autogenerated code by CppSharp.
// Do not edit this file or all your changes will be lost after re-generation.
//----------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CppSharp
{
    namespace clang
    {
        public unsafe partial class DiagnosticMapping : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 4)]
            public partial struct Internal
            {
                [FieldOffset(0)]
                public uint Severity;

                [FieldOffset(0)]
                public uint IsUser;

                [FieldOffset(0)]
                public uint IsPragma;

                [FieldOffset(0)]
                public uint HasNoWarningAsError;

                [FieldOffset(0)]
                public uint HasNoErrorAsFatal;

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang17DiagnosticMappingC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang17DiagnosticMapping4MakeENS_4diag8SeverityEbb")]
                internal static extern clang.DiagnosticMapping.Internal Make_0(clang.diag.Severity Severity, bool IsUser, bool IsPragma);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang17DiagnosticMapping11getSeverityEv")]
                internal static extern clang.diag.Severity getSeverity_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang17DiagnosticMapping11setSeverityENS_4diag8SeverityE")]
                internal static extern void setSeverity_0(global::System.IntPtr instance, clang.diag.Severity Value);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang17DiagnosticMapping6isUserEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isUser_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang17DiagnosticMapping8isPragmaEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isPragma_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang17DiagnosticMapping19hasNoWarningAsErrorEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool hasNoWarningAsError_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang17DiagnosticMapping19setNoWarningAsErrorEb")]
                internal static extern void setNoWarningAsError_0(global::System.IntPtr instance, bool Value);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang17DiagnosticMapping17hasNoErrorAsFatalEv")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool hasNoErrorAsFatal_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang17DiagnosticMapping17setNoErrorAsFatalEb")]
                internal static extern void setNoErrorAsFatal_0(global::System.IntPtr instance, bool Value);
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DiagnosticMapping> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DiagnosticMapping>();

            private readonly bool __ownsNativeInstance;

            public static DiagnosticMapping __CreateInstance(global::System.IntPtr native)
            {
                return new DiagnosticMapping((DiagnosticMapping.Internal*) native);
            }

            public static DiagnosticMapping __CreateInstance(DiagnosticMapping.Internal native)
            {
                return new DiagnosticMapping(native);
            }

            private static DiagnosticMapping.Internal* __CopyValue(DiagnosticMapping.Internal native)
            {
                var ret = (DiagnosticMapping.Internal*) Marshal.AllocHGlobal(4);
                *ret = native;
                return ret;
            }

            private DiagnosticMapping(DiagnosticMapping.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected DiagnosticMapping(DiagnosticMapping.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public DiagnosticMapping()
            {
                __Instance = Marshal.AllocHGlobal(4);
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
                clang.DiagnosticMapping __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }

            public clang.diag.Severity getSeverity()
            {
                var __ret = Internal.getSeverity_0(__Instance);
                return __ret;
            }

            public void setSeverity(clang.diag.Severity Value)
            {
                var arg0 = Value;
                Internal.setSeverity_0(__Instance, arg0);
            }

            public bool isUser()
            {
                var __ret = Internal.isUser_0(__Instance);
                return __ret;
            }

            public bool isPragma()
            {
                var __ret = Internal.isPragma_0(__Instance);
                return __ret;
            }

            public bool hasNoWarningAsError()
            {
                var __ret = Internal.hasNoWarningAsError_0(__Instance);
                return __ret;
            }

            public void setNoWarningAsError(bool Value)
            {
                Internal.setNoWarningAsError_0(__Instance, Value);
            }

            public bool hasNoErrorAsFatal()
            {
                var __ret = Internal.hasNoErrorAsFatal_0(__Instance);
                return __ret;
            }

            public void setNoErrorAsFatal(bool Value)
            {
                Internal.setNoErrorAsFatal_0(__Instance, Value);
            }

            public static clang.DiagnosticMapping Make(clang.diag.Severity Severity, bool IsUser, bool IsPragma)
            {
                var arg0 = Severity;
                var __ret = Internal.Make_0(arg0, IsUser, IsPragma);
                return clang.DiagnosticMapping.__CreateInstance(__ret);
            }
        }

        /// <summary>
        /// <para>Used for handling and querying diagnostic IDs.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Used for handling and querying diagnostic IDs.</para>
        /// <para>///</para>
        /// <para>/// Can be used and shared by multiple Diagnostics for multiple
        /// translation units.</para>
        /// </remarks>
        public unsafe partial class DiagnosticIDs : IDisposable
        {
            [StructLayout(LayoutKind.Explicit, Size = 16)]
            public partial struct Internal
            {
                [FieldOffset(8)]
                public global::System.IntPtr CustomDiagInfo;

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDsC2Ev")]
                internal static extern void ctor_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDsC2ERKS0_")]
                internal static extern void cctor_1(global::System.IntPtr instance, global::System.IntPtr _0);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDsD2Ev")]
                internal static extern void dtor_0(global::System.IntPtr instance);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs15getCustomDiagIDENS0_5LevelEN4llvm9StringRefE")]
                internal static extern uint getCustomDiagID_0(global::System.IntPtr instance, clang.DiagnosticIDs.Level L, llvm.StringRef.Internal FormatString);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZNK5clang13DiagnosticIDs14getDescriptionEj")]
                internal static extern llvm.StringRef.Internal getDescription_0(global::System.IntPtr instance, uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs27isBuiltinWarningOrExtensionEj")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isBuiltinWarningOrExtension_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs23isDefaultMappingAsErrorEj")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isDefaultMappingAsError_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs13isBuiltinNoteEj")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isBuiltinNote_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs22isBuiltinExtensionDiagEj")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isBuiltinExtensionDiag_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs22isBuiltinExtensionDiagEjRb")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isBuiltinExtensionDiag_1(uint DiagID, bool* EnabledByDefault);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs23getWarningOptionForDiagEj")]
                internal static extern llvm.StringRef.Internal getWarningOptionForDiag_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs24getCategoryNumberForDiagEj")]
                internal static extern uint getCategoryNumberForDiag_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs21getNumberOfCategoriesEv")]
                internal static extern uint getNumberOfCategories_0();

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs21getCategoryNameFromIDEj")]
                internal static extern llvm.StringRef.Internal getCategoryNameFromID_0(uint CategoryID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs15isARCDiagnosticEj")]
                [return: MarshalAsAttribute(UnmanagedType.I1)]
                internal static extern bool isARCDiagnostic_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs27getDiagnosticSFINAEResponseEj")]
                internal static extern clang.DiagnosticIDs.SFINAEResponse getDiagnosticSFINAEResponse_0(uint DiagID);

                [SuppressUnmanagedCodeSecurity]
                [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                    EntryPoint="_ZN5clang13DiagnosticIDs16getNearestOptionENS_4diag6FlavorEN4llvm9StringRefE")]
                internal static extern llvm.StringRef.Internal getNearestOption_0(clang.diag.Flavor Flavor, llvm.StringRef.Internal Group);
            }

            /// <summary>
            /// <para>The level of the diagnostic, after it has been through
            /// mapping.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief The level of the diagnostic, after it has been through
            /// mapping.</para>
            /// </remarks>
            public enum Level : uint
            {
                Ignored = 0,
                Note = 1,
                Remark = 2,
                Warning = 3,
                Error = 4,
                Fatal = 5
            }

            /// <summary>
            /// <para>Enumeration describing how the emission of a diagnostic should be
            /// treated when it occurs during C++ template argument deduction.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Enumeration describing how the emission of a
            /// diagnostic should</para>
            /// <para>  /// be treated when it occurs during C++ template argument
            /// deduction.</para>
            /// </remarks>
            public enum SFINAEResponse : uint
            {
                /// <summary>The diagnostic should not be reported, but it should cause template argument deduction to fail.</summary>
                SFINAE_SubstitutionFailure = 0,
                /// <summary>The diagnostic should be suppressed entirely.</summary>
                SFINAE_Suppress = 1,
                /// <summary>The diagnostic should be reported.</summary>
                SFINAE_Report = 2,
                /// <summary>The diagnostic is an access-control diagnostic, which will be substitution failures in some contexts and reported in others.</summary>
                SFINAE_AccessControl = 3
            }

            public global::System.IntPtr __Instance { get; protected set; }
            public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DiagnosticIDs> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, DiagnosticIDs>();

            private readonly bool __ownsNativeInstance;

            public static DiagnosticIDs __CreateInstance(global::System.IntPtr native)
            {
                return new DiagnosticIDs((DiagnosticIDs.Internal*) native);
            }

            public static DiagnosticIDs __CreateInstance(DiagnosticIDs.Internal native)
            {
                return new DiagnosticIDs(native);
            }

            private static DiagnosticIDs.Internal* __CopyValue(DiagnosticIDs.Internal native)
            {
                var ret = (DiagnosticIDs.Internal*) Marshal.AllocHGlobal(16);
                *ret = native;
                return ret;
            }

            private DiagnosticIDs(DiagnosticIDs.Internal native)
                : this(__CopyValue(native))
            {
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
            }

            protected DiagnosticIDs(DiagnosticIDs.Internal* native, bool isInternalImpl = false)
            {
                __Instance = new global::System.IntPtr(native);
            }

            public DiagnosticIDs()
            {
                __Instance = Marshal.AllocHGlobal(16);
                __ownsNativeInstance = true;
                NativeToManagedMap[__Instance] = this;
                Internal.ctor_0(__Instance);
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
                clang.DiagnosticIDs __dummy;
                NativeToManagedMap.TryRemove(__Instance, out __dummy);
                if (__ownsNativeInstance || force)
                    Internal.dtor_0(__Instance);
                if (__ownsNativeInstance)
                    Marshal.FreeHGlobal(__Instance);
            }

            /// <summary>
            /// <para>Return an ID for a diagnostic with the specified format string
            /// and level.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return an ID for a diagnostic with the specified
            /// format string and</para>
            /// <para>  /// level.</para>
            /// <para>  ///</para>
            /// <para>  /// If this is the first request for this diagnostic, it is
            /// registered and</para>
            /// <para>  /// created, otherwise the existing ID is returned.</para>
            /// </remarks>
            public uint getCustomDiagID(clang.DiagnosticIDs.Level L, llvm.StringRef FormatString)
            {
                var arg0 = L;
                var arg1 = ReferenceEquals(FormatString, null) ? new llvm.StringRef.Internal() : *(llvm.StringRef.Internal*) (FormatString.__Instance);
                var __ret = Internal.getCustomDiagID_0(__Instance, arg0, arg1);
                return __ret;
            }

            /// <summary>
            /// <para>Given a diagnostic ID, return a description of the issue.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Given a diagnostic ID, return a description of the
            /// issue.</para>
            /// </remarks>
            public llvm.StringRef getDescription(uint DiagID)
            {
                var __ret = Internal.getDescription_0(__Instance, DiagID);
                return llvm.StringRef.__CreateInstance(__ret);
            }

            /// <summary>
            /// <para>Return true if the unmapped diagnostic levelof the specified
            /// diagnostic ID is a Warning or Extension.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return true if the unmapped diagnostic levelof the
            /// specified</para>
            /// <para>  /// diagnostic ID is a Warning or Extension.</para>
            /// <para>  ///</para>
            /// <para>  /// This only works on builtin diagnostics, not custom ones,
            /// and is not</para>
            /// <para>  /// legal to call on NOTEs.</para>
            /// </remarks>
            public static bool isBuiltinWarningOrExtension(uint DiagID)
            {
                var __ret = Internal.isBuiltinWarningOrExtension_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Return true if the specified diagnostic is mapped to errors by
            /// default.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return true if the specified diagnostic is mapped to
            /// errors by</para>
            /// <para>  /// default.</para>
            /// </remarks>
            public static bool isDefaultMappingAsError(uint DiagID)
            {
                var __ret = Internal.isDefaultMappingAsError_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Determine whether the given built-in diagnostic ID is a
            /// Note.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Determine whether the given built-in diagnostic ID is
            /// a Note.</para>
            /// </remarks>
            public static bool isBuiltinNote(uint DiagID)
            {
                var __ret = Internal.isBuiltinNote_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Determine whether the given built-in diagnostic ID is for an
            /// extension of some sort.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Determine whether the given built-in diagnostic ID is
            /// for an</para>
            /// <para>  /// extension of some sort.</para>
            /// </remarks>
            public static bool isBuiltinExtensionDiag(uint DiagID)
            {
                var __ret = Internal.isBuiltinExtensionDiag_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Determine whether the given built-in diagnostic ID is for an
            /// extension of some sort, and whether it is enabled by default.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Determine whether the given built-in diagnostic ID is
            /// for an</para>
            /// <para>  /// extension of some sort, and whether it is enabled by
            /// default.</para>
            /// <para>  ///</para>
            /// <para>  /// This also returns EnabledByDefault, which is set to
            /// indicate whether the</para>
            /// <para>  /// diagnostic is ignored by default (in which case -pedantic
            /// enables it) or</para>
            /// <para>  /// treated as a warning/error by default.</para>
            /// <para>  ///</para>
            /// </remarks>
            public static bool isBuiltinExtensionDiag(uint DiagID, ref bool EnabledByDefault)
            {
                fixed (bool* arg1 = &EnabledByDefault)
                {
                    var __ret = Internal.isBuiltinExtensionDiag_1(DiagID, arg1);
                    return __ret;
                }
            }

            /// <summary>
            /// <para>Return the lowest-level warning option that enables the specified
            /// diagnostic.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return the lowest-level warning option that enables
            /// the specified</para>
            /// <para>  /// diagnostic.</para>
            /// <para>  ///</para>
            /// <para>  /// If there is no -Wfoo flag that controls the diagnostic,
            /// this returns null.</para>
            /// </remarks>
            public static llvm.StringRef getWarningOptionForDiag(uint DiagID)
            {
                var __ret = Internal.getWarningOptionForDiag_0(DiagID);
                return llvm.StringRef.__CreateInstance(__ret);
            }

            /// <summary>
            /// <para>Return the category number that a specified DiagID belongs to, or
            /// 0 if no category.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return the category number that a specified \p DiagID
            /// belongs to,</para>
            /// <para>  /// or 0 if no category.</para>
            /// </remarks>
            public static uint getCategoryNumberForDiag(uint DiagID)
            {
                var __ret = Internal.getCategoryNumberForDiag_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Return the number of diagnostic categories.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return the number of diagnostic categories.</para>
            /// </remarks>
            public static uint getNumberOfCategories()
            {
                var __ret = Internal.getNumberOfCategories_0();
                return __ret;
            }

            /// <summary>
            /// <para>Given a category ID, return the name of the category.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Given a category ID, return the name of the
            /// category.</para>
            /// </remarks>
            public static llvm.StringRef getCategoryNameFromID(uint CategoryID)
            {
                var __ret = Internal.getCategoryNameFromID_0(CategoryID);
                return llvm.StringRef.__CreateInstance(__ret);
            }

            /// <summary>
            /// <para>Return true if a given diagnostic falls into an ARC diagnostic
            /// category.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Return true if a given diagnostic falls into an ARC
            /// diagnostic</para>
            /// <para>  /// category.</para>
            /// </remarks>
            public static bool isARCDiagnostic(uint DiagID)
            {
                var __ret = Internal.isARCDiagnostic_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Determines whether the given built-in diagnostic ID is for an
            /// error that is suppressed if it occurs during C++ template argument
            /// deduction.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Determines whether the given built-in diagnostic ID
            /// is</para>
            /// <para>  /// for an error that is suppressed if it occurs during C++
            /// template</para>
            /// <para>  /// argument deduction.</para>
            /// <para>  ///</para>
            /// <para>  /// When an error is suppressed due to SFINAE, the template
            /// argument</para>
            /// <para>  /// deduction fails but no diagnostic is emitted. Certain
            /// classes of</para>
            /// <para>  /// errors, such as those errors that involve C++ access
            /// control,</para>
            /// <para>  /// are not SFINAE errors.</para>
            /// </remarks>
            public static clang.DiagnosticIDs.SFINAEResponse getDiagnosticSFINAEResponse(uint DiagID)
            {
                var __ret = Internal.getDiagnosticSFINAEResponse_0(DiagID);
                return __ret;
            }

            /// <summary>
            /// <para>Get the diagnostic option with the closest edit distance to the
            /// given group name.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief Get the diagnostic option with the closest edit
            /// distance to the</para>
            /// <para>  /// given group name.</para>
            /// </remarks>
            public static llvm.StringRef getNearestOption(clang.diag.Flavor Flavor, llvm.StringRef Group)
            {
                var arg0 = Flavor;
                var arg1 = ReferenceEquals(Group, null) ? new llvm.StringRef.Internal() : *(llvm.StringRef.Internal*) (Group.__Instance);
                var __ret = Internal.getNearestOption_0(arg0, arg1);
                return llvm.StringRef.__CreateInstance(__ret);
            }
        }

        namespace diag
        {
            /// <summary>
            /// <para>Enum values that allow the client to map NOTEs, WARNINGs, and
            /// EXTENSIONs to either Ignore (nothing), Remark (emit a remark), Warning
            /// (emit a warning) or Error (emit as an error). It allows clients to map
            /// ERRORs to Error or Fatal (stop emitting diagnostics after this one).</para>
            /// </summary>
            /// <remarks>
            /// <para>/// Enum values that allow the client to map NOTEs, WARNINGs, and
            /// EXTENSIONs</para>
            /// <para>    /// to either Ignore (nothing), Remark (emit a remark),
            /// Warning</para>
            /// <para>    /// (emit a warning) or Error (emit as an error).  It allows
            /// clients to</para>
            /// <para>    /// map ERRORs to Error or Fatal (stop emitting diagnostics
            /// after this one).</para>
            /// </remarks>
            public enum Severity
            {
                /// <summary>Do not present this diagnostic, ignore it.</summary>
                Ignored = 1,
                /// <summary>Present this diagnostic as a remark.</summary>
                Remark = 2,
                /// <summary>Present this diagnostic as a warning.</summary>
                Warning = 3,
                /// <summary>Present this diagnostic as an error.</summary>
                Error = 4,
                /// <summary>Present this diagnostic as a fatal error.</summary>
                Fatal = 5
            }

            /// <summary>
            /// <para>Flavors of diagnostics we can emit. Used to filter for a
            /// particular kind of diagnostic (for instance, for -W/-R flags).</para>
            /// </summary>
            /// <remarks>
            /// <para>/// Flavors of diagnostics we can emit. Used to filter for a
            /// particular</para>
            /// <para>    /// kind of diagnostic (for instance, for -W/-R
            /// flags).</para>
            /// </remarks>
            public enum Flavor
            {
                /// <summary>A diagnostic that indicates a problem or potential problem. Can be made fatal by -Werror.</summary>
                WarningOrError = 0,
                /// <summary>A diagnostic that indicates normal progress through compilation.</summary>
                Remark = 1
            }

            public enum DIAG : uint
            {
                DIAG_START_COMMON = 0,
                DIAG_START_DRIVER = 300,
                DIAG_START_FRONTEND = 400,
                DIAG_START_SERIALIZATION = 500,
                DIAG_START_LEX = 620,
                DIAG_START_PARSE = 920,
                DIAG_START_AST = 1420,
                DIAG_START_COMMENT = 1520,
                DIAG_START_SEMA = 1620,
                DIAG_START_ANALYSIS = 4620,
                DIAG_UPPER_LIMIT = 4720
            }

            public enum _0 : uint
            {
                __COMMONSTART = 0,
                err_arcmt_nsinvocation_ownership = 1,
                err_attribute_not_type_attr = 2,
                err_cannot_open_file = 3,
                err_default_special_members = 4,
                err_deleted_non_function = 5,
                err_enum_template = 6,
                err_expected = 7,
                err_expected_after = 8,
                err_expected_colon_after_setter_name = 9,
                err_expected_either = 10,
                err_expected_namespace_name = 11,
                err_expected_string_literal = 12,
                err_file_modified = 13,
                err_integer_literal_too_large = 14,
                err_invalid_character_udl = 15,
                err_invalid_numeric_udl = 16,
                err_invalid_storage_class_in_func_decl = 17,
                err_invalid_string_udl = 18,
                err_module_build_disabled = 19,
                err_module_cycle = 20,
                err_module_file_conflict = 21,
                err_module_lock_failure = 22,
                err_module_lock_timeout = 23,
                err_module_not_built = 24,
                err_module_not_found = 25,
                err_mt_message = 26,
                err_param_redefinition = 27,
                err_seh___except_block = 28,
                err_seh___except_filter = 29,
                err_seh___finally_block = 30,
                err_seh_expected_handler = 31,
                err_target_unknown_abi = 32,
                err_target_unknown_cpu = 33,
                err_target_unknown_fpmath = 34,
                err_target_unknown_triple = 35,
                err_target_unsupported_fpmath = 36,
                err_target_unsupported_unaligned = 37,
                err_unable_to_make_temp = 38,
                err_unable_to_rename_temp = 39,
                err_unsupported_bom = 40,
                ext_c99_longlong = 41,
                ext_cxx11_longlong = 42,
                ext_integer_literal_too_large_for_signed = 43,
                ext_variadic_templates = 44,
                fatal_too_many_errors = 45,
                note_also_found = 46,
                note_decl_hiding_tag_type = 47,
                note_declared_at = 48,
                note_duplicate_case_prev = 49,
                note_forward_declaration = 50,
                note_invalid_subexpr_in_const_expr = 51,
                note_matching = 52,
                note_mt_message = 53,
                note_possibility = 54,
                note_pragma_entered_here = 55,
                note_previous_declaration = 56,
                note_previous_definition = 57,
                note_previous_implicit_declaration = 58,
                note_previous_use = 59,
                note_type_being_defined = 60,
                note_using = 61,
                warn_arcmt_nsalloc_realloc = 62,
                warn_cxx98_compat_longlong = 63,
                warn_cxx98_compat_variadic_templates = 64,
                warn_method_param_declaration = 65,
                warn_method_param_redefinition = 66,
                warn_mt_message = 67,
                NUM_BUILTIN_COMMON_DIAGNOSTICS = 68
            }

            /// <summary>
            /// <para>All of the diagnostics that can be emitted by the
            /// frontend.</para>
            /// </summary>
            /// <remarks>
            /// <para>/// \brief All of the diagnostics that can be emitted by the
            /// frontend.</para>
            /// </remarks>
        }
    }
}
