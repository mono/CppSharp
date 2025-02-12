using System;
using System.Runtime.InteropServices;

namespace CppSharp
{
    public enum TargetPlatform
    {
        Windows,
        Linux,
        Android,
        MacOS,
        iOS,
        WatchOS,
        TVOS,
        Emscripten,
    }

    public static class Platform
    {
        public static bool IsWindows =>
            Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => true,
                PlatformID.Win32S => true,
                PlatformID.Win32Windows => true,
                PlatformID.WinCE => true,
                _ => false
            };

        public static bool IsMacOS { get; } = GetIsMacOS();

        private static bool GetIsMacOS()
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

            [DllImport("libc")]
            static extern int uname(IntPtr buf);
        }

        public static bool IsLinux => Environment.OSVersion.Platform == PlatformID.Unix && !IsMacOS;

        public static bool IsMono => Type.GetType("Mono.Runtime") != null;

        public static bool IsUnixPlatform
        {
            get
            {
                var platform = Environment.OSVersion.Platform;
                return platform is PlatformID.Unix or PlatformID.MacOSX;
            }
        }

        public static TargetPlatform Host { get; } = GetHostPlatform();

        private static TargetPlatform GetHostPlatform()
        {
            if (IsWindows)
                return TargetPlatform.Windows;

            if (IsMacOS)
                return TargetPlatform.MacOS;

            if (IsLinux)
                return TargetPlatform.Linux;

            throw new NotImplementedException();
        }
    }
}
