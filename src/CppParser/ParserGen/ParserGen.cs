using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;
using CppSharp.Passes;
using CppAbi = CppSharp.Parser.AST.CppAbi;

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

                if (Directory.Exists(path))
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
            {
                if(Triple.Contains("arm64"))
                {
                    SetupLinuxOptions(parserOptions, "arm64-linux-gnu");
                }
                else
                {
                    SetupLinuxOptions(parserOptions, "x86_64-linux-gnu");
                }
            }

            var basePath = Path.Combine(GetSourceDirectory("src"), "CppParser");
            parserModule.IncludeDirs.Add(basePath);
            parserModule.LibraryDirs.Add(".");

            options.OutputDir = Path.Combine(GetSourceDirectory("src"), "CppParser",
                "Bindings", Kind.ToString());

            var extraTriple = IsGnuCpp11Abi ? "-cxx11abi" : string.Empty;

            if (Kind == GeneratorKind.CSharp)
                options.OutputDir = Path.Combine(options.OutputDir, parserOptions.TargetTriple + extraTriple);

            options.CheckSymbols = false;
            //options.Verbose = true;
            parserOptions.UnityBuild = true;
        }

        private void SetupLinuxOptions(ParserOptions options, string headerFolderName)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            var headersPath = Platform.IsLinux ? string.Empty :
                Path.Combine(GetSourceDirectory("build"), "headers", headerFolderName);
            options.SetupLinux(headersPath);
            options.AddDefines("_GLIBCXX_USE_CXX11_ABI=" + (IsGnuCpp11Abi ? "1" : "0"));
        }

        private static void SetupMacOptions(ParserOptions options)
        {
            if (Platform.IsMacOS)
            {
                options.SetupXcode();
                return;
            }

            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            var headersPath = Path.Combine(GetSourceDirectory("build"), "headers",
                "osx");
            options.AddDefines("__DARWIN_OS_INLINE=inline");
            options.AddSystemIncludeDirs(Path.Combine(headersPath, "include", "c++", "v1"));
            options.AddSystemIncludeDirs(options.BuiltinsDir);
            options.AddSystemIncludeDirs(Path.Combine(headersPath, "include"));
            options.AddArguments("-stdlib=libc++");
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

            var osxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\osx");
            if (Directory.Exists(osxHeadersPath) || Platform.IsMacOS)
            {
                Console.WriteLine("Generating the C# parser bindings for OSX x86...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-apple-darwin12.4.0"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for OSX x64...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-apple-darwin12.4.0"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for OSX ARM64...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "arm64-apple-darwin12.4.0"));
                Console.WriteLine();
            }

            var linuxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\x86_64-linux-gnu");
            if (Directory.Exists(linuxHeadersPath) || Platform.IsLinux)
            {
                Console.WriteLine("Generating the C# parser bindings for Linux x64...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Linux x64 (GCC C++11 ABI)...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu",
                    isGnuCpp11Abi: true));
                Console.WriteLine();
            }

            var linuxArmHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\arm64-linux-gnu");
            if (Directory.Exists(linuxArmHeadersPath))
            {
                Console.WriteLine("Generating the C# parser bindings for Linux ARM64...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "arm64-linux-gnu"));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Linux ARM64 (GCC C++11 ABI)...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "arm64-linux-gnu",
                    isGnuCpp11Abi: true));
                Console.WriteLine();
            }
        }
    }
}
