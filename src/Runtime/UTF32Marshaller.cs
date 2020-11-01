using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CppSharp.Runtime
{
    public class UTF32Marshaller : ICustomMarshaler
    {
        static private UTF32Marshaller marshaler;

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            var p = (Int32*)pNativeData;
            int count = 0;
            while (*p++ != 0)
                ++count;
            return Encoding.UTF32.GetString((byte*)pNativeData, count * 4);
        }

        public unsafe IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (!(ManagedObj is string @string))
                return IntPtr.Zero;

            var capacity = @string.Length * 4 + 4;
            var result = Marshal.AllocCoTaskMem(capacity);
            var byteCount = 0;
            fixed (char* stringPtr = @string)
                byteCount = Encoding.UTF32.GetBytes(stringPtr, @string.Length, (byte*)result, capacity);
            *(Int32*)(result + byteCount) = 0;
            return result;
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeCoTaskMem(pNativeData);
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public static ICustomMarshaler GetInstance(string pstrCookie)
        {
            if (marshaler == null)
                marshaler = new UTF32Marshaller();
            return marshaler;
        }
    }
}
