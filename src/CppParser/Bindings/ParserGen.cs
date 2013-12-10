using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp
{
    /// <summary>
    /// Generates C# and C++/CLI bindings for the CppSharp.CppParser project.
    /// </summary>
    class ParserGen : ILibrary
    {
        private readonly GeneratorKind Kind;

        public ParserGen(GeneratorKind kind)
        {
            Kind = kind;
        }

        static string GetSourceDirectory()
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "src");

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Could not find sources directory");
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = "CppSharp.CppParser.dll";
            options.GeneratorKind = Kind;
            options.Headers.Add("AST.h");
            options.Headers.Add("CppParser.h");
            var basePath = Path.Combine(GetSourceDirectory(), "CppParser");
            options.IncludeDirs.Add(basePath);
            options.Libraries.Add("CppSharp.CppParser.lib");
            options.LibraryDirs.Add(".");
            options.OutputDir = "../../../../src/CppParser/Bindings/";
            options.OutputDir += Kind.ToString();
            options.GenerateLibraryNamespace = false;
            options.CheckSymbols = false;
        }

        public void SetupPasses(Driver driver)
        {

        }

        public void Preprocess(Driver driver, ASTContext lib)
        {
            lib.SetClassAsValueType("CppSharp::ParserOptions");
            lib.SetClassAsValueType("CppSharp::ParserDiagnostic");
            lib.SetClassAsValueType("CppSharp::ParserResult");

            lib.RenameNamespace("CppSharp::CppParser", "Parser");
        }

        public void Postprocess(Driver driver, ASTContext lib)
        {
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Generating the C++/CLI parser bindings...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI));
            Console.WriteLine();

            Console.WriteLine("Generating the C# parser bindings...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp));
        }
    }
}
