using Cxxi.Generators;
using Cxxi.Passes;
using Cxxi.Types;
using System;
using System.Collections.Generic;

namespace Cxxi
{
    public class Driver
    {
        private readonly DriverOptions options;
        private readonly ILibrary transform;
        private readonly IDiagnosticConsumer diagnostics;
        private readonly TypeMapDatabase typeDatabase;
        private Library library;

        public Driver(DriverOptions options, ILibrary transform)
        {
            this.options = options;
            this.transform = transform;
            this.diagnostics = new TextDiagnosticPrinter();

            typeDatabase = new TypeMapDatabase();
            typeDatabase.SetupTypeMaps();
        }

        public void ParseCode()
        {
            Console.WriteLine("Parsing code...");

            var headers = new List<string>();
            transform.SetupHeaders(headers);

            var parser = new Parser(options);
            parser.HeaderParsed += (file, result) =>
                Console.WriteLine(result.Success ? "  Parsed '" + file + "'." :
                                            "  Could not parse '" + file + "'.");

            parser.ParseHeaders(headers);
            parser.ParseHeaders(options.Headers);

            library = parser.Library;
        }

        public void ProcessCode()
        {
            if (Transform != null)
                Transform.Preprocess(library);

            var passes = new PassBuilder(library);
            passes.SortDeclarations();
            passes.ResolveIncompleteDecls(typeDatabase);
            passes.CleanInvalidDeclNames();
            passes.CheckFlagEnums();

            if (transform != null)
                transform.SetupPasses(passes);

            var transformer = new Transform() { Options = options, Passes = passes };
            transformer.TransformLibrary(library);

            if (Transform != null)
                Transform.Postprocess(library);
        }

        public void GenerateCode()
        {
            if (library.TranslationUnits.Count <= 0)
                return;

            Console.WriteLine("Generating wrapper code...");

            Generator generator = null;

            switch (Options.GeneratorKind)
            {
                case LanguageGeneratorKind.CSharp:
                    generator = new CSharpGenerator(this);
                    break;
                case LanguageGeneratorKind.CPlusPlusCLI:
                default:
                    generator = new CLIGenerator(this);
                    break;
            }

            if (!Directory.Exists(Options.OutputDir))
                Directory.CreateDirectory(Options.OutputDir);

            // Process everything in the global namespace for now.
            foreach (var unit in Library.TranslationUnits)
            {
                if (unit.ExplicityIgnored || !unit.HasDeclarations)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                // Generate the target code.
                generator.Generate(unit);
            }
        }
    }

    public class DriverOptions
    {
        public DriverOptions()
        {
            Defines = new List<string>();
            IncludeDirs = new List<string>();
            Headers = new List<string>();
            Assembly = string.Empty;
            GeneratorKind = LanguageGeneratorKind.CSharp;
        }

        public bool Verbose = false;
        public bool ShowHelpText = false;
        public bool OutputDebug = false;
        public string OutputNamespace;
        public string OutputDir;
        public string LibraryName;
        public List<string> Defines;
        public List<string> IncludeDirs;
        public List<string> Headers;
        public string Template;
        public string Assembly;
        public int ToolsetToUse;
        public string WrapperSuffix;
        public LanguageGeneratorKind GeneratorKind;
    }
}