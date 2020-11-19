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

        static TargetPlatform GetCurrentPlatform()
        {
            if (Platform.IsWindows)
                return TargetPlatform.Windows;

            if (Platform.IsMacOS)
                return TargetPlatform.MacOS;

            if (Platform.IsLinux)
                return TargetPlatform.Linux;

            throw new System.NotImplementedException("Unknown host platform");
        }

        void SetupTargetTriple()
        {
            var tripleBuilder = new StringBuilder();

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
        }

        public bool ValidateOptions(List<string> messages)
        {
            if (options.HeaderFiles.Count == 0)
            {
                messages.Add("No source header file has been given to generate bindings from.");
                return false;
            }

            if (!options.Platform.HasValue)
                options.Platform = GetCurrentPlatform();

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

            if(!string.IsNullOrEmpty(options.InputLibraryName))
                module.SharedLibraryName = options.InputLibraryName;

            module.Headers.AddRange(options.HeaderFiles);
            module.Libraries.AddRange(options.Libraries);
            module.OutputNamespace = options.OutputNamespace;

            if (abi == CppAbi.Microsoft)
                parserOptions.MicrosoftMode = true;

            parserOptions.UnityBuild = options.UnityBuild;
            parserOptions.EnableRTTI = options.EnableRTTI;

            parserOptions.Setup();

            if (triple.Contains("linux"))
                SetupLinuxOptions(parserOptions);

            foreach (string s in options.Arguments)
                parserOptions.AddArguments(s);

            foreach (string s in options.IncludeDirs)
                parserOptions.AddIncludeDirs(s);

            foreach (KeyValuePair<string, string> d in options.Defines)
            {
                if(string.IsNullOrEmpty(d.Value))
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

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Run()
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append($"Generating {GetGeneratorKindName(options.Kind)}");
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

        private static string GetGeneratorKindName(GeneratorKind kind)
        {
            switch (kind)
            {
                case GeneratorKind.CLI:
                    return "C++/CLI";
                case GeneratorKind.CSharp:
                    return "C#";
                default:
                    return kind.ToString();
            }
        }
    }
}
