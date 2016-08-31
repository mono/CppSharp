using CppSharp.Parser.AST;

namespace CppSharp.Parser
{
    public class ParserOptions2 : ParserOptions
    {
        public ParserOptions2()
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
            Abi = CppSharp.Parser.AST.CppAbi.Microsoft;
            ToolSetToUse = MSVCToolchain.GetCLVersion(vsVersion) * 10000000;

            addArguments("-fms-extensions");
            addArguments("-fms-compatibility");
            addArguments("-fdelayed-template-parsing");

            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);
            foreach (var include in includes)
                addSystemIncludeDirs(include);
        }

        public void SetupXcode()
        {
            var builtinsPath = XcodeToolchain.GetXcodeBuiltinIncludesFolder();
            addSystemIncludeDirs(builtinsPath);

            var cppIncPath = XcodeToolchain.GetXcodeCppIncludesFolder();
            addSystemIncludeDirs(cppIncPath);

            var includePath = XcodeToolchain.GetXcodeIncludesFolder();
            addSystemIncludeDirs(includePath);

            NoBuiltinIncludes = true;
            NoStandardIncludes = true;

            addArguments("-stdlib=libc++");
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
