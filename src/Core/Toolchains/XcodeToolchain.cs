using System;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace CppSharp
{
    public static class XcodeToolchain
    {
        public static string GetXcodePath()
        {
            return GetXcodePathFromXcodeSelect() ?? GetXcodePathFromFileSystem();
        }

        public static string GetXcodeToolchainPath()
        {
            var toolchainPath = GetXcodePath();

            var toolchains = Directory.EnumerateDirectories(Path.Combine(toolchainPath,
                "Contents/Developer/Toolchains")).ToList();
            toolchains.Sort();

            toolchainPath = toolchains.LastOrDefault();
            if (toolchainPath == null)
                throw new Exception("Could not find a valid Xcode toolchain");

            return toolchainPath;
        }   

        public static string GetXcodeCppIncludesFolder()
        {
            var toolchainPath = GetXcodeToolchainPath();

            var includePath = Path.Combine(toolchainPath, "usr/include/c++/v1");

            if (includePath == null || !Directory.Exists(includePath))
                throw new Exception($"Could not find a valid C++ include folder: {includePath}");

            return includePath;
        }

        public static string GetXcodeBuiltinIncludesFolder()
        {
            var toolchainPath = GetXcodeToolchainPath();

            var includePaths = Directory.EnumerateDirectories(Path.Combine(toolchainPath,
                "usr/lib/clang")).ToList();
            var includePath = includePaths.LastOrDefault();

            if (includePath == null || !Directory.Exists(includePath))
                throw new Exception($"Could not find a valid Clang builtins folder: {includePath}");

            return Path.Combine(includePath, "include");
        }

        public static string GetXcodeIncludesFolder()
        {
            var toolchainPath = GetXcodePath();

            var sdkPaths = Directory.EnumerateDirectories(Path.Combine(toolchainPath,
                "Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs"))
                .Where(dir => dir.Contains("MacOSX.sdk")).ToList();
            var sdkPath = sdkPaths.LastOrDefault();

            if (sdkPath == null || !Directory.Exists(sdkPath))
                throw new Exception($"Could not find a valid Mac SDK folder: {sdkPath}");

            return Path.Combine(sdkPath, "usr/include");
        }

        private static string GetXcodePathFromXcodeSelect() 
        {
            try 
            {
                var xcodeSelect = Process.Start(new ProcessStartInfo("xcode-select", "--print-path") {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });

                var result = xcodeSelect.StandardOutput.ReadToEnd();

                if(!string.IsNullOrEmpty(result)) 
                {
                    var extraStuffIndex = result.LastIndexOf("/Contents/Developer");
                    if(extraStuffIndex >= 0)
                        return result.Remove(extraStuffIndex);
                }
            } catch {
                // TODO: Log exception?
            }
            return null;
        }

        private static string GetXcodePathFromFileSystem() 
        {
            var toolchains = Directory.EnumerateDirectories("/Applications", "Xcode*")
                .ToList();
            toolchains.Sort();

            var toolchainPath = toolchains.LastOrDefault();
            if (toolchainPath == null)
                throw new Exception("Could not find a valid Xcode SDK");

            return toolchainPath;
        }
    }
}