using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Types;
using CppAbi = CppSharp.Parser.AST.CppAbi;
using CppSharp.Parser;
using CppSharp;

namespace CppSharp
{
    class Generator : ILibrary
    {
        private Options _options;

        private String _triple = "";
        private CppAbi _abi = CppAbi.Microsoft;

        public Generator(Options options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            _options = options;
        }

        public void ValidateOptions()
        {
            if (Platform.IsWindows && _options.Platform != TargetPlatform.Windows)
                throw new NotSupportedException("Cannot create bindings for a platform other that Windows from a Windows running machine");
            else if (Platform.IsMacOS && _options.Platform != TargetPlatform.MacOS)
                throw new NotSupportedException("Cannot create bindings for a platform other that MacOS from a MacOS running machine");
            else if (Platform.IsUnixPlatform && _options.Platform != TargetPlatform.Linux)
                throw new NotSupportedException("Cannot create bindings for a platform other that Linux from a Linux running machine");

            if(_options.Platform != TargetPlatform.Windows && _options.Kind != GeneratorKind.CSharp)
                throw new NotSupportedException("Cannot create bindings for languages other than C# from a non Windows machine");

            if(_options.Platform == TargetPlatform.Linux && _options.Architecture != TargetArchitecture.x64)
                throw new NotSupportedException("Cannot create bindings for architectures other than x64 for Linux machines");

            if(_options.HeaderFiles.Count == 0)
                throw new NotSupportedException("No source header file has been given");

            if (_options.OutputNamespace == String.Empty)
                throw new NotSupportedException("Output namespace is empty");

            if (_options.InputLibraryName == String.Empty)
                throw new NotSupportedException("Input library name is empty");

            if (_options.Architecture == TargetArchitecture.x64)
                _triple = "x86_64-";
            else if(_options.Architecture == TargetArchitecture.x86)
                _triple = "i686-";

            if (_options.Platform == TargetPlatform.Windows)
            {
                _triple += "pc-win32-msvc";
                _abi = CppAbi.Microsoft;
            }
            else if (_options.Platform == TargetPlatform.MacOS)
            {
                _triple += "apple-darwin12.4.0";
                _abi = CppAbi.Itanium;
            }
            else if (_options.Platform == TargetPlatform.Linux)
            {
                _triple += "linux-gnu";
                _abi = CppAbi.Itanium;

                if(_options.Cpp11ABI)
                    _triple += "-cxx11abi";
            }
        }

        public void Setup(Driver driver)
        {
            var parserOptions = driver.ParserOptions;
            parserOptions.TargetTriple = _triple;
            parserOptions.Abi = _abi;

            var options = driver.Options;
            options.LibraryName = _options.InputLibraryName;

            if(_options.InputSharedLibraryName != String.Empty)
                options.SharedLibraryName = _options.InputSharedLibraryName;

            options.GeneratorKind = _options.Kind;
            options.Headers.AddRange(_options.HeaderFiles);
            options.Libraries.AddRange(_options.Libraries);

            if (_abi == CppAbi.Microsoft)
                parserOptions.MicrosoftMode = true;

            if (_triple.Contains("apple"))
                SetupMacOptions(parserOptions);

            if (_triple.Contains("linux"))
                SetupLinuxOptions(parserOptions);

            Console.WriteLine("\n\nAdding " + (_options.IncludeDirs.Count) + " include dirs\n\n");

            foreach (String s in _options.IncludeDirs)
            {
                parserOptions.AddIncludeDirs(s);
                Console.WriteLine("Add include: " + s);
            }

            foreach (String s in _options.LibraryDirs)
                parserOptions.AddLibraryDirs(s);

            foreach (String s in _options.Defines)
                parserOptions.AddDefines(s);

            options.OutputDir = _options.OutputDir;
            options.OutputNamespace = _options.OutputNamespace;
            options.CheckSymbols = _options.CheckSymbols;
            options.UnityBuild = _options.UnityBuild;
        }

        private void SetupLinuxOptions(ParserOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

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
                options.AddSystemIncludeDirs(Path.Combine(headersPath, dir));

            options.AddDefines("_GLIBCXX_USE_CXX11_ABI=" + (_options.Cpp11ABI ? "1" : "0"));
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
            driver.AddTranslationUnitPass(new CheckMacroPass());

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
            ValidateOptions();

            String message = "Generating the ";

            switch(_options.Kind)
            {
                case GeneratorKind.CLI:
                    message += "C++/CLI";
                    break;
                case GeneratorKind.CSharp:
                    message += "C#";
                    break;
            }

            message += " parser bindings for ";
            
            switch (_options.Platform)
            {
                case TargetPlatform.Linux:
                    message += "Linux";
                    break;
                case TargetPlatform.MacOS:
                    message += "OSX";
                    break;
                case TargetPlatform.Windows:
                    message += "Windows";
                    break;
            }

            message += " ";

            switch (_options.Architecture)
            {
                case TargetArchitecture.x86:
                    message += "x86";
                    break;
                case TargetArchitecture.x64:
                    message += "x64";
                    break;
            }

            if(_options.Cpp11ABI)
                message += " (GCC C++11 ABI)";

            message += "...";

            Console.WriteLine(message);

            ConsoleDriver.Run(this);

            Console.WriteLine();
        }
    }
}
