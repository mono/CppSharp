using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp
{
    class ParserGen : ILibrary
    {
        private readonly LanguageGeneratorKind Kind;

        public ParserGen(LanguageGeneratorKind kind)
        {
            Kind = kind;
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = "CppSharp";
            options.GeneratorKind = Kind;
            options.Headers.Add("AST.h");
            options.Headers.Add("CppParser.h");
            options.IncludeDirs.Add("../../../src/CppParser/");
            options.Libraries.Add("CppSharp.CppParser.lib");
            options.LibraryDirs.Add(".");
            options.OutputDir = "../../../src/CppParser/Bindings/";
            options.OutputDir += Kind.ToString();
            options.GenerateLibraryNamespace = false;
            //options.CheckSymbols = false;
        }

        public void SetupPasses(Driver driver)
        {

        }

        public void Preprocess(Driver driver, Library lib)
        {
            lib.SetClassAsValueType("CppSharp::ParserOptions");
            lib.SetClassAsValueType("CppSharp::ParserDiagnostic");
            lib.SetClassAsValueType("CppSharp::ParserResult");

            lib.RenameNamespace("CppSharp::CppParser", "Parser");
        }

        public void Postprocess(Library lib)
        {

        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new ParserGen(LanguageGeneratorKind.CLI));
            ConsoleDriver.Run(new ParserGen(LanguageGeneratorKind.CSharp));
        }
    }
}
