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
        private readonly Options options;
        private string triple = "";
        private CppAbi abi = CppAbi.Microsoft;

        public Generator(Options options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        void SetupTargetTriple()
        {
            var tripleBuilder = new StringBuilder();

            switch (options.Architecture)
            {
                case TargetArchitecture.x64:
                    tripleBuilder.Append("x86_64-");
                    break;
                case TargetArchitecture.x86:
                    tripleBuilder.Append("i686-");
                    break;
                case TargetArchitecture.WASM32:
                    tripleBuilder.Append("wasm32-");
                    break;
                case TargetArchitecture.WASM64:
                    tripleBuilder.Append("wasm64-");
                    break;
            }

            switch (options.Platform)
            {
                case TargetPlatform.Windows:
                    tripleBuilder.Append("pc-win32-msvc");
                    abi = CppAbi.Microsoft;
                    break;
                case TargetPlatform.MacOS:
                    tripleBuilder.Append("apple-darwin12.4.0");
                    abi = CppAbi.Itanium;
                    break;
                case TargetPlatform.Linux:
                {
                    tripleBuilder.Append("linux-gnu");
                    abi = CppAbi.Itanium;

                    if (options.Cpp11ABI)
                        tripleBuilder.Append("-cxx11abi");
                    break;
                }
                case TargetPlatform.Emscripten:
                {
                    if (options.Architecture != TargetArchitecture.WASM32 &&
                        options.Architecture != TargetArchitecture.WASM64)
                        throw new Exception("Emscripten target is only compatible with WASM architectures");

                    tripleBuilder.Append("unknown-emscripten");
                    abi = CppAbi.Itanium;
                    break;
                }
            }

            triple = tripleBuilder.ToString();
        }

        public bool ValidateOptions(List<string> messages)
        {
            if (options.HeaderFiles.Count == 0)
            {
                messages.Add("No source header file has been given to generate bindings from.");
                return false;
            }

            options.Platform ??= Platform.Host;

            if (string.IsNullOrEmpty(options.OutputDir))
            {
                options.OutputDir = Path.Combine(Directory.GetCurrentDirectory(), "gen");
            }

            string moduleName;
            if (options.HeaderFiles.Count == 1)
            {
                moduleName = Path.GetFileNameWithoutExtension(options.HeaderFiles.First());
            }
            else
            {
                var dir = Path.GetDirectoryName(options.HeaderFiles.First());
                moduleName = new DirectoryInfo(dir).Name;
            }

            if (string.IsNullOrEmpty(options.OutputFileName))
                options.OutputFileName = moduleName;

            if (string.IsNullOrEmpty(options.OutputNamespace))
                options.OutputNamespace = moduleName;

            if (options.IncludeDirs.Count == 0)
                options.IncludeDirs.Add(Path.GetDirectoryName(options.HeaderFiles.First()));

            SetupTargetTriple();

            return true;
        }

        public void Setup(Driver driver)
        {
            var parserOptions = driver.ParserOptions;
            parserOptions.TargetTriple = triple;
            parserOptions.Verbose = options.Verbose;

            var driverOptions = driver.Options;
            driverOptions.GeneratorKind = options.Kind;
            var module = driverOptions.AddModule(options.OutputFileName);

            if (!string.IsNullOrEmpty(options.InputLibraryName))
                module.SharedLibraryName = options.InputLibraryName;

            module.Headers.AddRange(options.HeaderFiles);
            module.Libraries.AddRange(options.Libraries);
            module.IncludeDirs.AddRange(options.IncludeDirs);
            module.LibraryDirs.AddRange(options.LibraryDirs);
            module.OutputNamespace = options.OutputNamespace;

            if (abi == CppAbi.Microsoft)
                parserOptions.MicrosoftMode = true;

            parserOptions.UnityBuild = options.UnityBuild;
            parserOptions.EnableRTTI = options.EnableRTTI;

            parserOptions.Setup(options.Platform ?? Platform.Host);

            if (triple.Contains("linux"))
                SetupLinuxOptions(parserOptions);

            foreach (string s in options.Arguments)
                parserOptions.AddArguments(s);

            foreach (KeyValuePair<string, string> d in options.Defines)
            {
                if (string.IsNullOrEmpty(d.Value))
                    parserOptions.AddDefines(d.Key);
                else
                    parserOptions.AddDefines(d.Key + "=" + d.Value);
            }

            if (options.EnableExceptions)
                parserOptions.AddArguments("-fcxx-exceptions");

            driverOptions.GenerateDebugOutput = options.Debug;
            driverOptions.CompileCode = options.Compile;
            driverOptions.OutputDir = options.OutputDir;
            driverOptions.CheckSymbols = options.CheckSymbols;
            driverOptions.Verbose = options.Verbose;

            if (!string.IsNullOrEmpty(options.Prefix))
                driverOptions.GenerateName = name =>
                    options.Prefix + name.FileNameWithoutExtension;
        }

        private void SetupLinuxOptions(ParserOptions parserOptions)
        {
            parserOptions.SetupLinux();
            parserOptions.AddDefines("_GLIBCXX_USE_CXX11_ABI=" + (options.Cpp11ABI ? "1" : "0"));
        }

        public void SetupPasses(Driver driver)
        {
            driver.Context.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
            driver.Context.TranslationUnitPasses.AddPass(new MarshalPrimitivePointersAsRefTypePass());
        }

        public void GenerateCode(Driver driver, List<GeneratorOutput> outputs)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Run()
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.Append($"Generating {options.Kind.Name}");
            messageBuilder.Append($" bindings for {GetPlatformName(options.Platform)} {options.Architecture}");

            if (options.Cpp11ABI)
                messageBuilder.Append(" (GCC C++11 ABI)");

            messageBuilder.Append("...");
            Console.WriteLine(messageBuilder.ToString());

            ConsoleDriver.Run(this);

            Console.WriteLine();
        }

        private static string GetPlatformName(TargetPlatform? platform)
        {
            if (!platform.HasValue)
                return string.Empty;

            switch (platform.Value)
            {
                case TargetPlatform.MacOS:
                    return "macOS";
                default:
                    return platform.ToString();
            }
        }
    }
}
