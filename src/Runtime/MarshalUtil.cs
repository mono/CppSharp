using System;
using System.Text;

namespace CppSharp.Runtime
{
    public unsafe static class MarshalUtil
    {
        public static string GetString(Encoding encoding, IntPtr str)
        {
            if (str == IntPtr.Zero)
                return null;

            int byteCount = 0;

            if (encoding == Encoding.UTF32)
            {
                var str32 = (int*)str;
                while (*(str32++) != 0) byteCount += sizeof(int);
            }
            else if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
            {
                var str16 = (short*)str;
                while (*(str16++) != 0) byteCount += sizeof(short);
            }
            else
            {
                var str8 = (byte*)str;
                while (*(str8++) != 0) byteCount += sizeof(byte);
            }

            return encoding.GetString((byte*)str, byteCount);
        }
    }
}
