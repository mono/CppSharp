using System;
using System.IO;
using System.Linq;
using CppSharp.Parser;

namespace CppSharp
{
    public static class XcodeToolchain
    {
        public static string GetXcodePath()
        {
            var toolchains = Directory.EnumerateDirectories("/Applications", "Xcode*")
                .ToList();
            toolchains.Sort();

            var toolchainPath = toolchains.LastOrDefault();
            if (toolchainPath == null)
                throw new Exception("Could not find a valid Xcode SDK");

            return toolchainPath;
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

            if (includePath == null)
                throw new Exception("Could not find a valid C++ include folder");

            return includePath;
        }

        public static string GetXcodeBuiltinIncludesFolder()
        {
            var toolchainPath = GetXcodeToolchainPath();

            var includePaths = Directory.EnumerateDirectories(Path.Combine(toolchainPath,
                "usr/lib/clang")).ToList();
            var includePath = includePaths.LastOrDefault();

            if (includePath == null)
                throw new Exception("Could not find a valid Clang include folder");

            return Path.Combine(includePath, "include");
        }

        public static string GetXcodeIncludesFolder()
        {
            var toolchainPath = GetXcodePath();

            var sdkPaths = Directory.EnumerateDirectories(Path.Combine(toolchainPath,
                "Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs")).ToList();
            var sdkPath = sdkPaths.LastOrDefault();

            if (sdkPath == null)
                throw new Exception("Could not find a valid Mac SDK");

            return Path.Combine(sdkPath, "usr/include");
        }
    }

    public static partial class OptionsExtensions
    {
        public static void SetupXcode(this ParserOptions options)
        {
            var builtinsPath = XcodeToolchain.GetXcodeBuiltinIncludesFolder();
            options.addSystemIncludeDirs(builtinsPath);

            var cppIncPath = XcodeToolchain.GetXcodeCppIncludesFolder();
            options.addSystemIncludeDirs(cppIncPath);

            var includePath = XcodeToolchain.GetXcodeIncludesFolder();
            options.addSystemIncludeDirs(includePath);

            options.NoBuiltinIncludes = true;
            options.NoStandardIncludes = true;

            options.addArguments("-stdlib=libc++");
        }
    }
}