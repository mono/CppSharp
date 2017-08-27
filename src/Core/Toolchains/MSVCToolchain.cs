using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.VisualStudio.Setup.Configuration;
using System.Runtime.InteropServices;

namespace CppSharp
{
    /// <summary>Represents a Visual Studio version.</summary>
    public enum VisualStudioVersion
    {
        VS2012 = 11,
        VS2013 = 12,
        VS2015 = 14,
        VS2017 = 15,
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

        public override string ToString()
        {
            return string.Format("{0} (version: {1})", Directory, Version);
        }
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
        static void DumpSdks(string sku, IEnumerable<ToolchainVersion> sdks)
        {
            Console.WriteLine("\n{0} SDKs:", sku);
            foreach (var sdk in sdks)
                Console.WriteLine("\t({0}) {1}", sdk.Version, sdk.Directory);
        }

        /// <summary>Dumps the detected SDK versions.</summary>
        public static void DumpSdks()
        {
            List<ToolchainVersion> vsSdks;
            GetVisualStudioSdks(out vsSdks);
            DumpSdks("Visual Studio", vsSdks);

            List<ToolchainVersion> windowsSdks;
            GetWindowsSdks(out windowsSdks);
            DumpSdks("Windows", windowsSdks);

            List<ToolchainVersion> windowsKitsSdks;
            GetWindowsKitsSdks(out windowsKitsSdks);
            DumpSdks("Windows Kits", windowsKitsSdks);

            List<ToolchainVersion> netFrameworkSdks;
            GetNetFrameworkSdks(out netFrameworkSdks);
            DumpSdks(".NET Framework", netFrameworkSdks);

            List<ToolchainVersion> msbuildSdks;
            GetMSBuildSdks(out msbuildSdks);
            DumpSdks("MSBuild", msbuildSdks);
        }

        /// <summary>Dumps include directories for selected toolchain.</summary>
        /// <param name="vsVersion">The version of Visual Studio to dump the SDK-s of.</param>
        public static void DumpSdkIncludes(VisualStudioVersion vsVersion =
            VisualStudioVersion.Latest)
        {
            Console.WriteLine("\nInclude search path (VS: {0}):", vsVersion);
            VisualStudioVersion foundVsVersion;
            foreach (var include in GetSystemIncludes(vsVersion, out foundVsVersion))
                Console.WriteLine($"\t{include}");
        }

        public static Version GetCLVersion(VisualStudioVersion vsVersion)
        {
            Version clVersion;
            switch (vsVersion)
            {
                case VisualStudioVersion.VS2012:
                    clVersion = new Version { Major = 17, Minor = 0};
                    break;
                case VisualStudioVersion.VS2013:
                    clVersion = new Version { Major = 18, Minor = 0 };
                    break;
                case VisualStudioVersion.VS2015:
                    clVersion = new Version { Major = 19, Minor = 0 };
                    break;
                case VisualStudioVersion.VS2017:
                case VisualStudioVersion.Latest:
                    clVersion = new Version { Major = 19, Minor = 10 };
                    break;
                default:
                    throw new Exception("Unknown Visual Studio version");
            }

            return clVersion;
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
                    return 14;
                case VisualStudioVersion.VS2017:
                case VisualStudioVersion.Latest:
                    return 15;
                default:
                    throw new Exception("Unknown Visual Studio version");
            }
        }

        public static ToolchainVersion GetVSToolchain(VisualStudioVersion vsVersion)
        {
            List<ToolchainVersion> vsSdks;
            GetVisualStudioSdks(out vsSdks);

            if (vsSdks.Count == 0)
                throw new Exception("Could not find a valid Visual Studio toolchain");

            return (vsVersion == VisualStudioVersion.Latest)
                ? vsSdks.Last()
                : vsSdks.Find(version =>
                    (int) version.Version == GetVisualStudioVersion(vsVersion));
        }

