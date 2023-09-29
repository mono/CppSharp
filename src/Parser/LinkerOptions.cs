using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CppSharp.Parser
{
    public class LinkerOptions : CppLinkerOptions
    {
        public LinkerOptions()
        {
        }

        public static bool UseCompilerDriverAsLinker = true;

        public LinkerOptions(LinkerOptions other)
        {
            for (uint i = 0; i < other.ArgumentsCount; i++)
            {
                AddArguments(other.GetArguments(i));
            }
            for (uint i = 0; i < other.LibraryDirsCount; i++)
            {
                AddLibraryDirs(other.GetLibraryDirs(i));
            }
            for (uint i = 0; i < other.LibrariesCount; i++)
            {
                AddLibraries(other.GetLibraries(i));
            }
        }

        public string SystemLibraryPath { get; set; }
        public System.Version MacOSSDKVersion { get; set; } = new System.Version("10.12.0");

        public void Setup(string triple, LanguageVersion? languageVersion)
        {
            string[] parts = triple.Split('-');
            switch (Platform.Host)
            {
                case TargetPlatform.Windows:
                    AddArguments("-dll");
                    AddArguments("libcmt.lib");
                    if (parts.Any(p => p.StartsWith("mingw") || p.StartsWith("gnu")))
                    {
                        AddArguments("libstdc++-6.dll");
                    }
                    break;
                case TargetPlatform.Linux:
                case TargetPlatform.Android:
                    AddArguments("-L" + (SystemLibraryPath ?? "/usr/lib/x86_64-linux-gnu"));
                    AddArguments("-lc");
                    AddArguments("--shared");
                    AddArguments(UseCompilerDriverAsLinker ? "-Wl,-rpath" : "-rpath");
                    AddArguments(".");
                    break;
                case TargetPlatform.MacOS:
                case TargetPlatform.iOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.TVOS:
                    if (languageVersion > LanguageVersion.C99_GNU)
                    {
                        AddArguments("-lc++");
                    }
                    AddArguments("-lSystem");
                    AddArguments("-dylib");
                    AddArguments("-arch");
                    AddArguments(parts.Length > 0 ? parts[0] : "x86_64");
                    AddArguments("-platform_version");
                    AddArguments("macos");
                    AddArguments(MacOSSDKVersion.ToString());
                    AddArguments(MacOSSDKVersion.ToString());
                    AddArguments("-L" + (SystemLibraryPath ?? "/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/usr/lib"));
                    AddArguments("-rpath");
                    AddArguments(".");
                    break;
            }
        }

        public void SetupLibraryArguments()
        {
            for (uint i = 0; i < LibraryDirsCount; i++)
            {
                var dir = GetLibraryDirs(i);
                AddArguments("-L" + dir);
            }

            for (uint i = 0; i < LibrariesCount; i++)
            {
                var lib = GetLibraries(i);
                AddArguments("-l" + lib);
            }
        }

        public string GetLinkerInvocation()
        {
            var args = new List<string>();
            for (uint i = 0; i < ArgumentsCount; i++)
            {
                var arg = GetArguments(i);
                args.Add(arg);
            }

            return string.Join(" ", args);
        }

        public static string GetSharedObjectName(string path, TargetPlatform targetPlatform)
        {
            var prefix = GetPlatformSharedObjectPrefix(targetPlatform);
            var extension = GetPlatformSharedObjectExtension(targetPlatform);
            var name = $"{prefix}{Path.GetFileNameWithoutExtension(path)}.{extension}";
            return Path.Join(Path.GetDirectoryName(path), name);
        }

        public static string GetLinkerExecutableName(TargetPlatform targetPlatform)
        {
            // If LLD exists on the PATH, then prefer it. If not, use the host linker.
            var lldLinkerExe = GetLLDLinkerExecutableName(targetPlatform);
            return (ExistsOnPath(lldLinkerExe) && !UseCompilerDriverAsLinker) ?
                lldLinkerExe : GetPlatformLinkerExecutableName(targetPlatform);
        }

        public static string GetPlatformSharedObjectPrefix(TargetPlatform targetPlatform)
        {
            switch (targetPlatform)
            {
                case TargetPlatform.Windows:
                    return "";
                case TargetPlatform.Linux:
                case TargetPlatform.Android:
                case TargetPlatform.MacOS:
                case TargetPlatform.iOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.TVOS:
                case TargetPlatform.Emscripten:
                    return "lib";
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetPlatform), targetPlatform, null);
            }
        }

        public static string GetPlatformSharedObjectExtension(TargetPlatform targetPlatform)
        {
            switch (targetPlatform)
            {
                case TargetPlatform.Windows:
                    return "dll";
                case TargetPlatform.Linux:
                case TargetPlatform.Android:
                case TargetPlatform.Emscripten:
                    return "so";
                case TargetPlatform.MacOS:
                case TargetPlatform.iOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.TVOS:
                    return "dylib";
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetPlatform), targetPlatform, null);
            }
        }

        public static string GetPlatformLinkerExecutableName(TargetPlatform targetPlatform)
        {
            switch (targetPlatform)
            {
                case TargetPlatform.Windows:
                    return "link.exe";
                case TargetPlatform.Linux:
                    return UseCompilerDriverAsLinker ? "gcc" : "ld";
                case TargetPlatform.Android:
                case TargetPlatform.MacOS:
                case TargetPlatform.iOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.TVOS:
                    return "ld";
                case TargetPlatform.Emscripten:
                    return GetLLDLinkerExecutableName(targetPlatform);
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetPlatform), targetPlatform, null);
            }
        }

        public static string GetLLDLinkerExecutableName(TargetPlatform targetPlatform)
        {
            switch (targetPlatform)
            {
                case TargetPlatform.Windows:
                    return "lld-link";
                case TargetPlatform.Linux:
                case TargetPlatform.Android:
                    return "ld.lld";
                case TargetPlatform.MacOS:
                case TargetPlatform.iOS:
                case TargetPlatform.WatchOS:
                case TargetPlatform.TVOS:
                    return "ld64.lld";
                case TargetPlatform.Emscripten:
                    return "wasm-ld";
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetPlatform), targetPlatform, null);
            }
        }

        private static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        private static string GetFullPath(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var environmentVariablePath = Environment.GetEnvironmentVariable("PATH");
            if (environmentVariablePath == null)
                throw new NullReferenceException(nameof(environmentVariablePath));

            foreach (var path in environmentVariablePath.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
