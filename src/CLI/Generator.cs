using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;
using CppSharp.Passes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppAbi = CppSharp.Parser.AST.CppAbi;

namespace CppSharp
{
    class Generator : ILibrary
    {
        private Options options = null;
        private string triple = "";
        private CppAbi abi = CppAbi.Microsoft;

        public Generator(Options options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.options = options;
        }

        public bool ValidateOptions(List<string> messages)
        {
            if (Platform.IsWindows && options.Platform != TargetPlatform.Windows)
            {
                messages.Add("Cannot create bindings for a platform other that Windows from a Windows running machine");
                return false;
            }
            else if (Platform.IsMacOS && options.Platform != TargetPlatform.MacOS)
            {
                messages.Add("Cannot create bindings for a platform other that MacOS from a MacOS running machine");
                return false;
            }
            else if (Platform.IsLinux && options.Platform != TargetPlatform.Linux)
            {
                messages.Add("Cannot create bindings for a platform other that Linux from a Linux running machine");
                return false;
            }

            if (options.Platform != TargetPlatform.Windows && options.Kind != GeneratorKind.CSharp)
            {
                messages.Add("Cannot create bindings for languages other than C# from a non Windows machine");
                return false;
            }

            if (options.Platform == TargetPlatform.Linux && options.Architecture != TargetArchitecture.x64)
            {
                messages.Add("Cannot create bindings for architectures other than x64 for Linux machines");
                return false;
            }

            if (options.HeaderFiles.Count == 0)
            {
                messages.Add("No source header file has been given to generate bindings from");
                return false;
            }

            if (string.IsNullOrEmpty(options.OutputNamespace))
            {
                messages.Add("Output namespace is empty");
                return false;
            }

            if (string.IsNullOrEmpty(options.OutputFileName))
            {
                messages.Add("Output not specified");
                return false;
            }

            if (string.IsNullOrEmpty(options.InputLibraryName) && !options.CheckSymbols)
            {
                messages.Add("Input library name not specified and check symbols not enabled. Either set the input library name or the check symbols flag");
                return false;
            }

            if (string.IsNullOrEmpty(options.InputLibraryName) && options.CheckSymbols && options.Libraries.Count == 0)
            {
                messages.Add("Input library name not specified and check symbols is enabled but no libraries were given. Either set the input library name or add at least one library");
                return false;
            }
            
            StringBuilder tripleBuilder = new StringBuilder();
            if (options.Architecture == TargetArchitecture.x64)
                tripleBuilder.Append("x86_64-");
            else if(options.Architecture == TargetArchitecture.x86)
                tripleBuilder.Append("i686-");

            if (options.Platform == TargetPlatform.Windows)
            {
                tripleBuilder.Append("pc-win32-msvc");
                abi = CppAbi.Microsoft;
            }
            else if (options.Platform == TargetPlatform.MacOS)
            {
                tripleBuilder.Append("apple-darwin12.4.0");
                abi = CppAbi.Itanium;
            }
            else if (options.Platform == TargetPlatform.Linux)
            {
                tripleBuilder.Append("linux-gnu");
                abi = CppAbi.Itanium;

                if(options.Cpp11ABI)
                    tripleBuilder.Append("-cxx11abi");
            }

            triple = tripleBuilder.ToString();

            return true;
        }

        public void Setup(Driver driver)
        {
            var parserOptions = driver.ParserOptions;
            parserOptions.TargetTriple = triple;
            parserOptions.Abi = abi;

            var driverOptions = driver.Options;
            driverOptions.GeneratorKind = options.Kind;
            var module = driverOptions.AddModule(options.OutputFileName);

            if(!string.IsNullOrEmpty(options.InputLibraryName))
                module.SharedLibraryName = options.InputLibraryName;

            module.Headers.AddRange(options.HeaderFiles);
            module.Libraries.AddRange(options.Libraries);
            module.OutputNamespace = options.OutputNamespace;

            if (abi == CppAbi.Microsoft)
                parserOptions.MicrosoftMode = true;

            if (triple.Contains("apple"))
                SetupMacOptions(parserOptions);

            if (triple.Contains("linux"))
                SetupLinuxOptions(parserOptions);
            
            foreach (string s in options.IncludeDirs)
                parserOptions.AddIncludeDirs(s);

            foreach (string s in options.LibraryDirs)
                parserOptions.AddLibraryDirs(s);

            foreach (KeyValuePair<string, string> d in options.Defines)
            {
                if(string.IsNullOrEmpty(d.Value))
                    parserOptions.AddDefines(d.Key);
                else
                    parserOptions.AddDefines(d.Key + "=" + d.Value);
            }

            driverOptions.OutputDir = options.OutputDir;
            driverOptions.CheckSymbols = options.CheckSymbols;
            driverOptions.UnityBuild = options.UnityBuild;
        }