        public static ToolchainVersion GetWindowsKitsToolchain(VisualStudioVersion vsVersion,
            out int windowsSdkMajorVer)
        {
            var vsSdk = GetVSToolchain(vsVersion);

            var vsDir = vsSdk.Directory;
            vsDir = vsDir.Substring(0, vsDir.LastIndexOf(@"\Common7\IDE",
                StringComparison.Ordinal));

            // Check VCVarsQueryRegistry.bat to see which Windows SDK version
            // is supposed to be used with this VS version.
            var vcVarsPath = Path.Combine(vsDir, @"Common7\Tools\VCVarsQueryRegistry.bat");

            windowsSdkMajorVer = 0;
            string kitsRootKey = string.Empty;

            var vcVarsFile = File.ReadAllText(vcVarsPath);
            var match = Regex.Match(vcVarsFile, @"Windows\\v([1-9][0-9]*)\.?([0-9]*)");
            if (match.Success)
                windowsSdkMajorVer = int.Parse(match.Groups[1].Value);

            match = Regex.Match(vcVarsFile, @"KitsRoot([1-9][0-9]*)");
            if (match.Success)
                kitsRootKey = match.Groups[0].Value;

            List<ToolchainVersion> windowsKitsSdks;
            GetWindowsKitsSdks(out windowsKitsSdks);

            var windowsKitSdk = (!string.IsNullOrWhiteSpace(kitsRootKey))
                ? windowsKitsSdks.Find(version => version.Value == kitsRootKey)
                : windowsKitsSdks.Last();
            // If for some reason we cannot find the SDK version reported by VS
            // in the system, then fallback to the latest version found.
            if (windowsKitSdk.Value == null)
                windowsKitSdk = windowsKitsSdks.Last();   
            return windowsKitSdk;
        }

        /// <summary>Gets the system include folders for the given Visual Studio version.</summary>
        /// <param name="wantedVsVersion">The version of Visual Studio to get
        /// system includes from.</param>
        /// <param name="foundVsVersion">The found version of Visual Studio
        /// system includes are actually got from.</param>
        public static List<string> GetSystemIncludes(VisualStudioVersion wantedVsVersion,
            out VisualStudioVersion foundVsVersion)
        {
            if (wantedVsVersion != VisualStudioVersion.Latest)
            {
                var vsSdk = GetVSToolchain(wantedVsVersion);
                if (vsSdk.IsValid)
                {
                    var vsDir = vsSdk.Directory;
                    vsDir = vsDir.Substring(0, vsDir.LastIndexOf(@"\Common7\IDE",
                        StringComparison.Ordinal));

                    foundVsVersion = wantedVsVersion;
                    return GetSystemIncludes(wantedVsVersion, vsDir);
                }
            }

            // we don't know what "latest" is on a given machine
            // so start from the latest specified version and loop until a match is found 
            for (var i = VisualStudioVersion.Latest - 1; i >= VisualStudioVersion.VS2012; i--)
            {
                var includes = GetSystemIncludes(i, out foundVsVersion);
                if (includes.Count > 0)
                    return includes;
            }

            foundVsVersion = VisualStudioVersion.Latest;
            return new List<string>();
        }

        private static List<string> GetSystemIncludes(VisualStudioVersion vsVersion, string vsDir)
        {
            if (vsVersion == VisualStudioVersion.VS2017)
                return GetSystemIncludesVS2017(vsDir);

            int windowsSdkMajorVer;
            var windowsKitSdk = GetWindowsKitsToolchain(vsVersion, out windowsSdkMajorVer);

            List<ToolchainVersion> windowsSdks;
            GetWindowsSdks(out windowsSdks);

            var includes = new List<string> { Path.Combine(vsDir, @"VC\include") };
            // Older Visual Studio versions provide their own Windows SDK.
            if (windowsSdks.Count == 0)
            {
                includes.Add(Path.Combine(vsDir, @"\VC\PlatformSDK\Include"));
            }
            else
            {
                includes.AddRange(GetIncludeDirsFromWindowsSdks(windowsSdkMajorVer, windowsSdks));
            }

            includes.AddRange(
                CollectUniversalCRuntimeIncludeDirs(vsDir, windowsKitSdk, windowsSdkMajorVer));

            return includes;
        }

