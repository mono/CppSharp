using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;

namespace CppSharp
{
    /// <summary>
    /// Generates C# and C++/CLI bindings for the CppSharp.CppParser project.
    /// </summary>
    class ParserGen : ILibrary
    {
        internal readonly GeneratorKind Kind;
        internal readonly string Triple;
        internal readonly bool IsGnuCpp11Abi;

        public static string IncludesDir => GetSourceDirectory("include");

        public ParserGen(GeneratorKind kind, string triple,
            bool isGnuCpp11Abi = false)
        {
            Kind = kind;
            Triple = triple;
            IsGnuCpp11Abi = isGnuCpp11Abi;
        }

        static string GetSourceDirectory(string dir)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, dir);

                if (Directory.Exists(path) &&
                    Directory.Exists(Path.Combine(directory.FullName, "deps")))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Could not find build directory: " + dir);
        }

        public void Setup(Driver driver)
        {
            var parserOptions = driver.ParserOptions;
            parserOptions.TargetTriple = Triple;

            var options = driver.Options;
            options.GeneratorKind = Kind;
            options.CommentKind = CommentKind.BCPLSlash;
            var parserModule = options.AddModule("CppSharp.CppParser");
            parserModule.Headers.AddRange(new[]
            {
                "AST.h",
                "Sources.h",
                "CppParser.h"
            });
            parserModule.OutputNamespace = string.Empty;

            if (parserOptions.IsMicrosoftAbi)
                parserOptions.MicrosoftMode = true;

            if (Triple.Contains("apple"))
                SetupMacOptions(parserOptions);

            if (Triple.Contains("linux"))
                SetupLinuxOptions(parserOptions);

            var basePath = Path.Combine(GetSourceDirectory("src"), "CppParser");
            parserOptions.AddIncludeDirs(basePath);
            parserOptions.AddLibraryDirs(".");

            options.OutputDir = Path.Combine(GetSourceDirectory("src"), "CppParser",
                "Bindings", Kind.ToString());

            var extraTriple = IsGnuCpp11Abi ? "-cxx11abi" : string.Empty;

            if (Kind == GeneratorKind.CSharp)
                options.OutputDir = Path.Combine(options.OutputDir, parserOptions.TargetTriple + extraTriple);

            options.CheckSymbols = false;
            //options.Verbose = true;
            parserOptions.UnityBuild = true;
        }

        private void SetupLinuxOptions(ParserOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;
            options.NoStandardIncludes = true;

            if (Platform.IsLinux)
            {
                options.SetupLinux();
            }
            else
            {
                /*
                options.AddDefines("_ISOMAC");
                options.AddSystemIncludeDirs($"{IncludesDir}/libcxx/include");
                options.AddSystemIncludeDirs($"{IncludesDir}/libc/include/x86_64-linux-gnu");
                options.AddSystemIncludeDirs($"{IncludesDir}/libc/include/x86_64-linux-any");
                options.AddSystemIncludeDirs($"{IncludesDir}/libc/include/any-linux-any");
                options.AddSystemIncludeDirs($"{IncludesDir}/libc/include/generic-glibc");
                options.AddSystemIncludeDirs($"{IncludesDir}/libc/glibc/include");
                options.AddSystemIncludeDirs($"{IncludesDir}/libc/glibc");
                options.AddSystemIncludeDirs(options.BuiltinsDir);
                //options.AddSystemIncludeDirs($"{IncludesDir}/include");

                options.AddArguments("-fgnuc-version=6.0.0");
                options.AddArguments("-stdlib=libc++");*/

                options.SetupLinux($"{IncludesDir}/linux");
            }

            options.AddDefines("_GLIBCXX_USE_CXX11_ABI=" + (IsGnuCpp11Abi ? "1" : "0"));
        }

        private static void SetupMacOptions(ParserOptions options)
        {
            if (Platform.IsMacOS)
            {
                //options.SetupXcode();
                //return;
            }

            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            var headersPath = Path.Combine(GetSourceDirectory("build"), "headers",
                "osx");

            //IncludesDir

            options.AddSystemIncludeDirs(Path.Combine(headersPath, "include", "c++", "v1"));
            options.AddSystemIncludeDirs(options.BuiltinsDir);
            options.AddSystemIncludeDirs(Path.Combine(headersPath, "include"));
            options.AddArguments("-stdlib=libc++");
            options.Verbose = true;
        }

        public void SetupPasses(Driver driver)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.RenameNamespace("CppSharp::CppParser", "Parser");

            if (driver.Options.IsCSharpGenerator)
            {
                driver.Generator.OnUnitGenerated += o =>
                {
                    Block firstBlock = o.Outputs[0].RootBlock.Blocks[1];
                    if (o.TranslationUnit.Module == driver.Options.SystemModule)
                    {
                        firstBlock.NewLine();
                        firstBlock.WriteLine("[assembly:InternalsVisibleTo(\"CppSharp.Parser.CSharp\")]");
                    }
                    else
                    {
                        firstBlock.WriteLine("using System.Runtime.CompilerServices;");
                        firstBlock.NewLine();
                        firstBlock.WriteLine("[assembly:InternalsVisibleTo(\"CppSharp.Parser\")]");
                    }
                };
            }
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            var useCrossHeaders = true;
            var osxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\osx");
            var linuxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\x86_64-linux-gnu");

            if (Platform.IsWindows)
            {
                Console.WriteLine("Generating the C++/CLI parser bindings for Windows...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI, "i686-pc-win32-msvc"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Windows...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-pc-win32-msvc"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# 64-bit parser bindings for Windows...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-pc-win32-msvc"));
                Console.WriteLine();
            }

            if (Platform.IsMacOS || useCrossHeaders)
            {
                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-apple-darwin"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-apple-darwin"));
                Console.WriteLine();
            }

            if (Platform.IsLinux)
            {
                Console.WriteLine("Generating the C# parser bindings for Linux...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Linux (GCC C++11 ABI)...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu",
                    isGnuCpp11Abi: true));
                Console.WriteLine();
            }
        }
    }
}
