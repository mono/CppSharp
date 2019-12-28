using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LanguageVersion = CppSharp.Parser.LanguageVersion;

namespace CppSharp.Parser
{
    public enum LanguageVersion
    {
        /// <summary>
        /// C programming language (year 1999).
        /// </summary>
        C99,
        /// <summary>
        /// C programming language (year 1999, GNU variant).
        /// </summary>
        C99_GNU,
        /// <summary>
        /// C++ programming language (year 1998).
        /// </summary>
        CPP98,
        /// <summary>
        /// C++ programming language (year 1998, GNU variant).
        /// </summary>
        CPP98_GNU,
        /// <summary>
        /// C++ programming language (year 2011).
        /// </summary>
        CPP11,
        /// <summary>
        /// C++ programming language (year 2011, GNU variant).
        /// </summary>
        CPP11_GNU,
        /// <summary>
        /// C++ programming language (year 2014).
        /// </summary>
        CPP14,
        /// <summary>
        /// C++ programming language (year 2014, GNU variant).
        /// </summary>
        CPP14_GNU,
        /// <summary>
        /// C++ programming language (year 2017).
        /// </summary>
        CPP17,
        /// <summary>
        /// C++ programming language (year 2017, GNU variant).
        /// </summary>
        CPP17_GNU,
    }

    public class ParserOptions : CppParserOptions
    {
        public ParserOptions()
        {
            MicrosoftMode = !Platform.IsUnixPlatform;
        }

        public ParserOptions(ParserOptions options)
        {
            ToolSetToUse = options.ToolSetToUse;
            TargetTriple = options.TargetTriple;
            NoStandardIncludes = options.NoStandardIncludes;
            NoBuiltinIncludes = options.NoBuiltinIncludes;
            MicrosoftMode = options.MicrosoftMode;
            Verbose = options.Verbose;
            EnableRTTI = options.EnableRTTI;
            LanguageVersion = options.LanguageVersion;
            UnityBuild = options.UnityBuild;
            SkipPrivateDeclarations = options.SkipPrivateDeclarations;
            SkipFunctionBodies = options.SkipFunctionBodies;
            SkipLayoutInfo = options.SkipLayoutInfo;
            ForceClangToolchainLookup = options.ForceClangToolchainLookup;
        }

        public bool IsItaniumLikeAbi => !IsMicrosoftAbi;
        public bool IsMicrosoftAbi => TargetTriple.Contains("win32") || 
            TargetTriple.Contains("windows") || TargetTriple.Contains("msvc");

        public bool EnableRTTI { get; set; }
        public LanguageVersion? LanguageVersion { get; set; }

        /// <summary>
        /// This option forces the driver code to use Clang's toolchain code
        /// to lookup the location of system headers and library locations.
        /// At the moment, it only makes a difference for MSVC targets.
        /// If its true, then we opt to use Clang's MSVC lookup logic.
        /// </summary>
        public bool ForceClangToolchainLookup = true;

        public ParserOptions BuildForSourceFile(
            IEnumerable<CppSharp.AST.Module> modules, string file = null)
        {
            var options = new ParserOptions(this);

            // This eventually gets passed to Clang's MSCompatibilityVersion, which
            // is in turn used to derive the value of the built-in define _MSC_VER.
            // It used to receive a 4-digit based identifier but now expects a full
            // version MSVC digit, so check if we still have the old version and
            // convert to the right format.

            if (ToolSetToUse.ToString(CultureInfo.InvariantCulture).Length == 4)
                ToolSetToUse *= 100000;

            for (uint i = 0; i < ArgumentsCount; ++i)
            {
                var arg = GetArguments(i);
                options.AddArguments(arg);
            }

            for (uint i = 0; i < IncludeDirsCount; ++i)
            {
                var include = GetIncludeDirs(i);
                options.AddIncludeDirs(include);
            }

            for (uint i = 0; i < SystemIncludeDirsCount; ++i)
            {
                var include = GetSystemIncludeDirs(i);
                options.AddSystemIncludeDirs(include);
            }

            for (uint i = 0; i < DefinesCount; ++i)
            {
                var define = GetDefines(i);
                options.AddDefines(define);
            }

            for (uint i = 0; i < UndefinesCount; ++i)
            {
                var define = GetUndefines(i);
                options.AddUndefines(define);
            }

            for (uint i = 0; i < LibraryDirsCount; ++i)
            {
                var lib = GetLibraryDirs(i);
                options.AddLibraryDirs(lib);
            }

            foreach (var module in modules.Where(
                m => file == null || m.Headers.Contains(file)))
            {
                foreach (var include in module.IncludeDirs)
                    options.AddIncludeDirs(include);

                foreach (var define in module.Defines)
                    options.AddDefines(define);

                foreach (var undefine in module.Undefines)
                    options.AddUndefines(undefine);

                foreach (var libraryDir in module.LibraryDirs)
                    options.AddLibraryDirs(libraryDir);
            }

            return options;
        }

