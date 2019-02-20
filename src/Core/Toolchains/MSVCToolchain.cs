using System;
using System.Collections.Generic;
using System.Globalization;
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
