using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Types;
using CppAbi = CppSharp.Parser.AST.CppAbi;

namespace CppSharp
{
    /// <summary>
    /// Generates C# and C++/CLI bindings for the CppSharp.CppParser project.
    /// </summary>
    class ParserGen : ILibrary
    {
        const string LINUX_INCLUDE_BASE_DIR = "../../../../build/headers/x86_64-linux-gnu";

        internal readonly GeneratorKind Kind;
        internal readonly string Triple;
        internal readonly CppAbi Abi;

        public ParserGen(GeneratorKind kind, string triple, CppAbi abi)
        {
            Kind = kind;
            Triple = triple;
            Abi = abi;
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
            var options = driver.Options;
            options.TargetTriple = Triple;
            options.Abi = Abi;
            options.LibraryName = "CppSharp.CppParser.dll";
            options.GeneratorKind = Kind;
            options.Headers.AddRange(new[]
            {
                "AST.h",
                "Sources.h",
                "CppParser.h"
            });
            options.Libraries.Add("CppSharp.CppParser.lib");

            if (Abi == CppAbi.Microsoft)
                options.MicrosoftMode = true;

            if (Triple.Contains("apple"))
                SetupMacOptions(options);

            if (Triple.Contains("linux"))
                SetupLinuxOptions(options);

            var basePath = Path.Combine(GetSourceDirectory("src"), "CppParser");
            options.addIncludeDirs(basePath);
            options.addLibraryDirs(".");

            options.OutputDir = Path.Combine(GetSourceDirectory("src"), "CppParser",
                "Bindings", Kind.ToString());

            if (Kind == GeneratorKind.CSharp)
                options.OutputDir = Path.Combine(options.OutputDir, options.TargetTriple);

            options.GenerateLibraryNamespace = false;
            options.CheckSymbols = false;
        }

        private static void SetupLinuxOptions(DriverOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            string[] sysincdirs = new[] {
                "/usr/include/c++/4.8",
                "/usr/include/x86_64-linux-gnu/c++/4.8",
                "/usr/include/c++/4.8/backward",
                "/usr/lib/gcc/x86_64-linux-gnu/4.8/include",
                "/usr/include/x86_64-linux-gnu",
                "/usr/include",
            };

            foreach (var dir in sysincdirs)
            {
                options.addSystemIncludeDirs(LINUX_INCLUDE_BASE_DIR + dir);
            }
        }

        private static void SetupMacOptions(DriverOptions options)
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


            if (Directory.Exists(LINUX_INCLUDE_BASE_DIR))
            {
                Console.WriteLine("Generating the C# parser bindings for Linux...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "x86_64-linux-gnu",
                     CppAbi.Itanium));
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
            var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
            var typeName = type.Visit(typePrinter);

            return typeName.Contains("std::");
        }
    }
}
