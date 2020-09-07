using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CppSharp.Runtime
{
    public static class Helpers
    {
        public static string MarshalEncodedString(IntPtr ptr, Encoding encoding)
        {
            if (ptr == IntPtr.Zero)
                return null;

            var size = 0;
            while (Marshal.ReadInt32(ptr, size) != 0)
                size += sizeof(int);

            var buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);

            return encoding.GetString(buffer);
        }

        public static IntPtr StringToHGlobalMultiByteUni(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            IntPtr nativePtr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, nativePtr, bytes.Length);

            return nativePtr;
        }
    }
}
