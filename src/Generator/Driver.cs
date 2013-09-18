using System.Text;
using CppSharp.AST;
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
            Generators[LanguageGeneratorKind.CLI] = driver =>
                new CLIGenerator(driver);
        }

        public DriverOptions Options { get; private set; }
        public IDiagnosticConsumer Diagnostics { get; private set; }
        public Parser Parser { get; private set; }
        public TypeMapDatabase TypeDatabase { get; private set; }
        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; private set; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; private set; }
        public Generator Generator { get; private set; }

        public Library Library { get; private set; }
        public Library LibrarySymbols { get; private set; }

        public Driver(DriverOptions options, IDiagnosticConsumer diagnostics)
        {
            Options = options;
            Diagnostics = diagnostics;
            Parser = new Parser(Options);
            Parser.OnHeaderParsed += OnFileParsed;
            Parser.OnLibraryParsed += OnFileParsed;
            TypeDatabase = new TypeMapDatabase();
            TranslationUnitPasses = new PassBuilder<TranslationUnitPass>(this);
            GeneratorOutputPasses = new PassBuilder<GeneratorOutputPass>(this);
        }

        static void ValidateOptions(DriverOptions options)
        {
            if (!Generators.ContainsKey(options.GeneratorKind))
                throw new InvalidOptionException();

            if (string.IsNullOrWhiteSpace(options.LibraryName))
                throw new InvalidOptionException();

            for (var i = 0; i < options.IncludeDirs.Count; i++)
                options.IncludeDirs[i] = Path.GetFullPath(options.IncludeDirs[i]);

            for (var i = 0; i < options.LibraryDirs.Count; i++)
                options.LibraryDirs[i] = Path.GetFullPath(options.LibraryDirs[i]);

            if (string.IsNullOrWhiteSpace(options.OutputNamespace))
                options.OutputNamespace = options.LibraryName;
        }

        public void Setup()
        {
            ValidateOptions(Options);

            Generator = Generators[Options.GeneratorKind](this);
            TypeDatabase.SetupTypeMaps();
        }

        void OnFileParsed(string file, ParserResult result)
        {
            switch (result.Kind)
            {
                case ParserResultKind.Success:
                    Diagnostics.EmitMessage(DiagnosticId.ParseResult,
                        "Parsed '{0}'", file);
                    break;
                case ParserResultKind.Error:
                    Diagnostics.EmitError(DiagnosticId.ParseResult,
                        "Error parsing '{0}'", file);
                    break;
                case ParserResultKind.FileNotFound:
                    Diagnostics.EmitError(DiagnosticId.ParseResult,
                        "File '{0}' was not found", file);
                    break;
            }

            foreach (var diag in result.Diagnostics)
            {
                if (Options.IgnoreParseWarnings
                    && diag.Level == ParserDiagnosticLevel.Warning)
                    continue;

                Diagnostics.EmitMessage(DiagnosticId.ParserDiagnostic,
                    "{0}({1},{2}): {3}: {4}", diag.FileName, diag.LineNumber,
                    diag.ColumnNumber, diag.Level.ToString().ToLower(),
                    diag.Message);
            }
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

        public void SetupPasses(ILibrary library)
        { 
            TranslationUnitPasses.AddPass(new CleanUnitPass(Options));
            TranslationUnitPasses.AddPass(new SortDeclarationsPass());
            TranslationUnitPasses.AddPass(new ResolveIncompleteDeclsPass());
            TranslationUnitPasses.AddPass(new CleanInvalidDeclNamesPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());
            if (Options.IsCSharpGenerator)
                TranslationUnitPasses.AddPass(new GenerateInlinesCodePass());

            library.SetupPasses(this);

            TranslationUnitPasses.AddPass(new FindSymbolsPass());
            TranslationUnitPasses.AddPass(new MoveOperatorToClassPass());
            TranslationUnitPasses.AddPass(new CheckAmbiguousFunctions());
            TranslationUnitPasses.AddPass(new CheckOperatorsOverloadsPass());
            TranslationUnitPasses.AddPass(new CheckVirtualOverrideReturnCovariance());
            TranslationUnitPasses.AddPass(new MultipleInheritancePass());

            Generator.SetupPasses();
            TranslationUnitPasses.AddPass(new FieldToPropertyPass());
            TranslationUnitPasses.AddPass(new CleanInvalidDeclNamesPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());
            TranslationUnitPasses.AddPass(new CheckFlagEnumsPass());
            TranslationUnitPasses.AddPass(new CheckDuplicatedNamesPass());
            if (Options.GenerateAbstractImpls)
                TranslationUnitPasses.AddPass(new GenerateAbstractImplementationsPass());
        }

        public void ProcessCode()
        {
            TranslationUnitPasses.RunPasses(pass => pass.VisitLibrary(Library));
            Generator.Process();
        }

        public List<GeneratorOutput> GenerateCode()
        {
            return Generator.Generate();
        }

        public void WriteCode(List<GeneratorOutput> outputs)
        {
            var outputPath = Options.OutputDir;

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
                    File.WriteAllText(Path.GetFullPath(filePath), template.Generate());
                }
            }
        }

        public void AddTranslationUnitPass(TranslationUnitPass pass)
        {
            TranslationUnitPasses.AddPass(pass);
        }

        public void AddGeneratorOutputPass(GeneratorOutputPass pass)
        {
            GeneratorOutputPasses.AddPass(pass);
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

            OutputDir = Directory.GetCurrentDirectory();

            var platform = Environment.OSVersion.Platform;
            var isUnix = platform == PlatformID.Unix || platform == PlatformID.MacOSX;
            MicrosoftMode = !isUnix;
            Abi = isUnix ? CppAbi.Itanium : CppAbi.Microsoft;

            LibraryDirs = new List<string>();
            Libraries = new List<string>();
            CheckSymbols = true;

            GeneratorKind = LanguageGeneratorKind.CSharp;
            GenerateLibraryNamespace = true;
            GeneratePartialClasses = true;
            OutputInteropIncludes = true;
            MaxIndent = 80;
            CommentPrefix = "///";

            Encoding = Encoding.ASCII;
        }

        // General options
        public bool Verbose;
        public bool Quiet;
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
        public bool IgnoreParseWarnings;
        public bool IgnoreParseErrors;
        public CppAbi Abi;
        public bool IsItaniumAbi { get { return Abi == CppAbi.Itanium; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }

        // Library options
        public List<string> LibraryDirs;
        public List<string> Libraries;
        public bool CheckSymbols;
        public string SharedLibraryName;

        // Generator options
        public LanguageGeneratorKind GeneratorKind;
        public string OutputNamespace;
        public string OutputDir;
        public string LibraryName;
        public bool OutputInteropIncludes;
        public bool GenerateLibraryNamespace;
        public bool GenerateFunctionTemplates;
        public bool GeneratePartialClasses;
        public bool GenerateVirtualTables;
        public bool GenerateAbstractImpls;
        public bool GenerateInternalImports;
        public string IncludePrefix;
        public bool WriteOnlyWhenChanged;
        public Func<TranslationUnit, string> GenerateName;
        public int MaxIndent;
        public string CommentPrefix;

        public Encoding Encoding { get; set; }

        private string inlinesLibraryName;
        public string InlinesLibraryName
        {
            get
            {
                if (string.IsNullOrEmpty(inlinesLibraryName))
                {
                    return string.Format("{0}-inlines", OutputNamespace);
                }
                return inlinesLibraryName;
            }
            set { inlinesLibraryName = value; }
        }

        public bool IsCSharpGenerator
        {
            get { return GeneratorKind == LanguageGeneratorKind.CSharp; }
        }

        public bool IsCLIGenerator
        {
            get { return GeneratorKind == LanguageGeneratorKind.CLI; }
        }

        public bool Is32Bit { get { return true; } }
    }

    public class InvalidOptionException : Exception
    {
    }

    public static class ConsoleDriver
    {
        public static void Run(ILibrary library)
        {
            var options = new DriverOptions();
            var driver = new Driver(options, new TextDiagnosticPrinter());
            library.Setup(driver);
            driver.Setup();

            if (!options.Quiet)
                Console.WriteLine("Parsing libraries...");

            if (!driver.ParseLibraries())
                return;

            if (!options.Quiet)
                Console.WriteLine("Indexing library symbols...");

            driver.LibrarySymbols.IndexSymbols();

            if (!options.Quiet) 
                Console.WriteLine("Parsing code...");

            if (!driver.ParseCode())
                return;

            if (!options.Quiet)
                Console.WriteLine("Processing code...");

            library.Preprocess(driver, driver.Library);

            driver.SetupPasses(library);

            driver.ProcessCode();
            library.Postprocess(driver.Library);

            if (!options.Quiet)
                Console.WriteLine("Generating code...");

            var outputs = driver.GenerateCode();

            foreach (var output in outputs)
            {
                foreach (var pass in driver.GeneratorOutputPasses.Passes)
                {
                    pass.Driver = driver;
                    pass.VisitGeneratorOutput(output);
                }
            }

            driver.WriteCode(outputs);
        }
    }
}