        private static IEnumerable<string> GetIncludeDirsFromWindowsSdks(
            int windowsSdkMajorVer, List<ToolchainVersion> windowsSdks)
        {
            var majorWindowsSdk = windowsSdks.Find(
                version => (int) Math.Floor(version.Version) == windowsSdkMajorVer);
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
                var match = Regex.Match(vsVarsFile, @"INCLUDE=%UniversalCRTSdkDir%(.+)%INCLUDE%");
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
        /// <param name="versions">Collection holding information about available .Net-framework-sdks</param>
        /// <returns>Success of the operation</returns>
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

        /// <summary>
        /// Gets MSBuild installation directories.
        /// </summary>
        /// <param name="versions">Collection holding information about available ms-build-sdks</param>
        /// <returns>Success of the operation</returns>
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

        /// <summary>
        /// Gets Windows SDK installation directories.
        /// </summary>
        /// <param name="versions">Collection holding information about available windows-sdks</param>
        /// <returns>Success of the operation</returns>
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

        /// <summary>
        /// Gets Windows Kits SDK installation directories.
        /// </summary>
        /// <param name="versions">Collection holding information about available WindowsKitsSdks</param>
        /// <returns>Success of the operation</returns>
        public static bool GetWindowsKitsSdks(out List<ToolchainVersion> versions)
        {
            versions = new List<ToolchainVersion>();

            GetToolchainsFromSystemRegistryValues(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows Kits\\Installed Roots",
                "KitsRoot", versions, RegistryView.Registry32);

            if (versions.Count == 0 && Environment.Is64BitProcess)
            {
                GetToolchainsFromSystemRegistryValues(
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows Kits\\Installed Roots",
                    "KitsRoot", versions, RegistryView.Registry64);
            }

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));

