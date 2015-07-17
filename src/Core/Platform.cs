using System;
using System.Runtime.InteropServices;

namespace CppSharp
{
    public static class Platform
    {
        public static bool IsWindows
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                        return true;
                }

                return false;
            }
        }

        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        public static bool IsMacOS
        {
            get
            {
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                    return false;

                IntPtr buf = Marshal.AllocHGlobal(8192);
                if (uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);
                    switch (os)
                    {
                        case "Darwin":
                            return true;
                    }
                }
                Marshal.FreeHGlobal(buf);

                return false;
            }
        }

        public static bool IsMono
        {
            get { return Type.GetType("Mono.Runtime") != null; }
        }
    }
}