        public void SetupMSVC()
        {
            var vsVersion = VisualStudioVersion.Latest;

            // Silence "warning CS0162: Unreachable code detected"
            #pragma warning disable 162

            switch (BuildConfig.Choice)
            {
                case "vs2012":
                    vsVersion = VisualStudioVersion.VS2012;
                    break;
                case "vs2013":
                    vsVersion = VisualStudioVersion.VS2013;
                    break;
                case "vs2015":
                    vsVersion = VisualStudioVersion.VS2015;
                    break;
                case "vs2017":
                    vsVersion = VisualStudioVersion.VS2017;
                    break;

            #pragma warning restore 162

            }

            SetupMSVC(vsVersion);
        }

        /// <summary>
        /// Sets up the parser options to work with the given Visual Studio toolchain.
        /// </summary>
        /// <param name="vsVersion">The version of Visual Studio to look for.</param>
        public void SetupMSVC(VisualStudioVersion vsVersion)
        {
            MicrosoftMode = true;

            var clVersion = MSVCToolchain.GetCLVersion(vsVersion);
            ToolSetToUse = clVersion.Major * 10000000 + clVersion.Minor * 100000;

            if (!ForceClangToolchainLookup)
            {
                NoStandardIncludes = true;
                NoBuiltinIncludes = true;

                AddSystemIncludeDirs(BuiltinsDir);

                vsVersion = MSVCToolchain.FindVSVersion(vsVersion);
                foreach (var include in MSVCToolchain.GetSystemIncludes(vsVersion))
                    AddSystemIncludeDirs(include);
            }

            // do not remove the CppSharp prefix becase the Mono C# compiler breaks
            if (!LanguageVersion.HasValue)
                LanguageVersion = CppSharp.Parser.LanguageVersion.CPP14_GNU;

            AddArguments("-fms-extensions");
            AddArguments("-fms-compatibility");
            AddArguments("-fdelayed-template-parsing");
        }

        /// <summary>
        /// Set to true to opt for Xcode Clang builtin headers
        /// </summary>
        public bool UseXcodeBuiltins;

        public void SetupXcode()
        {
            var cppIncPath = XcodeToolchain.GetXcodeCppIncludesFolder();
            AddSystemIncludeDirs(cppIncPath);

            var builtinsPath = XcodeToolchain.GetXcodeBuiltinIncludesFolder();
            AddSystemIncludeDirs(UseXcodeBuiltins ? builtinsPath : BuiltinsDir);

            var includePath = XcodeToolchain.GetXcodeIncludesFolder();
            AddSystemIncludeDirs(includePath);

            NoBuiltinIncludes = true;
            NoStandardIncludes = true;

            AddArguments("-fgnuc-version=4.2.1");
            AddArguments("-stdlib=libc++");
        }

        private void GetUnixCompilerInfo(string headersPath, out string compiler,
            out string longVersion, out string shortVersion)
        {
            if (!Platform.IsLinux)
            {
                compiler = "gcc";

                // Search for the available GCC versions on the provided headers.
                var versions = Directory.EnumerateDirectories(Path.Combine(headersPath,
                    "usr", "include", "c++"));

                if (versions.Count() == 0)
                    throw new Exception("No valid GCC version found on system include paths");

                string gccVersionPath = versions.First();
                longVersion = shortVersion = gccVersionPath.Substring(
                    gccVersionPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                return;
            }

            var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("CXX") ?? "gcc", "-v");
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;
            info.UseShellExecute = false;

            var process = Process.Start(info);
            if (process == null)
                throw new SystemException("GCC compiler was not found.");

            process.WaitForExit();

            var output = process.StandardError.ReadToEnd();
            var match = Regex.Match(output, "(gcc|clang) version (([0-9]+\\.[0-9]+)\\.[0-9]+)");
            if (!match.Success)
                throw new SystemException("GCC compiler was not found.");

            compiler = match.Groups[1].ToString();
            longVersion = match.Groups[2].ToString();
            shortVersion = match.Groups[3].ToString();
        }

