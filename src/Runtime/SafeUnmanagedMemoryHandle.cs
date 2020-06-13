using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace CppSharp.Runtime
{
    // https://stackoverflow.com/a/17563315/
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public sealed class SafeUnmanagedMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeUnmanagedMemoryHandle() : base(true) { }

        public SafeUnmanagedMemoryHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle) => SetHandle(preexistingHandle);

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(handle);
                handle = IntPtr.Zero;
                return true;
            }
            return false;
        }
    }
}
