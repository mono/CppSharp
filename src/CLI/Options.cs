using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CppSharp.Generators;

namespace CppSharp
{
    enum TargetPlatform
    {
        Windows,
        MacOS,
        Linux
    }

    enum TargetArchitecture
    {
        x86,
        x64
    }

    class Options
    {
        private List<String> _headerFiles = new List<string>();
        public List<String> HeaderFiles { get { return _headerFiles; } set { _headerFiles = value; } }

        private List<String> _includeDirs = new List<string>();
        public List<String> IncludeDirs { get { return _includeDirs; } set { _includeDirs = value; } }

        private List<String> _libraryDirs = new List<string>();
        public List<String> LibraryDirs { get { return _libraryDirs; } set { _libraryDirs = value; } }

        private List<String> _libraries = new List<string>();
        public List<String> Libraries { get { return _libraries; } set { _libraries = value; } }

        private List<String> _defines = new List<string>();
        public List<String> Defines { get { return _defines; } set { _defines = value; } }

        private String _outputDir = "";
        public String OutputDir { get { return _outputDir; } set { _outputDir = value; } }

        private String _outputNamespace = "";
        public String OutputNamespace { get { return _outputNamespace; } set { _outputNamespace = value; } }

        private String _inputLibraryName = "";
        public String InputLibraryName { get { return _inputLibraryName; } set { _inputLibraryName = value; } }

        private String _inputSharedLibraryName = "";
        public String InputSharedLibraryName { get { return _inputSharedLibraryName; } set { _inputSharedLibraryName = value; } }

        private String _triple = "";
        public String Triple { get { return _triple; } set { _triple = value; } }

        private TargetPlatform _platform = TargetPlatform.Windows;
        public TargetPlatform Platform { get { return _platform; } set { _platform = value; } }

        private TargetArchitecture _architecture = TargetArchitecture.x86;
        public TargetArchitecture Architecture { get { return _architecture; } set { _architecture = value; } }

        private GeneratorKind _kind = GeneratorKind.CSharp;
        public GeneratorKind Kind { get { return _kind; } set { _kind = value; } }
        
        //private bool _verbose = false;
        //public bool Verbose { get { return _verbose; } set { _verbose = value; } }

        private bool _checkSymbols = false;
        public bool CheckSymbols { get { return _checkSymbols; } set { _checkSymbols = value; } }

        private bool _unityBuild = false;
        public bool UnityBuild { get { return _unityBuild; } set { _unityBuild = value; } }

        private bool _cpp11ABI = false;
        public bool Cpp11ABI { get { return _cpp11ABI; } set { _cpp11ABI = value; } }
    }
}