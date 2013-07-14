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
        public delegate Generator CreateGeneratorDelegate(Driver driver);
        public static Dictionary<LanguageGeneratorKind, CreateGeneratorDelegate>
            Generators;

        static Driver()
        {
            Generators = new Dictionary<LanguageGeneratorKind,
                CreateGeneratorDelegate>();
            Generators[LanguageGeneratorKind.CSharp] = driver =>
                new CSharpGenerator(driver);
            Generators[LanguageGeneratorKind.CPlusPlusCLI] = driver =>
                new CLIGenerator(driver);
        }

        public DriverOptions Options { get; private set; }
        public IDiagnosticConsumer Diagnostics { get; private set; }
        public Parser Parser { get; private set; }
        public TypeMapDatabase TypeDatabase { get; private set; }
        public PassBuilder Passes { get; private set; }
        public Generator Generator { get; private set; }

        public Library Library { get; private set; }
        public Library LibrarySymbols { get; private set; }

        public Driver(DriverOptions options, IDiagnosticConsumer diagnostics)
        {
            Options = options;
            Diagnostics = diagnostics;
            Parser = new Parser(Options);
            TypeDatabase = new TypeMapDatabase();
            Passes = new PassBuilder(this);
        }

        static void ValidateOptions(DriverOptions options)
        {
            if (!Generators.ContainsKey(options.GeneratorKind))
                throw new InvalidOptionException();

            if (string.IsNullOrWhiteSpace(options.LibraryName))
                throw new InvalidOptionException();

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

            Generator = Generators[Options.GeneratorKind](this);
            TypeDatabase.SetupTypeMaps();
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

        public void AddPrePasses()
        {
            Passes.CleanUnit(Options);
            Passes.SortDeclarations();
            Passes.ResolveIncompleteDecls();
        }

        public void AddPostPasses()
        {
            Passes.CleanInvalidDeclNames();
            Passes.CheckIgnoredDecls();
            Passes.CheckTypeReferences();
            Passes.CheckFlagEnums();
            Passes.CheckAmbiguousOverloads();
            Generator.SetupPasses(Passes);
        }

        public void ProcessCode()
        {
            foreach (var pass in Passes.Passes)
                pass.VisitLibrary(Library);
        }

        public List<GeneratorOutput> GenerateCode()
        {
            var outputs = Generator.Generate();
            return outputs;
        }

        public void WriteCode(List<GeneratorOutput> outputs)
        {
            var outputPath = Options.OutputDir ?? Directory.GetCurrentDirectory();

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            foreach (var output in outputs)
            {
                var fileBase = output.TranslationUnit.FileNameWithoutExtension;

                if (Options.GenerateName != null)
                    fileBase = Options.GenerateName(output.TranslationUnit);

                foreach (var template in output.Templates)
                {
                    var fileName = string.Format("{0}.{1}", fileBase, template.FileExtension);
                    Diagnostics.EmitMessage(DiagnosticId.FileGenerated, "Generated '{0}'", fileName);

                    var filePath = Path.Combine(outputPath, fileName);

                    var text = template.GenerateText();
                    File.WriteAllText(Path.GetFullPath(filePath), text);
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

    public class InvalidOptionException : Exception
    {
        public InvalidOptionException()
        {
            
        }
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
            var driver = new Driver(options, new TextDiagnosticPrinter());
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
            library.Preprocess(driver, driver.Library);

            driver.AddPrePasses();
            library.SetupPasses(driver, driver.Passes);
            driver.AddPostPasses();

            driver.ProcessCode();
            library.Postprocess(driver.Library);

            Console.WriteLine("Generating code...");
            var outputs = driver.GenerateCode();
            driver.WriteCode(outputs);
        }
    }
}