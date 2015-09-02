using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace CppSharp.Runtime
{
    public static class Helpers
    {
        public static string MarshalEncodedString(IntPtr ptr, Encoding encoding)
        {
            var size = 0;
            while (Marshal.ReadInt32(ptr, size) != 0)
                size += sizeof(int);

            var buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);

            return encoding.GetString(buffer);
        }

#if WINDOWS
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("libc", EntryPoint = "memcpy")]
#endif
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);
    }
}
