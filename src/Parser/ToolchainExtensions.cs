using CppSharp.Parser;

namespace CppSharp
{
    public static partial class OptionsExtensions
    {
        /// Sets up the parser options to work with the given Visual Studio toolchain.
        public static void SetupMSVC(this ParserOptions options,
            VisualStudioVersion vsVersion = VisualStudioVersion.Latest)
        {
            options.MicrosoftMode = true;
            options.NoBuiltinIncludes = true;
            options.NoStandardIncludes = true;
            options.Abi = CppSharp.Parser.AST.CppAbi.Microsoft;
            options.ToolSetToUse = MSVCToolchain.GetCLVersion(vsVersion) * 10000000;

            options.addArguments("-fms-extensions");
            options.addArguments("-fms-compatibility");
            options.addArguments("-fdelayed-template-parsing");

            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);
            foreach (var include in includes)
                options.addSystemIncludeDirs(include);
        }
        
        public static void SetupXcode(this ParserOptions options)
        {
            var builtinsPath = XcodeToolchain.GetXcodeBuiltinIncludesFolder();
            options.addSystemIncludeDirs(builtinsPath);

            var cppIncPath = XcodeToolchain.GetXcodeCppIncludesFolder();
            options.addSystemIncludeDirs(cppIncPath);

            var includePath = XcodeToolchain.GetXcodeIncludesFolder();
            options.addSystemIncludeDirs(includePath);

            options.NoBuiltinIncludes = true;
            options.NoStandardIncludes = true;

            options.addArguments("-stdlib=libc++");
        }
    }
}