using System;

namespace CppSharp.Runtime
{
    public class Pointer<T>
    {
        public Pointer(IntPtr ptr) => this.ptr = ptr;

        public static implicit operator IntPtr(Pointer<T> pointer) => pointer.ptr;

        private readonly IntPtr ptr;
    }
}
