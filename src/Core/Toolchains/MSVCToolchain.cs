using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.Parser;
using Microsoft.Win32;

namespace CppSharp
{
    /// Represents a Visual Studio version.
    public enum VisualStudioVersion
    {
        VS2012,
        VS2013,
        VS2015,
        Latest
    }

    /// Represents a toolchain with associated version and directory.
    public struct ToolchainVersion
    {
        public float Version;
        public string Directory;

        public override string ToString()
        {
            return string.Format("{0} (version: {1})", Directory, Version);
        }
    }

    public static class MSVCToolchain
    {
        static void DumpSdks(string sku, IEnumerable<ToolchainVersion> sdks)
        {
            Console.WriteLine("\n{0} SDKs:", sku);
            foreach (var sdk in sdks)
                Console.WriteLine("\t({0}) {1}", sdk.Version, sdk.Directory);
        }

        /// Dumps the detected SDK versions.
        public static void DumpSdks()
        {
            List<ToolchainVersion> vsSdks;
            GetVisualStudioSdks(out vsSdks);
            DumpSdks("Visual Studio", vsSdks);

            List<ToolchainVersion> windowsSdks;
            GetWindowsSdks(out windowsSdks);
            DumpSdks("Windows", windowsSdks);

            List<ToolchainVersion> netFrameworkSdks;
            GetNetFrameworkSdks(out netFrameworkSdks);
            DumpSdks(".NET Framework", netFrameworkSdks);

            List<ToolchainVersion> msbuildSdks;
            GetMSBuildSdks(out msbuildSdks);
            DumpSdks("MSBuild", msbuildSdks);
        }

        /// Dumps include directories for selected toolchain.
        public static void DumpSdkIncludes(VisualStudioVersion vsVersion =
            VisualStudioVersion.Latest)
        {
            Console.WriteLine("\nInclude search path (VS: {0}):", vsVersion);
            var includes = GetSystemIncludes(vsVersion);
            foreach (var include in includes)
                Console.WriteLine("\t{0}", include);
        }

        static int GetVisualStudioVersion(VisualStudioVersion version)
        {
            switch (version)
            {
                case VisualStudioVersion.VS2012:
                    return 11;
                case VisualStudioVersion.VS2013:
                    return 12;
                case VisualStudioVersion.VS2015:
                case VisualStudioVersion.Latest:
                    return 14;
                default:
                    throw new Exception("Unknown Visual Studio version");
            }
        }

        /// Gets the system include folders for the given Visual Studio version.
        public static List<string> GetSystemIncludes(VisualStudioVersion vsVersion)
        {
            var includes = new List<string>();

            List<ToolchainVersion> vsSdks;
            GetVisualStudioSdks(out vsSdks);

            if (vsSdks.Count == 0)
                throw new Exception("Could not find a valid Visual Studio toolchain");

            var vsSdk = (vsVersion == VisualStudioVersion.Latest)
                ? vsSdks.Last()
                : vsSdks.Find(version =>
                    (int)version.Version == GetVisualStudioVersion(vsVersion));

            var vsDir = vsSdk.Directory;
            vsDir = vsDir.Substring(0, vsDir.LastIndexOf(@"\Common7\IDE",
                StringComparison.Ordinal));

            includes.Add(Path.Combine(vsDir, @"VC\include"));

            // Check VCVarsQueryRegistry.bat to see which Windows SDK version
            // is supposed to be used with this VS version.
            var vcVarsPath = Path.Combine(vsDir, @"Common7\Tools\VCVarsQueryRegistry.bat");

            int windowsSdkMajorVer = 0, windowsSdkMinorVer = 0;
            if (File.Exists(vcVarsPath))
            {
                var vcVarsFile = File.ReadAllText(vcVarsPath);
                var match = Regex.Match(vcVarsFile, @"Windows\\v([1-9][0-9]*)\.?([0-9]*)");
                if (match.Success)
                {
                    windowsSdkMajorVer = int.Parse(match.Groups[1].Value);
                    windowsSdkMinorVer = int.Parse(match.Groups[2].Value);
                }
            }

            List<ToolchainVersion> windowSdks;
            GetWindowsSdks(out windowSdks);

            var windowSdk = (windowsSdkMajorVer != 0)
                ? windowSdks.Find(version =>
                    (int)Math.Floor(version.Version) == windowsSdkMajorVer)
                : windowSdks.Last();

            var windowSdkDir = windowSdk.Directory;

            // Older Visual Studio versions provide their own Windows SDK.
            if (windowSdks.Count == 0)
            {
                includes.Add(Path.Combine(vsDir, @"\VC\PlatformSDK\Include"));
            }
            else
            {
                if (windowsSdkMajorVer >= 8)
                {
                    includes.Add(Path.Combine(windowSdkDir, @"include\shared"));
                    includes.Add(Path.Combine(windowSdkDir, @"include\um"));
                    includes.Add(Path.Combine(windowSdkDir, @"include\winrt"));
                }
                else
                {
                    includes.Add(Path.Combine(windowSdkDir, "include"));
                }
            }

            return includes;
        }

