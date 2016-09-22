using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CppSharp
{
    public static class MonoToolchain
    {
        public static string FindMonoPath()
        {
            if (Platform.IsWindows)
                return @"C:\\Program Files (x86)\\Mono";
            else if (Platform.IsMacOS)
                return "/Library/Frameworks/Mono.framework/Versions/Current";

            throw new NotImplementedException();
        }

        public static string FindCSharpCompilerPath()
        {
            if (Platform.IsWindows)
            {
                List<ToolchainVersion> versions;
                if (!MSVCToolchain.GetMSBuildSdks(out versions))
                    throw new Exception("Could not find MSBuild SDK paths");

                var sdk = versions.Last();

                return Path.Combine(sdk.Directory, "csc.exe");
            }

            return Path.Combine(FindMonoPath(), "bin", "mcs.exe");
        }
    }
}