        private void SetupLinuxOptions(ParserOptions parserOptions)
        {
            parserOptions.MicrosoftMode = false;
            parserOptions.NoBuiltinIncludes = true;

            var headersPath = string.Empty;

            // Search for the available GCC versions on the provided headers.
            var versions = Directory.EnumerateDirectories(Path.Combine(headersPath, "usr/include/c++"));

            if (versions.Count() == 0)
                throw new Exception("No valid GCC version found on system include paths");

            string gccVersionPath = versions.First();
            string gccVersion = gccVersionPath.Substring(gccVersionPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            string[] systemIncludeDirs = {
                Path.Combine("usr", "include", "c++", gccVersion),
                Path.Combine("usr", "include", "x86_64-linux-gnu", "c++", gccVersion),
                Path.Combine("usr", "include", "c++", gccVersion, "backward"),
                Path.Combine("usr", "lib", "gcc", "x86_64-linux-gnu", gccVersion, "include"),
                Path.Combine("usr", "include", "x86_64-linux-gnu"),
                Path.Combine("usr", "include")
            };

            foreach (var dir in systemIncludeDirs)
                parserOptions.AddSystemIncludeDirs(Path.Combine(headersPath, dir));

            parserOptions.AddDefines("_GLIBCXX_USE_CXX11_ABI=" + (options.Cpp11ABI ? "1" : "0"));
        }

        private static void SetupMacOptions(ParserOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            if (Platform.IsMacOS)
            {
                var headersPaths = new List<string> {
                    // Path.Combine(GetSourceDirectory("deps"), "llvm/tools/clang/lib/Headers"),
                    // Path.Combine(GetSourceDirectory("deps"), "libcxx", "include"),
                    "/usr/include"
                };

                foreach (var header in headersPaths)
                    Console.WriteLine(header);

                foreach (var header in headersPaths)
                    options.AddSystemIncludeDirs(header);
            }

            // var headersPath = Path.Combine(GetSourceDirectory("build"), "headers", "osx");

            // options.AddSystemIncludeDirs(Path.Combine(headersPath, "include"));
            // options.AddSystemIncludeDirs(Path.Combine(headersPath, "clang", "4.2", "include"));
            // options.AddSystemIncludeDirs(Path.Combine(headersPath, "libcxx", "include"));
            options.AddArguments("-stdlib=libc++");
        }

        public void SetupPasses(Driver driver)
        {
            driver.Context.TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any);
            driver.Context.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
            driver.Context.TranslationUnitPasses.AddPass(new MarshalPrimitivePointersAsRefTypePass());
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate |
                RenameTargets.Field | RenameTargets.Variable,
                RenameCasePattern.UpperCamelCase).VisitASTContext(driver.Context.ASTContext);
        }

        public void Run()
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append("Generating the ");

            switch(options.Kind)
            {
                case GeneratorKind.CLI:
                    messageBuilder.Append("C++/CLI");
                    break;
                case GeneratorKind.CSharp:
                    messageBuilder.Append("C#");
                    break;
            }

            messageBuilder.Append(" parser bindings for ");
            
            switch (options.Platform)
            {
                case TargetPlatform.Linux:
                    messageBuilder.Append("Linux");
                    break;
                case TargetPlatform.MacOS:
                    messageBuilder.Append("OSX");
                    break;
                case TargetPlatform.Windows:
                    messageBuilder.Append("Windows");
                    break;
            }

            messageBuilder.Append(" ");

            switch (options.Architecture)
            {
                case TargetArchitecture.x86:
                    messageBuilder.Append("x86");
                    break;
                case TargetArchitecture.x64:
                    messageBuilder.Append("x64");
                    break;
            }

            if(options.Cpp11ABI)
                messageBuilder.Append(" (GCC C++11 ABI)");

            messageBuilder.Append("...");

            Console.WriteLine(messageBuilder.ToString());

            ConsoleDriver.Run(this);

            Console.WriteLine();
        }
    }
}