            return true;
        }

        /// <summary>
        /// Gets Visual Studio installation directories.
        /// </summary>
        /// <param name="versions">Collection holding information about available Visual Studio instances</param>
        /// <returns>Success of the operation</returns>
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

            //Check for VS 2017
            GetVs2017Instances(versions);

            versions.Sort((v1, v2) => (int)(v1.Version - v2.Version));
            return true;
        }

        /// Read registry strings looking for matching values.
        public static bool GetToolchainsFromSystemRegistryValues(string keyPath,
            string matchValue, ICollection<ToolchainVersion> entries, RegistryView view)
        {
            string subKey;
            var hive = GetRegistryHive(keyPath, out subKey);
            using (var rootKey = RegistryKey.OpenBaseKey(hive, view))
            using (var key = rootKey.OpenSubKey(subKey, writable: false))
            {
                if (key == null)
                    return false;

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
            }

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

#region VS2017

        /// <summary>
        /// Returns all system includes of the installed VS 2017 instance.
        /// </summary>
        /// <param name="vsDir">Path to the visual studio installation (for identifying the correct instance)</param>
        /// <returns>The system includes</returns>
        private static List<string> GetSystemIncludesVS2017(string vsDir)
        {
            List<string> includes = new List<string>();
            //They includes are in a different folder
            const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
            try
            {
                var query = new SetupConfiguration();
                var query2 = (ISetupConfiguration2) query;
                var e = query2.EnumAllInstances();

                int fetched;
                var instances = new ISetupInstance[1];
                var regexWinSDK10Version = new Regex(@"Windows10SDK\.(\d+)\.");
                do
                {
                    e.Next(1, instances, out fetched);
                    if (fetched > 0)
                    {
                        var instance = (ISetupInstance2) instances[0];
                        if (instance.GetInstallationPath() != vsDir) continue;
                        var packages = instance.GetPackages();
                        var vc_tools = from package in packages
                                       where package.GetId().Contains("Microsoft.VisualStudio.Component.VC.Tools")
                                       orderby package.GetId()
                                       select package;
                        if (vc_tools.Count() > 0)
                        { // Tools found, get path
                            var path = instance.GetInstallationPath();
                            var versionFilePath = path + @"\VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt";
                            var version = System.IO.File.ReadLines(versionFilePath).ElementAt(0).Trim();
                            includes.Add(path + @"\VC\Tools\MSVC\" + version + @"\include");
                            includes.Add(path + @"\VC\Tools\MSVC\" + version + @"\atlmfc\include");
                        }
                        var sdks = from package in packages
                                   where package.GetId().Contains("Windows10SDK") || package.GetId().Contains("Windows81SDK") || package.GetId().Contains("Win10SDK_10")
                                   select package;
                        var win10sdks = from sdk in sdks
                                        where sdk.GetId().Contains("Windows10SDK")
                                        select sdk;
                        var win8sdks = from sdk in sdks
                                       where sdk.GetId().Contains("Windows81SDK")
                                       select sdk;
                        if (win10sdks.Count() > 0)
                        {
                            var sdk = win10sdks.Last();
                            var matchVersion = regexWinSDK10Version.Match(sdk.GetId());
                            string path;
                            if (matchVersion.Success)
                            {
                                Environment.SpecialFolder specialFolder = Environment.Is64BitOperatingSystem ?
                                    Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles;
                                var programFiles = Environment.GetFolderPath(specialFolder);
                                path = Path.Combine(programFiles, "Windows Kits", "10", "include",
                                    $"10.0.{matchVersion.Groups[1].Value}.0");
                            }
                            else
                            {
                                path = "<invalid>";
                            }
                            var shared = Path.Combine(path, "shared");
                            var um = Path.Combine(path, "um");
                            var winrt = Path.Combine(path, "winrt");
                            var ucrt = Path.Combine(path, "ucrt");
                            Console.WriteLine(path);
                            if (Directory.Exists(shared) &&
                                Directory.Exists(um) &&
                                Directory.Exists(winrt) &&
                                Directory.Exists(ucrt))
                            {
                                includes.Add(shared);
                                includes.Add(um);
                                includes.Add(winrt);
                                includes.Add(ucrt);
                            }
                        }
                        else if (win8sdks.Count() > 0)
                        {
                            includes.Add(@"C:\Program Files (x86)\Windows Kits\8.1\include\shared");
                            includes.Add(@"C:\Program Files (x86)\Windows Kits\8.1\include\um");
                            includes.Add(@"C:\Program Files (x86)\Windows Kits\8.1\include\winrt");
                        }

                        return includes; //We've collected all information.
                    }

                }
                while (fetched > 0);
            }
            // a COM exception means the VS 2017 COM API (therefore VS itself) is not installed, ignore
            catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error 0x{ex.HResult:x8}: {ex.Message}");
            }
            return includes;
        }

        /// <summary>
        /// Tries to get all vs 2017 instances. 
        /// </summary>
        /// <param name="versions">Collection holding available visual studio instances</param>
        /// <returns>Success of the operation</returns>
        private static bool GetVs2017Instances(ICollection<ToolchainVersion> versions)
        {
            const int REGDB_E_CLASSNOTREG = unchecked((int) 0x80040154);
            try
            {
                var query = new SetupConfiguration();
                var query2 = (ISetupConfiguration2) query;
                var e = query2.EnumAllInstances();
                int fetched;
                var instances = new ISetupInstance[1];
                do
                {
                    e.Next(1, instances, out fetched);
                    if (fetched > 0)
                    {
                        var instance = (ISetupInstance2) instances[0];
                        var toolchain = new ToolchainVersion
                        {
                            Directory = instance.GetInstallationPath() + @"\Common7\IDE",
                            Version = float.Parse(instance.GetInstallationVersion().Remove(2)),
                            Value = null // Not used currently
                        };
                        versions.Add(toolchain);
                    }
                }
                while (fetched > 0);
            }
            // a COM exception means the VS 2017 COM API (therefore VS itself) is not installed, ignore
            catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error 0x{ex.HResult:x8}: {ex.Message}");
                return false;
            }
            return true;
        }
#endregion
    }
}