        public void SetupLinux(string headersPath = "")
        {
            MicrosoftMode = false;
            NoBuiltinIncludes = true;
            NoStandardIncludes = true;

            string compiler, longVersion, shortVersion;
            GetUnixCompilerInfo(headersPath, out compiler, out longVersion, out shortVersion);

            AddSystemIncludeDirs(BuiltinsDir);
            AddArguments($"-fgnuc-version={longVersion}");

            string[] versions = { longVersion, shortVersion };
            string[] triples = { "x86_64-linux-gnu", "x86_64-pc-linux-gnu" };
            if (compiler == "gcc")
            {
                foreach (var version in versions)
                {
                    AddSystemIncludeDirs($"{headersPath}/usr/include/c++/{version}");
                    AddSystemIncludeDirs($"{headersPath}/usr/include/c++/{version}/backward");

                    foreach (var triple in triples)
                    {
                        AddSystemIncludeDirs($"{headersPath}/usr/include/{triple}/c++/{version}");
                        AddSystemIncludeDirs($"{headersPath}/usr/include/c++/{version}/{triple}");
                    }
                }
            }

            foreach (var triple in triples)
            {
                foreach (var version in versions)
                {
                    AddSystemIncludeDirs($"{headersPath}/usr/lib/{compiler}/{triple}/{version}/include");
                    AddSystemIncludeDirs($"{headersPath}/usr/lib/{compiler}/{triple}/{version}/include/c++");
                    AddSystemIncludeDirs($"{headersPath}/usr/lib/{compiler}/{triple}/{version}/include/c++/{triple}");
                }

                AddSystemIncludeDirs($"{headersPath}/usr/include/{triple}");
            }

            AddSystemIncludeDirs($"{headersPath}/usr/include");
            AddSystemIncludeDirs($"{headersPath}/usr/include/linux");
        }

        public void Setup()
        {
            SetupArguments();

            if (!NoBuiltinIncludes)
                SetupIncludes();
        }

        private void SetupArguments()
        {
            // do not remove the CppSharp prefix becase the Mono C# compiler breaks
            if (!LanguageVersion.HasValue)
                LanguageVersion = CppSharp.Parser.LanguageVersion.CPP14_GNU;

            // As of Clang revision 5e866e411caa we are required to pass "-fgnuc-version="
            // to get the __GNUC__ symbol defined. macOS and Linux system headers require
            // this define, so we need explicitly pass it to Clang.

            // Note that this setup is more accurately done in the platform-specific
            // setup methods, below is generic fallback in case that logic was disabled.
            if (NoBuiltinIncludes)
            {
                switch (Platform.Host)
                {
                case TargetPlatform.MacOS:
                case TargetPlatform.Linux:
                    AddArguments("-fgnuc-version=4.2.1");
                    break;
                }
            }

            switch (LanguageVersion)
            {
                case CppSharp.Parser.LanguageVersion.C99:
                case CppSharp.Parser.LanguageVersion.C99_GNU:
                    AddArguments("-xc");
                    break;
                default:
                    AddArguments("-xc++");
                    break;
            }

            switch (LanguageVersion)
            {
                case CppSharp.Parser.LanguageVersion.C99:
                    AddArguments("-std=c99");
                    break;
                case CppSharp.Parser.LanguageVersion.C99_GNU:
                    AddArguments("-std=gnu99");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP98:
                    AddArguments("-std=c++98");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP98_GNU:
                    AddArguments("-std=gnu++98");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP11:
                    AddArguments("-std=c++11");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP11_GNU:
                    AddArguments("-std=gnu++11");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP14:
                    AddArguments("-std=c++14");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP14_GNU:
                    AddArguments("-std=gnu++14");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP17:
                    AddArguments("-std=c++1z");
                    break;
                case CppSharp.Parser.LanguageVersion.CPP17_GNU:
                    AddArguments("-std=gnu++1z");
                    break;
            }

            if (!EnableRTTI)
                AddArguments("-fno-rtti");
        }

        public string BuiltinsDir 
        {
            get
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var builtinsDir = Path.Combine(assemblyDir, "lib", "clang", ClangVersion, "include");
                return builtinsDir;
            }
        }

        private void SetupIncludes()
        {
            // Check that the builtin includes folder exists.
            if (!Directory.Exists(BuiltinsDir))
                throw new Exception($"Clang resource folder 'lib/clang/{ClangVersion}/include' was not found.");

            switch (Platform.Host)
            {
                case TargetPlatform.Windows:
                    SetupMSVC();
                    break;
                case TargetPlatform.MacOS:
                    SetupXcode();
                    break;
                case TargetPlatform.Linux:
                    SetupLinux();
                    break;
            }
        }
    }
}
