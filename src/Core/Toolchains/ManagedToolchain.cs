using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace CppSharp
{
    public static class ManagedToolchain
    {
        public static string ReadMonoPathFromWindowsRegistry(bool prefer64bits = false)
        {
            using (var baseKey = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine,
                prefer64bits ? RegistryView.Registry64 : RegistryView.Registry32))
            {
                if (baseKey == null)
                    return null;

                using (var subKey = baseKey.OpenSubKey (@"SOFTWARE\Mono"))
                {
                    if (subKey == null)
                        return null;

                    return (string)subKey.GetValue ("SdkInstallRoot", "");
                }
            }
        }

        public static string FindMonoPath(bool prefer64bits = false)
        {
            if (Platform.IsWindows)
            {
                // Try to lookup the Mono SDK path from registry.
                string path = ReadMonoPathFromWindowsRegistry(prefer64bits);
                if (!string.IsNullOrEmpty(path))
                   return path;

                // If that fails, then lets try a default location.
                return @"C:\Program Files (x86)\Mono";
            }
            else if (Platform.IsMacOS)
                return "/Library/Frameworks/Mono.framework/Versions/Current";
            else
                return "/usr";
        }

        public static string FindCSharpCompilerDir()
        {
            if (Platform.IsWindows)
            {
                List<ToolchainVersion> versions = MSVCToolchain.GetMSBuildSdks();
                if (versions.Count == 0)
                    throw new Exception("Could not find MSBuild SDK paths");

                var sdk = versions.Last();

                return sdk.Directory;
            }

            return FindMonoPath();
        }

        public static string FindCSharpCompilerPath()
        {
            return Path.Combine(FindCSharpCompilerDir(), "bin",
                Platform.IsWindows ? "csc.exe" : "mcs");
        }
    }
}
