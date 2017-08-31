﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CppSharp
{
    public static class ManagedToolchain
    {
        public static string FindMonoPath()
        {
            if (Platform.IsWindows)
                return @"C:\Program Files (x86)\Mono";
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
