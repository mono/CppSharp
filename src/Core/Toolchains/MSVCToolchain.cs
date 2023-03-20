using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CppSharp
{
    /// <summary>Represents a Visual Studio version.</summary>
    public enum VisualStudioVersion
    {
        VS2012 = 11,
        VS2013 = 12,
        VS2015 = 14,
        VS2017 = 15,
        VS2019 = 16,
        VS2022 = 17,
        Latest,
    }

    /// <summary>Represents a toolchain with associated version and directory.</summary>
    public struct ToolchainVersion
    {
        /// <summary>Version of the toolchain.</summary>
        public float Version;

        /// <summary>Directory location of the toolchain.</summary>
        public string Directory;

        /// <summary>Extra data value associated with the toolchain.</summary>
        public string Value;

        public bool IsValid => Version > 0 && !string.IsNullOrEmpty(Directory);

        public override string ToString() => $"{Directory} (version: {Version})";
    }

    /// <summary>
    /// Describes the major and minor version of something.
    /// Each version must be at least non negative and smaller than 100
    /// </summary>
    public struct Version
    {
        public int Major;

        public int Minor;
    }

    public static class MSVCToolchain
    {
        public static Version GetCLVersion(VisualStudioVersion vsVersion)
        {
            Version clVersion;
            switch (vsVersion)
            {
                case VisualStudioVersion.VS2012:
                    clVersion = new Version { Major = 17, Minor = 0 };
                    break;
                case VisualStudioVersion.VS2013:
                    clVersion = new Version { Major = 18, Minor = 0 };
                    break;
                case VisualStudioVersion.VS2015:
                    clVersion = new Version { Major = 19, Minor = 0 };
                    break;
                case VisualStudioVersion.VS2017:
                    clVersion = new Version { Major = 19, Minor = 10 };
                    break;
                case VisualStudioVersion.VS2019:
                    clVersion = new Version { Major = 19, Minor = 20 };
                    break;
                case VisualStudioVersion.VS2022:
                case VisualStudioVersion.Latest:
                    clVersion = new Version { Major = 19, Minor = 30 };
                    break;
                default:
                    throw new Exception("Unknown Visual Studio version");
            }

            return clVersion;
        }

        private static void DumpSdks(string sku, IEnumerable<ToolchainVersion> sdks)
        {
            Console.WriteLine("\n{0} SDKs:", sku);
            foreach (var sdk in sdks)
                Console.WriteLine("\t({0}) {1}", sdk.Version, sdk.Directory);
        }

        private static int GetVisualStudioVersion(VisualStudioVersion version)
        {
            switch (version)
            {
                case VisualStudioVersion.VS2012:
                    return 11;
                case VisualStudioVersion.VS2013:
                    return 12;
                case VisualStudioVersion.VS2015:
                    return 14;
                case VisualStudioVersion.VS2017:
                    return 15;
                case VisualStudioVersion.VS2019:
                case VisualStudioVersion.Latest:
                    return 16;
                default:
                    throw new Exception("Unknown Visual Studio version");
            }
        }

        private static IEnumerable<string> GetIncludeDirsFromWindowsSdks(
            int windowsSdkMajorVer, List<ToolchainVersion> windowsSdks)
        {
            var majorWindowsSdk = windowsSdks.Find(
                version => (int)Math.Floor(version.Version) == windowsSdkMajorVer);
            var windowsSdkDirs = majorWindowsSdk.Directory != null ?
                new[] { majorWindowsSdk.Directory } :
                windowsSdks.Select(w => w.Directory).Reverse();
            foreach (var windowsSdkDir in windowsSdkDirs)
            {
                if (windowsSdkMajorVer >= 8)
                {
                    var windowsSdkIncludeDir = Path.Combine(windowsSdkDir, "include");
                    IEnumerable<string> searchDirs = new[] { windowsSdkIncludeDir };
                    if (Directory.Exists(windowsSdkIncludeDir))
                        searchDirs = searchDirs.Union(Directory.EnumerateDirectories(windowsSdkIncludeDir));
                    foreach (var dir in searchDirs)
                    {
                        var shared = Path.Combine(dir, "shared");
                        var um = Path.Combine(dir, "um");
                        var winrt = Path.Combine(dir, "winrt");
                        if (Directory.Exists(shared) && Directory.Exists(um) &&
                            Directory.Exists(winrt))
                            return new[] { shared, um, winrt };
                    }
                }
                else
                {
                    var include = Path.Combine(windowsSdkDir, "include");
                    if (Directory.Exists(include))
                        return new[] { include };
                }
            }
            return new string[0];
        }

        private static IEnumerable<string> CollectUniversalCRuntimeIncludeDirs(
            string vsDir, ToolchainVersion windowsKitSdk, int windowsSdkMajorVer)
        {
            var includes = new List<string>();

            // Check vsvars32.bat to see location of the new Universal C runtime library.
            string ucrtPaths = string.Empty;
            var vsVarsPath = Path.Combine(vsDir, @"Common7\Tools\vsvars32.bat");
            if (File.Exists(vsVarsPath))
            {
                var vsVarsFile = File.ReadAllText(vsVarsPath);
                var match = Regex.Match(vsVarsFile, "INCLUDE=%UniversalCRTSdkDir%(.+)%INCLUDE%");
                if (match.Success)
                    ucrtPaths = match.Groups[1].Value;
            }

            if (string.IsNullOrWhiteSpace(ucrtPaths)) return includes;

            // like the rest of GetSystemIncludes, this is a hack
            // which copies the logic in the vcvarsqueryregistry.bat
            // the correct way would be to execute the script and collect the values
            // there's a reference implementation in the 'get_MSVC_UCRTVersion_from_VS_batch_script' branch
            // however, it cannot be used because it needs invoking an external process to run the .bat
            // which is killed prematurely by VS when the pre-build events of wrapper projects are run
            const string ucrtVersionEnvVar = "%UCRTVersion%";
            foreach (var splitPath in ucrtPaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var path = splitPath.TrimStart('\\');
                var index = path.IndexOf(ucrtVersionEnvVar, StringComparison.Ordinal);
                if (index >= 0)
                {
                    var include = path.Substring(0, index);
                    var parentIncludeDir = Path.Combine(windowsKitSdk.Directory, include);
                    var dirPrefix = windowsSdkMajorVer + ".";
                    var includeDir =
                        (from dir in Directory.EnumerateDirectories(parentIncludeDir).OrderByDescending(d => d)
                         where Path.GetFileName(dir).StartsWith(dirPrefix, StringComparison.Ordinal)
                         select Path.Combine(windowsKitSdk.Directory, include, dir)).FirstOrDefault();
                    if (!string.IsNullOrEmpty(includeDir))
                        includes.Add(Path.Combine(includeDir, Path.GetFileName(path)));
                }
                else
                    includes.Add(Path.Combine(windowsKitSdk.Directory, path));
            }

            return includes;
        }

        /// <summary>
        /// Gets .NET framework installation directories.
        /// </summary>
        /// <returns>Success of the operation</returns>
        public static List<ToolchainVersion> GetNetFrameworkSdks()
        {
            List<ToolchainVersion> versions = GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\.NETFramework",
                "InstallRoot", RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
                versions.AddRange(GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\.NETFramework",
                    "InstallRoot", RegistryView.Registry64));

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions;
        }

        /// <summary>
        /// Gets MSBuild installation directories.
        /// </summary>
        ///
        /// <returns>Success of the operation</returns>
        public static List<ToolchainVersion> GetMSBuildSdks()
        {
            List<ToolchainVersion> versions = GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions",
                "MSBuildToolsPath", RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
                versions.AddRange(GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\MSBuild\\ToolsVersions",
                    "MSBuildToolsPath", RegistryView.Registry64));

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions;
        }

        /// <summary>
        /// Gets Windows SDK installation directories.
        /// </summary>
        /// <returns>Success of the operation</returns>
        public static List<ToolchainVersion> GetWindowsSdks()
        {
            List<ToolchainVersion> versions = GetToolchainsFromSystemRegistry(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows",
                "InstallationFolder", RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
                versions.AddRange(GetToolchainsFromSystemRegistry(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Microsoft SDKs\\Windows",
                    "InstallationFolder", RegistryView.Registry64));

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions;
        }

        /// <summary>
        /// Gets Windows Kits SDK installation directories.
        /// </summary>
        /// <returns>Success of the operation</returns>
        public static List<ToolchainVersion> GetWindowsKitsSdks()
        {
            List<ToolchainVersion> versions = GetToolchainsFromSystemRegistryValues(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows Kits\\Installed Roots",
                "KitsRoot", RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
                versions.AddRange(GetToolchainsFromSystemRegistryValues(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows Kits\\Installed Roots",
                    "KitsRoot", RegistryView.Registry64));

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return versions;
        }

        /// <summary>
        /// Read registry strings looking for matching values.
        /// </summary>
        /// <param name="keyPath">The path to the key in the registry.</param>
        /// <param name="matchValue">The value to match in the located key, if any.</param>
        /// <param name="view">The type of registry, 32 or 64, to target.</param>
        ///
        public static List<ToolchainVersion> GetToolchainsFromSystemRegistryValues(
            string keyPath, string matchValue, RegistryView view)
        {
            string subKey;
            var hive = GetRegistryHive(keyPath, out subKey);
            using (var rootKey = RegistryKey.OpenBaseKey(hive, view))
            using (var key = rootKey.OpenSubKey(subKey, writable: false))
            {
                var entries = new List<ToolchainVersion>();
                if (key == null)
                    return entries;

                foreach (var valueName in key.GetValueNames())
                {
                    if (!valueName.Contains(matchValue))
                        continue;

                    var value = key.GetValue(valueName) as string;
                    if (value == null)
                        continue;

                    float version = 0;

                    // Get the number version from the key value.
                    var match = Regex.Match(value, @".*([1-9][0-9]*\.?[0-9]*)");
                    if (match.Success)
                    {
                        float.TryParse(match.Groups[1].Value, NumberStyles.Number,
                            CultureInfo.InvariantCulture, out version);
                    }

                    var entry = new ToolchainVersion
                    {
                        Directory = value,
                        Version = version,
                        Value = valueName
                    };

                    entries.Add(entry);
                }

                return entries;
            }
        }

        /// <summary>
        /// Read registry string.
        /// This also supports a means to look for high-versioned keys by use
        /// of a $VERSION placeholder in the key path.
        /// $VERSION in the key path is a placeholder for the version number,
        /// causing the highest value path to be searched for and used.
        /// I.e. "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio".
        /// There can be additional characters in the component.  Only the numeric
        /// characters are compared.
        /// </summary>
        /// <param name="keyPath">The path to the key in the registry.</param>
        /// <param name="valueName">The name of the value in the registry.</param>
        /// <param name="matchValue">The value to match in the located key, if any.</param>
        /// <param name="view">The type of registry, 32 or 64, to target.</param>
        private static List<ToolchainVersion> GetToolchainsFromSystemRegistry(
            string keyPath, string valueName, RegistryView view)
        {
            string subKey;
            var hive = GetRegistryHive(keyPath, out subKey);
            using (var rootKey = RegistryKey.OpenBaseKey(hive, view))
            using (var key = rootKey.OpenSubKey(subKey, writable: false))
            {
                var entries = new List<ToolchainVersion>();
                if (key == null)
                    return entries;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    ToolchainVersion entry;
                    if (HandleToolchainRegistrySubKey(out entry, key, valueName,
                        subKeyName))
                        entries.Add(entry);
                }
                return entries;
            }
        }

        private static bool HandleToolchainRegistrySubKey(out ToolchainVersion entry,
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

        private static RegistryHive GetRegistryHive(string keyPath, out string subKey)
        {
            var hive = (RegistryHive)0;
            subKey = null;

            if (keyPath.StartsWith("HKEY_CLASSES_ROOT\\", StringComparison.Ordinal))
            {
                hive = RegistryHive.ClassesRoot;
                subKey = keyPath.Substring(18);
            }
            else if (keyPath.StartsWith("HKEY_USERS\\", StringComparison.Ordinal))
            {
                hive = RegistryHive.Users;
                subKey = keyPath.Substring(11);
            }
            else if (keyPath.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.Ordinal))
            {
                hive = RegistryHive.LocalMachine;
                subKey = keyPath.Substring(19);
            }
            else if (keyPath.StartsWith("HKEY_CURRENT_USER\\", StringComparison.Ordinal))
            {
                hive = RegistryHive.CurrentUser;
                subKey = keyPath.Substring(18);
            }

            return hive;
        }
    }
}