        /// Gets .NET framework installation directories.
        public static bool GetNetFrameworkSdks(out List<ToolchainVersion> versions)
        {
            versions = new List<ToolchainVersion>();

            GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\.NETFramework",
                "InstallRoot", versions, RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
            {
                GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\.NETFramework",
                    "InstallRoot", versions, RegistryView.Registry64);
            }

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions.Count != 0;
        }

        /// Gets MSBuild installation directories.
        public static bool GetMSBuildSdks(out List<ToolchainVersion> versions)
        {
            versions = new List<ToolchainVersion>();

            GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions",
                "MSBuildToolsPath", versions, RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
            {
                GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions",
                    "MSBuildToolsPath", versions, RegistryView.Registry64);
            }

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions.Count != 0;
        }

        /// Gets Windows SDK installation directories.
        public static bool GetWindowsSdks(out List<ToolchainVersion> versions)
        {
            versions = new List<ToolchainVersion>();

            GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows",
                "InstallationFolder", versions, RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
            {
                GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows",
                    "InstallationFolder", versions, RegistryView.Registry64);
            }

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions.Count != 0;
        }

        /// Gets Visual Studio installation directories.
        public static bool GetVisualStudioSdks(out List<ToolchainVersion> versions)
        {
            versions = new List<ToolchainVersion>();

            GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio",
                "InstallDir", versions, RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
            {
                GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio",
                    "InstallDir", versions, RegistryView.Registry64);
            }

            GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VCExpress",
                "InstallDir", versions, RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
            {
                GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VCExpress",
                    "InstallDir", versions, RegistryView.Registry64);
            }

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return true;
        }

        /// Read registry string.
        /// This also supports a means to look for high-versioned keys by use
        /// of a $VERSION placeholder in the key path.
        /// $VERSION in the key path is a placeholder for the version number,
        /// causing the highest value path to be searched for and used.
        /// I.e. "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio".
        /// There can be additional characters in the component.  Only the numeric
        /// characters are compared.
        static bool GetToolchainsFromSystemRegistry(string keyPath, string valueName,
            ICollection<ToolchainVersion> entries, RegistryView view)
        {
            string subKey;
            var hive = GetRegistryHive(keyPath, out subKey);
            using (var rootKey = RegistryKey.OpenBaseKey(hive, view))
            using (var key = rootKey.OpenSubKey(subKey, writable: false))
            {
                if (key == null)
                    return false;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    ToolchainVersion entry;
                    if (HandleToolchainRegistrySubKey(out entry, key, valueName,
                        subKeyName))
                        entries.Add(entry);
                }
            }

            return true;
        }

        static bool HandleToolchainRegistrySubKey(out ToolchainVersion entry,
            RegistryKey key, string valueName, string subKeyName)
        {
            entry = new ToolchainVersion();

            // Get the number version from the key.
            var match = Regex.Match(subKeyName, @"[1-9][0-9]*\.?[0-9]*");
            if (!match.Success)
                return false;

            var versionText = match.Groups[0].Value;

            float version;
            float.TryParse(versionText, NumberStyles.Number,
                CultureInfo.InvariantCulture, out version);

            using (var versionKey = key.OpenSubKey(subKeyName))
            {
                if (versionKey == null)
                    return false;

                // Check that the key has a value passed by the caller.
                var keyValue = versionKey.GetValue(valueName);
                if (keyValue == null)
                    return false;

                entry = new ToolchainVersion
                {
                    Version = version,
                    Directory = keyValue.ToString()
                };
            }

            return true;
        }

        static RegistryHive GetRegistryHive(string keyPath, out string subKey)
        {
            var hive = (RegistryHive)0;
            subKey = null;

            if (keyPath.StartsWith("HKEY_CLASSES_ROOT\\"))
            {
                hive = RegistryHive.ClassesRoot;
                subKey = keyPath.Substring(18);
            }
            else if (keyPath.StartsWith("HKEY_USERS\\"))
            {
                hive = RegistryHive.Users;
                subKey = keyPath.Substring(11);
            }
            else if (keyPath.StartsWith("HKEY_LOCAL_MACHINE\\"))
            {
                hive = RegistryHive.LocalMachine;
                subKey = keyPath.Substring(19);
            }
            else if (keyPath.StartsWith("HKEY_CURRENT_USER\\"))
            {
                hive = RegistryHive.CurrentUser;
                subKey = keyPath.Substring(18);
            }

            return hive;
        }
    }

    public static partial class OptionsExtensions
    {
        /// Sets up the parser options to work with the given Visual Studio toolchain.
        public static void SetupMSVC(this ParserOptions options,
            VisualStudioVersion vsVersion = VisualStudioVersion.Latest)
        {
            options.MicrosoftMode = true;
            options.NoBuiltinIncludes = true;
            options.NoStandardIncludes = true;

            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);
            foreach (var include in includes)
                options.addSystemIncludeDirs(include);
        }
    }
}
