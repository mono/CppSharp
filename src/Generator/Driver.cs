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
        public IDiagnosticConsumer Diagnostics { get; private set; }
        public Parser Parser { get; private set; }
        public TypeMapDatabase TypeDatabase { get; private set; }
        public ILibrary Transform { get; private set; }
        public Generator Generator { get; private set; }

        public Library Library { get; private set; }
        public Library LibrarySymbols { get; private set; }

        public Driver(DriverOptions options, IDiagnosticConsumer diagnostics,
            ILibrary transform)
        {
            Options = options;
            Diagnostics = diagnostics;
            Parser = new Parser(Options);
            TypeDatabase = new TypeMapDatabase();
            Transform = transform;
        }

        static void ValidateOptions(DriverOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.LibraryName))
                throw new InvalidDataException();

            if (options.OutputDir == null)
                options.OutputDir = Directory.GetCurrentDirectory();

            for (var i = 0; i < options.IncludeDirs.Count; i++)
            {
                options.IncludeDirs[i] = Path.GetFullPath(options.IncludeDirs[i]);
            }

            for (var i = 0; i < options.LibraryDirs.Count; i++)
            {
                options.LibraryDirs[i] = Path.GetFullPath(options.LibraryDirs[i]);
            }

            if (string.IsNullOrWhiteSpace(options.OutputNamespace))
                options.OutputNamespace = options.LibraryName;
        }

        public void Setup()
        {
            ValidateOptions(Options);

            if (!Directory.Exists(Options.OutputDir))
                Directory.CreateDirectory(Options.OutputDir);

            if (!Generators.ContainsKey(Options.GeneratorKind))
                throw new NotImplementedException("Unknown generator kind");

            Generator = Generators[Options.GeneratorKind](this);
        }

        public bool ParseCode()
        {
            if (!Parser.ParseHeaders(Options.Headers))
                return false;

            Library = Parser.Library;

            return true;
        }

        public bool ParseLibraries()
        {
            if (!Parser.ParseLibraries(Options.Libraries))
                return false;

            LibrarySymbols = Parser.Library;

            return true;
        }

        public void ProcessCode()
        {
            TypeDatabase.SetupTypeMaps();

            if (Transform != null)
                Transform.Preprocess(this, Library);

            var passes = new PassBuilder(this);
            passes.CleanUnit(Options);
            passes.SortDeclarations();
            passes.ResolveIncompleteDecls();

            if (Transform != null)
                Transform.SetupPasses(this, passes);

            passes.CleanInvalidDeclNames();
            passes.CheckIgnoredDecls();

            passes.CheckTypeReferences();
            passes.CheckFlagEnums();
            passes.CheckAmbiguousOverloads();

            Generator.SetupPasses(passes);

            passes.RunPasses();

            if (Transform != null)
                Transform.Postprocess(Library);
        }

        public void GenerateCode()
        {
            if (Library.TranslationUnits.Count <= 0)
                return;

            foreach (var unit in Library.TranslationUnits)
            {
                if (unit.Ignore || !unit.HasDeclarations)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                var outputs = new List<GeneratorOutput>();
                if (!Generator.Generate(unit, outputs))
                    continue;

                foreach (var output in outputs)
                {
                    Diagnostics.EmitMessage(DiagnosticId.FileGenerated,
                        "Generated '{0}'", Path.GetFileName(output.OutputPath));

                    var text = output.Template.ToString();
                    File.WriteAllText(output.OutputPath, text);
                }
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
        public Func<TranslationUnit, string> GenerateName;
    }

    public static class ConsoleDriver
    {
        static void OnFileParsed(string file, ParserResult result)
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

        public static void Run(ILibrary library)
        {
            Console.BufferHeight = 1999;

            var options = new DriverOptions();
            var driver = new Driver(options, new TextDiagnosticPrinter(),
                library);
            library.Setup(driver);
            driver.Setup();

            driver.Parser.OnHeaderParsed += OnFileParsed;
            driver.Parser.OnLibraryParsed += OnFileParsed;

            Console.WriteLine("Parsing libraries...");
            if (!driver.ParseLibraries())
                return;

            Console.WriteLine("Indexing library symbols...");
            driver.LibrarySymbols.IndexSymbols();

            Console.WriteLine("Parsing code...");
            if (!driver.ParseCode())
                return;

            Console.WriteLine("Processing code...");
            driver.ProcessCode();

            Console.WriteLine("Generating code...");
            driver.GenerateCode();
        }
    }
}