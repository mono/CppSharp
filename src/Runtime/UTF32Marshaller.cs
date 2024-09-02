using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CppSharp.Runtime
{
    public class UTF32StringMarshaller : ICustomMarshaler
    {
        private static ICustomMarshaler _instance;

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
                return string.Empty;

            var p = (int*)pNativeData;
            int count = 0;
            while (*p++ != 0)
                ++count;
            return Encoding.UTF32.GetString((byte*)pNativeData, count * 4);
        }

        public unsafe IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;

            if (!(managedObj is string str))
                throw new MarshalDirectiveException("UTF32StringMarshaller must be used on string");

            int capacity = (str.Length * 4) + 4;
            IntPtr result = Marshal.AllocCoTaskMem(capacity);

            fixed (char* stringPtr = str)
            {
                int byteCount = Encoding.UTF32.GetBytes(stringPtr, str.Length, (byte*)result, capacity);
                Marshal.WriteInt32(result + byteCount, 0);
            }

            return result;
        }

        public void CleanUpNativeData(IntPtr pNativeData){
            if (pNativeData != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pNativeData);
        }


        public void CleanUpManagedData(object managedObj)
        {
        }

        public int GetNativeDataSize() => -1;


        public static ICustomMarshaler GetInstance(string pstrCookie) => _instance ??= new UTF32StringMarshaller();
    }

    public class UTF32CharMarshaller : ICustomMarshaler
    {
        private static ICustomMarshaler _instance;

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pNativeData);
        }

        public int GetNativeDataSize() => 8;

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (!(managedObj is char c))
                throw new MarshalDirectiveException("UTF32CharMarshaller must be used on a char");

            IntPtr pNativeData = Marshal.AllocCoTaskMem(8);
            int utf32Char = char.ConvertToUtf32(c.ToString(), 0);
            Marshal.WriteInt32(pNativeData, utf32Char);
            Marshal.WriteInt32(pNativeData + 4, 0); // Null terminator
            return pNativeData;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
                return '\0';

            int utf32Char = Marshal.ReadInt32(pNativeData);
            return utf32Char == 0 ? '\0' : char.ConvertFromUtf32(utf32Char)[0];
        }

        public static ICustomMarshaler GetInstance(string pstrCookie) => _instance ??= new UTF32CharMarshaller();
    }
}