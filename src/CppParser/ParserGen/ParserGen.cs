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

namespace CppSharp
{
    /// <summary>
    /// Generates C# and C++/CLI bindings for the CppSharp.CppParser project.
    /// </summary>
    class ParserGen : ILibrary
    {
        internal readonly GeneratorKind Kind;
        internal readonly string Triple;
        internal readonly CppAbi Abi;
        internal readonly bool IsGnuCpp11Abi;

        public ParserGen(GeneratorKind kind, string triple, CppAbi abi,
            bool isGnuCpp11Abi = false)
        {
            Kind = kind;
            Triple = triple;
            Abi = abi;
            IsGnuCpp11Abi = isGnuCpp11Abi;
        }

        static string GetSourceDirectory(string dir)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, dir);

                if (Directory.Exists(path) &&
                    Directory.Exists(Path.Combine(directory.FullName, "patches")))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Could not find build directory: " + dir);
        }

        public void Setup(Driver driver)
        {
            var parserOptions = driver.ParserOptions;
            parserOptions.TargetTriple = Triple;
            parserOptions.Abi = Abi;

            var options = driver.Options;
            options.LibraryName = "CppSharp.CppParser";
            options.SharedLibraryName = "CppSharp.CppParser.dll";
            options.GeneratorKind = Kind;
            options.Headers.AddRange(new[]
            {
                "AST.h",
                "Sources.h",
                "CppParser.h"
            });
            options.Libraries.Add("CppSharp.CppParser.lib");

            if (Abi == CppAbi.Microsoft)
                parserOptions.MicrosoftMode = true;

            if (Triple.Contains("apple"))
                SetupMacOptions(parserOptions);

            if (Triple.Contains("linux"))
                SetupLinuxOptions(parserOptions);

            var basePath = Path.Combine(GetSourceDirectory("src"), "CppParser");
            parserOptions.addIncludeDirs(basePath);
            parserOptions.addLibraryDirs(".");

            options.OutputDir = Path.Combine(GetSourceDirectory("src"), "CppParser",
                "Bindings", Kind.ToString());

            var extraTriple = IsGnuCpp11Abi ? "-cxx11abi" : string.Empty;

            if (Kind == GeneratorKind.CSharp)
                options.OutputDir = Path.Combine(options.OutputDir, parserOptions.TargetTriple + extraTriple);

            options.OutputNamespace = string.Empty;
            options.CheckSymbols = false;
            //options.Verbose = true;
            options.UnityBuild = true;
        }

        private void SetupLinuxOptions(ParserOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            var headersPath = Platform.IsLinux ? string.Empty :
                Path.Combine(GetSourceDirectory("build"), "headers", "x86_64-linux-gnu");

            // Search for the available GCC versions on the provided headers.
            var versions = Directory.EnumerateDirectories(Path.Combine(headersPath,
                "usr/include/c++"));

            if (versions.Count() == 0)
                throw new Exception("No valid GCC version found on system include paths");

            string gccVersionPath = versions.First();
            string gccVersion = gccVersionPath.Substring(
                gccVersionPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            string[] systemIncludeDirs = {
                Path.Combine("usr", "include", "c++", gccVersion),
                Path.Combine("usr", "include", "x86_64-linux-gnu", "c++", gccVersion),
                Path.Combine("usr", "include", "c++", gccVersion, "backward"),
                Path.Combine("usr", "lib", "gcc", "x86_64-linux-gnu", gccVersion, "include"),
                Path.Combine("usr", "include", "x86_64-linux-gnu"),
                Path.Combine("usr", "include")
            };

            foreach (var dir in systemIncludeDirs)
                options.addSystemIncludeDirs(Path.Combine(headersPath, dir));

            options.addDefines("_GLIBCXX_USE_CXX11_ABI=" + (IsGnuCpp11Abi ? "1" : "0"));
        }

        private static void SetupMacOptions(ParserOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            if (Platform.IsMacOS)
            {
                var headersPaths = new List<string> {
                    Path.Combine(GetSourceDirectory("deps"), "llvm/tools/clang/lib/Headers"),
                    Path.Combine(GetSourceDirectory("deps"), "libcxx", "include"),
                    "/usr/include",
                };

                foreach (var header in headersPaths)
                    Console.WriteLine(header);

                foreach (var header in headersPaths)
                    options.addSystemIncludeDirs(header);
            }

            var headersPath = Path.Combine(GetSourceDirectory("build"), "headers",
                "osx");

            options.addSystemIncludeDirs(Path.Combine(headersPath, "include"));
            options.addSystemIncludeDirs(Path.Combine(headersPath, "clang", "4.2", "include"));
            options.addSystemIncludeDirs(Path.Combine(headersPath, "libcxx", "include"));
            options.addArguments("-stdlib=libc++");
        }

        public void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new CheckMacroPass());
            driver.AddTranslationUnitPass(new IgnoreStdFieldsPass());
            driver.AddTranslationUnitPass(new GetterSetterToPropertyPass());
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.RenameNamespace("CppSharp::CppParser", "Parser");
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            if (Platform.IsWindows)
            {
                Console.WriteLine("Generating the C++/CLI parser bindings for Windows...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI, "i686-pc-win32-msvc",
                    CppAbi.Microsoft));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Windows...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-pc-win32-msvc",
                    CppAbi.Microsoft));
                Console.WriteLine();

                Console.WriteLine("Generating the C# 64-bit parser bindings for Windows...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-pc-win32-msvc",
                    CppAbi.Microsoft));
                Console.WriteLine();
            }

            var osxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\osx");
            if (Directory.Exists(osxHeadersPath) || Platform.IsMacOS)
            {
                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-apple-darwin12.4.0",
                    CppAbi.Itanium));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-apple-darwin12.4.0",
                    CppAbi.Itanium));
                Console.WriteLine();
            }

            var linuxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\x86_64-linux-gnu");
            if (Directory.Exists(linuxHeadersPath) || Platform.IsLinux)
            {
                Console.WriteLine("Generating the C# parser bindings for Linux...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu",
                     CppAbi.Itanium));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Linux (GCC C++11 ABI)...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu",
                     CppAbi.Itanium, isGnuCpp11Abi: true));
                Console.WriteLine();
            }
        }
    }

    public class IgnoreStdFieldsPass : TranslationUnitPass
    {
        public override bool VisitFieldDecl(Field field)
        {
            if (!field.IsGenerated)
                return false;

            if (!IsStdType(field.QualifiedType)) return false;

            field.ExplicitlyIgnore();
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.GenerationKind == GenerationKind.None)
                return false;

            if (function.Parameters.Any(param => IsStdType(param.QualifiedType)))
            {
                function.ExplicitlyIgnore();
                return false;
            }

            return true;
        }

        private bool IsStdType(QualifiedType type)
        {
            var typePrinter = new CppTypePrinter();
            var typeName = type.Visit(typePrinter);

            return typeName.Contains("std::");
        }
    }
}
