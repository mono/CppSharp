using System;
using System.Runtime.InteropServices;
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

        public static T[] GetArray<T>(void* array, int size) where T : unmanaged
        {
            if (array == null)
                return null;
            var result = new T[size];
            fixed (void* fixedResult = result)
                Buffer.MemoryCopy(array, fixedResult, sizeof(T) * size, sizeof(T) * size);
            return result;
        }

        public static char[] GetCharArray(sbyte* array, int size)
        {
            if (array == null)
                return null;
            var result = new char[size];
            for (var i = 0; i < size; ++i)
                result[i] = Convert.ToChar(array[i]);
            return result;
        }

        public static IntPtr[] GetIntPtrArray(IntPtr* array, int size)
        {
            return GetArray<IntPtr>(array, size);
        }

        public static T GetDelegate<T>(IntPtr[] vtables, short table, int i) where T : class
        {
            var slot = *(IntPtr*)(vtables[table] + i * sizeof(IntPtr));
            return Marshal.GetDelegateForFunctionPointer<T>(slot);
        }
    }
}
