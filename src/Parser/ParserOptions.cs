using CppSharp.Parser.AST;
using System.Reflection;
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
        CPP14_GNU
    };

    public class ParserOptions : CppParserOptions
    {
        public ParserOptions()
        {
            Abi = Platform.IsUnixPlatform ? CppAbi.Itanium : CppAbi.Microsoft;
            MicrosoftMode = !Platform.IsUnixPlatform;
            CurrentDir = Assembly.GetExecutingAssembly().Location;
        }

        public bool IsItaniumLikeAbi => Abi != CppAbi.Microsoft;
        public bool IsMicrosoftAbi => Abi == CppAbi.Microsoft;

        public bool EnableRTTI { get; set; }
        public LanguageVersion? LanguageVersion { get; set; }

        public void SetupMSVC()
        {
            VisualStudioVersion vsVersion = VisualStudioVersion.Latest;

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
        public void SetupMSVC(VisualStudioVersion vsVersion)
        {
            MicrosoftMode = true;
            NoBuiltinIncludes = true;
            NoStandardIncludes = true;
            Abi = CppAbi.Microsoft;

            VisualStudioVersion foundVsVersion;
            var includes = MSVCToolchain.GetSystemIncludes(vsVersion, out foundVsVersion);
            foreach (var include in includes)
                AddSystemIncludeDirs(include);

            if (!LanguageVersion.HasValue)
                LanguageVersion = CppSharp.Parser.LanguageVersion.CPP14_GNU;

            var clVersion = MSVCToolchain.GetCLVersion(foundVsVersion);
            ToolSetToUse = clVersion.Major * 10000000 + clVersion.Minor * 100000;

            AddArguments("-fms-extensions");
            AddArguments("-fms-compatibility");
            AddArguments("-fdelayed-template-parsing");
        }

        public void SetupXcode()
        {
            var builtinsPath = XcodeToolchain.GetXcodeBuiltinIncludesFolder();
            AddSystemIncludeDirs(builtinsPath);

            var cppIncPath = XcodeToolchain.GetXcodeCppIncludesFolder();
            AddSystemIncludeDirs(cppIncPath);

            var includePath = XcodeToolchain.GetXcodeIncludesFolder();
            AddSystemIncludeDirs(includePath);

            NoBuiltinIncludes = true;
            NoStandardIncludes = true;

            AddArguments("-stdlib=libc++");
        }

        public void Setup()
        {
            SetupArguments();

            if (!NoBuiltinIncludes)
                SetupIncludes();
        }

        private void SetupArguments()
        {
            if (!LanguageVersion.HasValue)
                LanguageVersion = CppSharp.Parser.LanguageVersion.CPP14_GNU;

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
            }

            if (!EnableRTTI)
                AddArguments("-fno-rtti");
        }

        private void SetupIncludes()
        {
            switch(Platform.Host)
            {
                case TargetPlatform.Windows:
                    SetupMSVC();
                    break;
                case TargetPlatform.MacOS:
                    SetupXcode();
                    break;
            }
        }
    }
}
