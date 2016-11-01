using CppSharp.Parser.AST;

namespace CppSharp.Parser
{
    public class ParserOptions : CppParserOptions
    {
        public ParserOptions()
        {
            Abi = Platform.IsUnixPlatform ? CppAbi.Itanium : CppAbi.Microsoft;
            MicrosoftMode = !Platform.IsUnixPlatform;
        }

        public bool IsItaniumLikeAbi { get { return Abi != CppAbi.Microsoft; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }

        /// Sets up the parser options to work with the given Visual Studio toolchain.
        public void SetupMSVC(VisualStudioVersion vsVersion = VisualStudioVersion.Latest)
        {
            MicrosoftMode = true;
            NoBuiltinIncludes = true;
            NoStandardIncludes = true;
            Abi = CppAbi.Microsoft;
            ToolSetToUse = MSVCToolchain.GetCLVersion(vsVersion) * 10000000;

            AddArguments("-fms-extensions");
            AddArguments("-fms-compatibility");
            AddArguments("-fdelayed-template-parsing");

            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);
            foreach (var include in includes)
                AddSystemIncludeDirs(include);
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

        public void SetupIncludes()
        {
            if (Platform.IsMacOS)
                SetupXcode();
            else if (Platform.IsWindows && !NoBuiltinIncludes)
                SetupMSVC();
        }
    }
}
