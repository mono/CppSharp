using System;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp
{
    class ParserGen : ILibrary
    {
        private readonly GeneratorKind Kind;

        public ParserGen(GeneratorKind kind)
        {
            Kind = kind;
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = "CppSharp.CppParser.dll";
            options.GeneratorKind = Kind;
            options.Headers.Add("AST.h");
            options.Headers.Add("CppParser.h");
            options.IncludeDirs.Add("../../../../src/CppParser/");
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
#if CLI
            Console.WriteLine("Generating the C++/CLI parser bindings...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CLI));
            Console.WriteLine();
#endif

            Console.WriteLine("Generating the C# parser bindings...");
            ConsoleDriver.Run(new ParserGen(GeneratorKind.CSharp));
        }
    }
}
