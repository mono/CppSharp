using System.Linq;

namespace CppSharp.Parser
{
    public class LinkerOptions : CppLinkerOptions
    {
        public LinkerOptions()
        {
        }

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
                    AddArguments("-rpath");
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
    }
}
