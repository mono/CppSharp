using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Types;

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
            options.Libraries.Add("CppSharp.CppParser.lib");

            var basePath = Path.Combine(GetSourceDirectory(), "CppParser");

#if OLD_PARSER
            options.IncludeDirs.Add(basePath);
            options.LibraryDirs.Add(".");

#else
            options.addIncludeDirs(basePath);
            options.addLibraryDirs(".");
#endif

            options.OutputDir = "../../../../src/CppParser/Bindings/";
            options.OutputDir += Kind.ToString();
            options.GenerateLibraryNamespace = false;
            options.CheckSymbols = false;
            options.Verbose = true;
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

    public class IgnoreStdFieldsPass : TranslationUnitPass
    {
        public override bool VisitFieldDecl(Field field)
        {
            if (field.Ignore)
                return false;

            var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
            var typeName = field.QualifiedType.Visit(typePrinter);

            if (!typeName.Contains("std::"))
                return false;

            field.ExplicityIgnored = true;
            return true;
        }
    }
}
