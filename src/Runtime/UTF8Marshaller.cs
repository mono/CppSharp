using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CppSharp.Runtime
{

    // HACK: .NET Standard 2.0 which we use in auto-building to support .NET Framework, lacks UnmanagedType.LPUTF8Str
    public class UTF8StringMarshaller : ICustomMarshaler
    {

        private static ICustomMarshaler _instance;
        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData != IntPtr.Zero)
                Marshal.FreeHGlobal(pNativeData);
        }

        public int GetNativeDataSize() => -1;

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;

            if (!(managedObj is string))
                throw new MarshalDirectiveException("UTF8StringMarshaler must be used on a string");

            byte[] strbuf = Encoding.UTF8.GetBytes((string)managedObj);
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length + 1);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);
            Marshal.WriteByte(buffer + strbuf.Length, 0); // Null terminator
            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr str)
        {
            if (str == IntPtr.Zero)
                return null;

            int byteCount = 0;
            var str8 = (byte*)str;
            while (*(str8++) != 0) byteCount += sizeof(byte);

            return Encoding.UTF8.GetString((byte*)str, byteCount);
        }

        public static ICustomMarshaler GetInstance(string pstrCookie) => _instance ??= new UTF8StringMarshaller();
    }

    public class UTF8CharMarshaller : ICustomMarshaler
    {
        private static ICustomMarshaler _instance;

        public void CleanUpManagedData(object managedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData){
            if (pNativeData != IntPtr.Zero)
                Marshal.FreeHGlobal(pNativeData);
        }

        public int GetNativeDataSize() => -1;

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (!(managedObj is char c))
                throw new MarshalDirectiveException("UTF8CharMarshaller must be used on a char");

            byte[] charbuf = Encoding.UTF8.GetBytes(new[] { c });
            IntPtr buffer = Marshal.AllocHGlobal(charbuf.Length + 1);
            Marshal.Copy(charbuf, 0, buffer, charbuf.Length);
            Marshal.WriteByte(buffer + charbuf.Length, 0); // Null terminator
            return buffer;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            byte[] charBytes = new byte[4];
            int byteCount = 0;

            while (byteCount < charBytes.Length && Marshal.ReadByte(pNativeData + byteCount) != 0)
            {
                charBytes[byteCount] = Marshal.ReadByte(pNativeData + byteCount);
                byteCount++;
            }

            string decodedChar = Encoding.UTF8.GetString(charBytes, 0, byteCount);
            if (decodedChar.Length != 1)
                throw new InvalidOperationException("The UTF-8 byte sequence does not represent a single char.");

            return decodedChar[0];
        }

        public static ICustomMarshaler GetInstance(string pstrCookie) => _instance ??= new UTF8CharMarshaller();
    }
}

