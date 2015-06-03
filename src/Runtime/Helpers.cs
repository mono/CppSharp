using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace CppSharp.Runtime
{
    public static class Helpers
    {
#if WINDOWS
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("libc", EntryPoint = "memcpy")]
#endif
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        public static ConcurrentDictionary<IntPtr, object> NativeToManagedMap
        {
            get { return nativeToManagedMap; }
        }

        private static readonly ConcurrentDictionary<IntPtr, object> nativeToManagedMap = new ConcurrentDictionary<IntPtr, object>();
    }
}
