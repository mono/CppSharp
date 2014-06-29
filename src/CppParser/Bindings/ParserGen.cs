using System;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Types;
#if !OLD_PARSER
using CppAbi = CppSharp.Parser.AST.CppAbi;
#endif

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

        public ParserGen(GeneratorKind kind, string triple, CppAbi abi)
        {
            Kind = kind;
            Triple = triple;
            Abi = abi;
        }

        static string GetSourceDirectory(string dir)
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

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
            var options = driver.Options;
            options.TargetTriple = Triple;
            options.Abi = Abi;
            options.LibraryName = "CppSharp.CppParser.dll";
            options.GeneratorKind = Kind;
            options.Headers.AddRange(new string[]
            {
                "AST.h",
                "Sources.h",
                "CppParser.h"
            });
            options.Libraries.Add("CppSharp.CppParser.lib");

            if (Triple.Contains("apple"))
                SetupMacOptions(options);

            var basePath = Path.Combine(GetSourceDirectory("src"), "CppParser");

#if OLD_PARSER
            options.IncludeDirs.Add(basePath);
            options.LibraryDirs.Add(".");

#else
            options.addIncludeDirs(basePath);
            options.addLibraryDirs(".");
#endif

            options.OutputDir = Path.Combine(GetSourceDirectory("src"), @"CppParser\Bindings",
                Kind.ToString());

            if (Kind == GeneratorKind.CSharp)
                options.OutputDir = Path.Combine(options.OutputDir, options.TargetTriple);

            options.GenerateLibraryNamespace = false;
            options.CheckSymbols = false;
        }

        private static void SetupMacOptions(DriverOptions options)
        {
            options.MicrosoftMode = false;
            options.NoBuiltinIncludes = true;

            var headersPath = Path.Combine(GetSourceDirectory("build"), "headers",
                "osx");

#if OLD_PARSER
            options.SystemIncludeDirs.Add(headersPath + @"include\osx");
            options.SystemIncludeDirs.Add(headersPath + @"lib\libcxx\include");
            options.SystemIncludeDirs.Add(headersPath + @"lib\clang\4.2\include");
            options.Arguments.Add("-stdlib=libc++");
#else
            options.addSystemIncludeDirs(Path.Combine(headersPath, "include"));
            options.addSystemIncludeDirs(Path.Combine(headersPath, "clang", "4.2", "include"));
            options.addSystemIncludeDirs(Path.Combine(headersPath, "libcxx", "include"));
            options.addArguments("-stdlib=libc++");
#endif
        }

        public void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new CheckMacroPass());
            driver.AddTranslationUnitPass(new IgnoreStdFieldsPass());
            driver.AddTranslationUnitPass(new GetterSetterToPropertyPass());
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("CppSharp::ParserOptions");
            ctx.SetClassAsValueType("CppSharp::ParserDiagnostic");
            ctx.SetClassAsValueType("CppSharp::ParserResult");

            ctx.RenameNamespace("CppSharp::CppParser", "Parser");
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Generating the C++/CLI parser bindings for Windows...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI, "i686-pc-win32",
                CppAbi.Microsoft));
            Console.WriteLine();

            Console.WriteLine("Generating the C# parser bindings for Windows...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-pc-win32",
                CppAbi.Microsoft));
            Console.WriteLine();

            var osxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\osx");
            if (Directory.Exists(osxHeadersPath))
            {
                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp, "i686-apple-darwin12.4.0",
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
