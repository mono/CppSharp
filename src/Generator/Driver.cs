using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace CppSharp
{
    public class Driver
    {
        public DriverOptions Options { get; private set; }
        public ILibrary Transform { get; private set; }
        public IDiagnosticConsumer Diagnostics { get; private set; }
        public TypeMapDatabase TypeDatabase { get; private set; }
        public Library Library { get; private set; }
        public Library LibrarySymbols { get; private set; }
        public Generator Generator { get; private set; }

        public Driver(DriverOptions options, ILibrary transform)
        {
            Options = options;
            Transform = transform;
            Diagnostics = new TextDiagnosticPrinter();
            TypeDatabase = new TypeMapDatabase();
        }

        public void Setup()
        {
            if (Transform != null)
                Transform.Setup(Options);

            ValidateOptions();

            Generator = CreateGenerator();
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(Options.LibraryName))
                throw new InvalidDataException();

            if (Options.OutputDir == null)
                Options.OutputDir = Directory.GetCurrentDirectory();

            for (var i = 0; i < Options.IncludeDirs.Count; i++)
            {
                Options.IncludeDirs[i] = Path.GetFullPath(Options.IncludeDirs[i]);
            }

            for (var i = 0; i < Options.LibraryDirs.Count; i++)
            {
                Options.LibraryDirs[i] = Path.GetFullPath(Options.LibraryDirs[i]);
            }

            if (string.IsNullOrWhiteSpace(Options.OutputNamespace))
                Options.OutputNamespace = Options.LibraryName;
        }

        Generator CreateGenerator()
        {
            switch (Options.GeneratorKind)
            {
                case LanguageGeneratorKind.CSharp:
                    return new CSharpGenerator(this);
                case LanguageGeneratorKind.CPlusPlusCLI:
                    return new CLIGenerator(this);
                default:
                    throw new NotImplementedException("Unknown language generator kind");
            }
        }

        private void OnFileParsed(string file, ParserResult result)
        {
            switch (result.Kind)
            {
                case ParserResultKind.Success:
                    Console.WriteLine("  Parsed '{0}'", file);
                    break;
                case ParserResultKind.Error:
                    Console.WriteLine("  Error parsing '{0}'", file);
                    break;
                case ParserResultKind.FileNotFound:
                    Console.WriteLine("  File '{0}' was not found", file);
                    break;
            }

            foreach (var diag in result.Diagnostics)
            {
                Console.WriteLine(string.Format("{0}({1},{2}): {3}: {4}",
                    diag.FileName, diag.LineNumber, diag.ColumnNumber,
                    diag.Level.ToString().ToLower(), diag.Message));
            }
        }

        public bool ParseCode()
        {
            Console.WriteLine("Parsing code...");

            var parser = new Parser(Options);
            parser.OnHeaderParsed += OnFileParsed;

            if( !parser.ParseHeaders(Options.Headers) )
                return false;

            Library = parser.Library;

            return true;
        }

        public bool ParseLibraries()
        {
            Console.WriteLine("Parsing libraries...");

            var parser = new Parser(Options);
            parser.OnLibraryParsed += OnFileParsed;

            if (!parser.ParseLibraries(Options.Libraries))
                return false;

            LibrarySymbols = parser.Library;

            Console.WriteLine("Indexing library symbols...");
            LibrarySymbols.IndexSymbols();

            return true;
        }

        public void ProcessCode()
        {
            TypeDatabase.SetupTypeMaps();

            if (Transform != null)
                Transform.Preprocess(Library);

            var passes = new PassBuilder(this);
            passes.CleanUnit(Options);
            passes.SortDeclarations();
            passes.ResolveIncompleteDecls(TypeDatabase);
            passes.CheckTypeReferences();
            passes.CheckFlagEnums();
            passes.CheckAmbiguousOverloads();

            if (Options.GeneratorKind == LanguageGeneratorKind.CSharp)
            {
                passes.CheckAbiParameters(Options);
                passes.CheckOperatorOverloads();
            }

            if (Transform != null)
                Transform.SetupPasses(this, passes);

            passes.CleanInvalidDeclNames();

            passes.RunPasses();

            if (Transform != null)
                Transform.Postprocess(Library);
        }

        public void GenerateCode()
        {
            if (Library.TranslationUnits.Count <= 0)
                return;

            Console.WriteLine("Generating wrapper code...");

            if (!Directory.Exists(Options.OutputDir))
                Directory.CreateDirectory(Options.OutputDir);

            // Process everything in the global namespace for now.
            foreach (var unit in Library.TranslationUnits)
            {
                if (unit.Ignore || !unit.HasDeclarations)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                // Generate the target code.
                Generator.Generate(unit);
            }
        }

        public static void Run(ILibrary library)
        {
            var options = new DriverOptions();

            var driver = new Driver(options, library);
            driver.Setup();
            
            if (driver.ParseLibraries() && driver.ParseCode())
            {
                driver.ProcessCode();
                driver.GenerateCode();
            }
        }
    }

    public class DriverOptions
    {
        public DriverOptions()
        {
            Defines = new List<string>();
            IncludeDirs = new List<string>();
            SystemIncludeDirs = new List<string>();
            Headers = new List<string>();

            var platform = Environment.OSVersion.Platform;
            var isUnix = platform == PlatformID.Unix || platform == PlatformID.MacOSX;
            MicrosoftMode = !isUnix;
            Abi = isUnix ? CppAbi.Itanium : CppAbi.Microsoft;

            LibraryDirs = new List<string>();
            Libraries = new List<string>();

            GeneratorKind = LanguageGeneratorKind.CSharp;
            GenerateLibraryNamespace = true;
            GeneratePartialClasses = true;
            OutputInteropIncludes = true;
        }

        // General options
        public bool Verbose;
        public bool ShowHelpText;
        public bool OutputDebug;

        // Parser options
        public List<string> Defines;
        public List<string> IncludeDirs;
        public List<string> SystemIncludeDirs;
        public List<string> Headers;
        public bool NoStandardIncludes;
        public bool NoBuiltinIncludes;
        public bool MicrosoftMode;
        public string TargetTriple;
        public int ToolsetToUse;
        public bool IgnoreParseErrors;
        public CppAbi Abi;
        public bool IsItaniumAbi { get { return Abi == CppAbi.Itanium; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }

        // Library options
        public List<string> LibraryDirs;
        public List<string> Libraries;

        // Generator options
        public LanguageGeneratorKind GeneratorKind;
        public string OutputNamespace;
        public string OutputDir;
        public string LibraryName;
        public bool OutputInteropIncludes;
        public bool GenerateLibraryNamespace;
        public bool GenerateFunctionTemplates;
        public bool GeneratePartialClasses;
        public string Template;
        public string Assembly;
        public string IncludePrefix;
        public bool WriteOnlyWhenChanged;

    }
}