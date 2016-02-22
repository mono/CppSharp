//----------------------------------------------------------------------------
// This is autogenerated code by CppSharp.
// Do not edit this file or all your changes will be lost after re-generation.
//----------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CppSharp
{
    public unsafe partial class SwapByteOrder
    {
        public partial struct Internal
        {
        }
    }

    namespace llvm
    {
        public unsafe partial class SwapByteOrder
        {
            public partial struct Internal
            {
            }
        }

        namespace sys
        {
            public unsafe partial class SwapByteOrder
            {
                public partial struct Internal
                {
                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys16SwapByteOrder_16Et")]
                    internal static extern ushort SwapByteOrder_16_0(ushort value);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys16SwapByteOrder_32Ej")]
                    internal static extern uint SwapByteOrder_32_0(uint value);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys16SwapByteOrder_64Ey")]
                    internal static extern ulong SwapByteOrder_64_0(ulong value);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEh")]
                    internal static extern byte getSwappedBytes_0(byte C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEc")]
                    internal static extern sbyte getSwappedBytes_2(sbyte C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEt")]
                    internal static extern ushort getSwappedBytes_3(ushort C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEs")]
                    internal static extern short getSwappedBytes_4(short C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEj")]
                    internal static extern uint getSwappedBytes_5(uint C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEi")]
                    internal static extern int getSwappedBytes_6(int C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEm")]
                    internal static extern ulong getSwappedBytes_7(ulong C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEl")]
                    internal static extern long getSwappedBytes_8(long C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEy")]
                    internal static extern ulong getSwappedBytes_9(ulong C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEx")]
                    internal static extern long getSwappedBytes_10(long C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEf")]
                    internal static extern float getSwappedBytes_11(float C);

                    [SuppressUnmanagedCodeSecurity]
                    [DllImport("CppSharp", CallingConvention = global::System.Runtime.InteropServices.CallingConvention.Cdecl,
                        EntryPoint="_ZN4llvm3sys15getSwappedBytesEd")]
                    internal static extern float getSwappedBytes_12(double C);
                }

                /// <summary>
                /// <para>SwapByteOrder_16 - This function returns a byte-swapped
                /// representation of the 16-bit argument.</para>
                /// </summary>
                /// <remarks>
                /// <para>/// SwapByteOrder_16 - This function returns a byte-swapped
                /// representation of</para>
                /// <para>/// the 16-bit argument.</para>
                /// </remarks>
                public static ushort SwapByteOrder_16(ushort value)
                {
                    var arg0 = value;
                    var __ret = Internal.SwapByteOrder_16_0(arg0);
                    return __ret;
                }

                /// <summary>
                /// <para>SwapByteOrder_32 - This function returns a byte-swapped
                /// representation of the 32-bit argument.</para>
                /// </summary>
                /// <remarks>
                /// <para>/// SwapByteOrder_32 - This function returns a byte-swapped
                /// representation of</para>
                /// <para>/// the 32-bit argument.</para>
                /// </remarks>
                public static uint SwapByteOrder_32(uint value)
                {
                    var arg0 = value;
                    var __ret = Internal.SwapByteOrder_32_0(arg0);
                    return __ret;
                }

                /// <summary>
                /// <para>SwapByteOrder_64 - This function returns a byte-swapped
                /// representation of the 64-bit argument.</para>
                /// </summary>
                /// <remarks>
                /// <para>/// SwapByteOrder_64 - This function returns a byte-swapped
                /// representation of</para>
                /// <para>/// the 64-bit argument.</para>
                /// </remarks>
                public static ulong SwapByteOrder_64(ulong value)
                {
                    var arg0 = value;
                    var __ret = Internal.SwapByteOrder_64_0(arg0);
                    return __ret;
                }

                public static byte getSwappedBytes(byte C)
                {
                    var __ret = Internal.getSwappedBytes_0(C);
                    return __ret;
                }

                public static sbyte getSwappedBytes(sbyte C)
                {
                    var __ret = Internal.getSwappedBytes_2(C);
                    return __ret;
                }

                public static ushort getSwappedBytes(ushort C)
                {
                    var __ret = Internal.getSwappedBytes_3(C);
                    return __ret;
                }

                public static short getSwappedBytes(short C)
                {
                    var __ret = Internal.getSwappedBytes_4(C);
                    return __ret;
                }

                public static uint getSwappedBytes(uint C)
                {
                    var __ret = Internal.getSwappedBytes_5(C);
                    return __ret;
                }

                public static int getSwappedBytes(int C)
                {
                    var __ret = Internal.getSwappedBytes_6(C);
                    return __ret;
                }

                public static ulong getSwappedBytes(ulong C)
                {
                    var __ret = Internal.getSwappedBytes_7(C);
                    return __ret;
                }

                public static long getSwappedBytes(long C)
                {
                    var __ret = Internal.getSwappedBytes_8(C);
                    return __ret;
                }

                public static ulong getSwappedBytes(ulong C)
                {
                    var __ret = Internal.getSwappedBytes_9(C);
                    return __ret;
                }

                public static long getSwappedBytes(long C)
                {
                    var __ret = Internal.getSwappedBytes_10(C);
                    return __ret;
                }

                public static float getSwappedBytes(float C)
                {
                    var __ret = Internal.getSwappedBytes_11(C);
                    return __ret;
                }

                public static float getSwappedBytes(double C)
                {
                    var __ret = Internal.getSwappedBytes_12(C);
                    return __ret;
                }
            }
        }
    }
}
