using System.Collections.Generic;
using CppSharp.Generators;

namespace CppSharp
{
    enum TargetArchitecture
    {
        x86,
        x64,
        WASM32,
        WASM64
    }

    class Options
    {
        public List<string> HeaderFiles { get; } = new List<string>();

        public List<string> IncludeDirs { get; } = new List<string>();

        public List<string> LibraryDirs { get; } = new List<string>();

        public List<string> Libraries { get; } = new List<string>();

        public List<string> Arguments { get; } = new List<string>();

        public Dictionary<string, string> Defines { get; } = new Dictionary<string, string>();

        public string OutputDir { get; set; }

        public string OutputNamespace { get; set; }

        public string OutputFileName { get; set; }

        public string InputLibraryName { get; set; }

        public string Prefix { get; set; }

        public TargetPlatform? Platform { get; set; }

        public TargetArchitecture Architecture { get; set; } = TargetArchitecture.x86;

        public GeneratorKind Kind { get; set; } = GeneratorKind.CSharp;

        public bool CheckSymbols { get; set; }

        public bool UnityBuild { get; set; }

        public bool Cpp11ABI { get; set; }

        public bool EnableExceptions { get; set; }

        public bool EnableRTTI { get; set; }

        public bool Compile { get; set; }

        public bool Debug { get; set; }

        public bool Verbose { get; set; }
    }
}