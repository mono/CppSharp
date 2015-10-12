using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using Microsoft.CSharp;
using CppSharp.Parser;

namespace CppSharp
{
    public class Driver
    {
        public IDiagnosticConsumer Diagnostics { get; set; }

        public DriverOptions Options { get; private set; }
        public Project Project { get; private set; }

        public TypeMapDatabase TypeDatabase { get; private set; }
        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; private set; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; private set; }
        public Generator Generator { get; private set; }

        public ASTContext ASTContext { get; private set; }
        public SymbolContext Symbols { get; private set; }

        private static readonly Dictionary<string, string> libraryMappings = new Dictionary<string, string>();

        public Driver(DriverOptions options, IDiagnosticConsumer diagnostics)
        {
            Options = options;
            Diagnostics = diagnostics;
            Project = new Project();
            ASTContext = new ASTContext();
            Symbols = new SymbolContext();
            TypeDatabase = new TypeMapDatabase();
            TranslationUnitPasses = new PassBuilder<TranslationUnitPass>(this);
            GeneratorOutputPasses = new PassBuilder<GeneratorOutputPass>(this);
        }

        Generator CreateGeneratorFromKind(GeneratorKind kind)
        {
            switch (kind)
            {
                case GeneratorKind.CLI:
                    return new CLIGenerator(this);
                case GeneratorKind.CSharp:
                    return new CSharpGenerator(this);
            }

            return null;
        }

        static void ValidateOptions(DriverOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.LibraryName))
                throw new InvalidOptionException();

            if (options.NoGenIncludeDirs != null)
                foreach (var incDir in options.NoGenIncludeDirs)
                    options.addIncludeDirs(incDir);

            if (string.IsNullOrWhiteSpace(options.OutputNamespace))
                options.OutputNamespace = options.LibraryName;
        }

        public void Setup()
        {
            ValidateOptions(Options);

            SetupIncludes();

            TypeDatabase.SetupTypeMaps(Options.GeneratorKind);
            Generator = CreateGeneratorFromKind(Options.GeneratorKind);
        }

        public void SetupIncludes()
        {
            if (Platform.IsMacOS)
                Options.SetupXcode();
            else if (Platform.IsWindows && !Options.NoBuiltinIncludes)
                Options.SetupMSVC();
        }

        void OnSourceFileParsed(SourceFile file, ParserResult result)
        {
            OnFileParsed(file.Path, result);
        }

        void OnFileParsed(string file, ParserResult result)
        {
            switch (result.Kind)
            {
                case ParserResultKind.Success:
                    Diagnostics.Message("Parsed '{0}'", file);
                    break;
                case ParserResultKind.Error:
                    Diagnostics.Error("Error parsing '{0}'", file);
                    break;
                case ParserResultKind.FileNotFound:
                    Diagnostics.Error("File '{0}' was not found", file);
                    break;
            }

            for (uint i = 0; i < result.DiagnosticsCount; ++i)
            {
                var diag = result.getDiagnostics(i);

                if (Options.IgnoreParseWarnings
                    && diag.Level == ParserDiagnosticLevel.Warning)
                    continue;

                if (diag.Level == ParserDiagnosticLevel.Note)
                    continue;

                Diagnostics.Message("{0}({1},{2}): {3}: {4}",
                    diag.FileName, diag.LineNumber, diag.ColumnNumber,
                    diag.Level.ToString().ToLower(), diag.Message);
            }
        }

        public ParserOptions BuildParseOptions(SourceFile file)
        {
            var options = new ParserOptions
            {
                FileName = file.Path,
                Abi = Options.Abi,
                ToolSetToUse = Options.ToolSetToUse,
                TargetTriple = Options.TargetTriple,
                NoStandardIncludes = Options.NoStandardIncludes,
                NoBuiltinIncludes = Options.NoBuiltinIncludes,
                MicrosoftMode = Options.MicrosoftMode,
                Verbose = Options.Verbose,
                LanguageVersion = Options.LanguageVersion
            };

            // This eventually gets passed to Clang's MSCompatibilityVersion, which
            // is in turn used to derive the value of the built-in define _MSC_VER.
            // It used to receive a 4-digit based identifier but now expects a full
            // version MSVC digit, so check if we still have the old version and 
            // convert to the right format.

            if (Options.ToolSetToUse.ToString(CultureInfo.InvariantCulture).Length == 4)
                Options.ToolSetToUse *= 100000;

            for (uint i = 0; i < Options.ArgumentsCount; ++i)
            {
                var arg = Options.getArguments(i);
                options.addArguments(arg);
            }

            for (uint i = 0; i < Options.IncludeDirsCount; ++i)
            {
                var include = Options.getIncludeDirs(i);
                options.addIncludeDirs(include);
            }

            for (uint i = 0; i < Options.SystemIncludeDirsCount; ++i)
            {
                var include = Options.getSystemIncludeDirs(i);
                options.addSystemIncludeDirs(include);
            }

            for (uint i = 0; i < Options.DefinesCount; ++i)
            {
                var define = Options.getDefines(i);
                options.addDefines(define);
            }

            for (uint i = 0; i < Options.UndefinesCount; ++i)
            {
                var define = Options.getUndefines(i);
                options.addUndefines(define);
            }

            for (uint i = 0; i < Options.LibraryDirsCount; ++i)
            {
                var lib = Options.getLibraryDirs(i);
                options.addLibraryDirs(lib);
            }

            return options;
        }

        public bool ParseCode()
        {
            var parser = new ClangParser(new Parser.AST.ASTContext());

            parser.SourceParsed += OnSourceFileParsed;
            parser.ParseProject(Project);
           
            TargetInfo = parser.GetTargetInfo(Options);

            ASTContext = ClangParser.ConvertASTContext(parser.ASTContext);

            return true;
        }

        public void BuildParseOptions()
        {
            foreach (var header in Options.Headers)
            {
                var source = Project.AddFile(header);
                source.Options = BuildParseOptions(source);
            }
        }

        public ParserTargetInfo TargetInfo { get; set; }

        public bool ParseLibraries()
        {
            foreach (var library in Options.Libraries)
            {
                if (this.Symbols.Libraries.Any(l => l.FileName == library))
                    continue;

                var parser = new ClangParser();
                parser.LibraryParsed += OnFileParsed;

                var res = parser.ParseLibrary(library, Options);

                if (res.Kind != ParserResultKind.Success)
                    continue;

                Symbols.Libraries.Add(ClangParser.ConvertLibrary(res.Library));
            }

            return true;
        }

        public void SetupPasses(ILibrary library)
        { 
            TranslationUnitPasses.AddPass(new CleanUnitPass(Options));
            TranslationUnitPasses.AddPass(new SortDeclarationsPass());
            TranslationUnitPasses.AddPass(new ResolveIncompleteDeclsPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());

            if (Options.IsCSharpGenerator && Options.GenerateInlines)
                TranslationUnitPasses.AddPass(new GenerateInlinesCodePass());

            library.SetupPasses(this);

            TranslationUnitPasses.AddPass(new FindSymbolsPass());
            TranslationUnitPasses.AddPass(new CheckStaticClass());
            TranslationUnitPasses.AddPass(new MoveOperatorToClassPass());
            TranslationUnitPasses.AddPass(new MoveFunctionToClassPass());
            TranslationUnitPasses.AddPass(new GenerateAnonymousDelegatesPass());

            if (Options.GenerateConversionOperators)
                TranslationUnitPasses.AddPass(new ConstructorToConversionOperatorPass());

            TranslationUnitPasses.AddPass(new MarshalPrimitivePointersAsRefTypePass());
            TranslationUnitPasses.AddPass(new CheckAmbiguousFunctions());
            TranslationUnitPasses.AddPass(new CheckOperatorsOverloadsPass());
            TranslationUnitPasses.AddPass(new CheckVirtualOverrideReturnCovariance());

            Generator.SetupPasses();

            TranslationUnitPasses.AddPass(new FieldToPropertyPass());
            TranslationUnitPasses.AddPass(new CleanInvalidDeclNamesPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());
            TranslationUnitPasses.AddPass(new CheckFlagEnumsPass());
            TranslationUnitPasses.AddPass(new CheckDuplicatedNamesPass());
            TranslationUnitPasses.AddPass(new RenameRootNamespacesPass());
            if (Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new GenerateAbstractImplementationsPass());
                if (Options.GenerateDefaultValuesForArguments)
                {
                    TranslationUnitPasses.AddPass(new HandleDefaultParamValuesPass());
                    TranslationUnitPasses.AddPass(new FixDefaultParamValuesOfOverridesPass());
                }
            }

            if (Options.GenerateInterfacesForMultipleInheritance)
            {
                TranslationUnitPasses.AddPass(new MultipleInheritancePass());
                TranslationUnitPasses.AddPass(new ParamTypeToInterfacePass());
            }

            TranslationUnitPasses.AddPass(new CheckVTableComponentsPass());

            if (Options.GenerateProperties)
                TranslationUnitPasses.AddPass(new GetterSetterToPropertyPass());

            if (Options.GeneratePropertiesAdvanced)
                TranslationUnitPasses.AddPass(new GetterSetterToPropertyAdvancedPass());
        }

        public void ProcessCode()
        {
            TranslationUnitPasses.RunPasses(pass =>
                {
                    Diagnostics.Debug("Pass '{0}'", pass);

                    Diagnostics.PushIndent(4);
                    pass.VisitLibrary(ASTContext);
                    Diagnostics.PopIndent();
                });
            Generator.Process();
        }

        public List<GeneratorOutput> GenerateCode()
        {
            return Generator.Generate();
        }

        public void WriteCode(List<GeneratorOutput> outputs)
        {
            var outputPath = Path.GetFullPath(Options.OutputDir);

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            foreach (var output in outputs.Where(o => o.TranslationUnit.IsValid))
            {
                var fileBase = output.TranslationUnit.FileNameWithoutExtension;

                if (Options.UseHeaderDirectories)
                {
                    var dir = Path.Combine(outputPath, output.TranslationUnit.FileRelativeDirectory);
                    Directory.CreateDirectory(dir);
                    fileBase = Path.Combine(output.TranslationUnit.FileRelativeDirectory, fileBase);
                }

                if (Options.GenerateName != null)
                    fileBase = Options.GenerateName(output.TranslationUnit);

                foreach (var template in output.Templates)
                {
                    var fileRelativePath = string.Format("{0}.{1}", fileBase, template.FileExtension);
                    Diagnostics.Message("Generated '{0}'", fileRelativePath);

                    var file = Path.Combine(outputPath, fileRelativePath);
                    File.WriteAllText(file, template.Generate());
                    Options.CodeFiles.Add(file);
                }
            }
        }

        public void CompileCode()
        {
            if (!Options.CompileCode)
                return;

            var assemblyFile = string.IsNullOrEmpty(Options.LibraryName) ?
                "out.dll" : Options.LibraryName + ".dll";

            var docFile = Path.ChangeExtension(Path.GetFileName(assemblyFile), ".xml");

            var compilerOptions = new StringBuilder();
            compilerOptions.Append(" /doc:" + docFile);
            compilerOptions.Append(" /debug:pdbonly");
            compilerOptions.Append(" /unsafe");

            var compilerParameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    TreatWarningsAsErrors = false,
                    OutputAssembly = assemblyFile,
                    GenerateInMemory = false,
                    CompilerOptions = compilerOptions.ToString()
                };

            // add a reference to System.Core
            compilerParameters.ReferencedAssemblies.Add(typeof(Enumerable).Assembly.Location);

            var location = Assembly.GetExecutingAssembly().Location;
            var outputDir = Path.GetDirectoryName(location);
            var locationRuntime = Path.Combine(outputDir, "CppSharp.Runtime.dll");
            compilerParameters.ReferencedAssemblies.Add(locationRuntime);

            compilerParameters.ReferencedAssemblies.AddRange(Symbols.Libraries.SelectMany(
                lib => lib.Dependencies.Where(
                    d => libraryMappings.ContainsKey(d) &&
                         !compilerParameters.ReferencedAssemblies.Contains(libraryMappings[d]))
                    .Select(l => libraryMappings[l])).ToArray());

            var codeProvider = new CSharpCodeProvider(
                new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var compilerResults = codeProvider.CompileAssemblyFromFile(
                compilerParameters, Options.CodeFiles.ToArray());

            var errors = compilerResults.Errors.Cast<CompilerError>();
            foreach (var error in errors.Where(error => !error.IsWarning))
                Diagnostics.Error(error.ToString());

            if (compilerResults.Errors.Count == 0)
            {
                var wrapper = Path.Combine(outputDir, assemblyFile);
                foreach (var library in Options.Libraries)
                    libraryMappings[library] = wrapper;
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

    public static class ConsoleDriver
    {
        public static void Run(ILibrary library)
        {
            var options = new DriverOptions();

            var Log = new TextDiagnosticPrinter();
            var driver = new Driver(options, Log);

            library.Setup(driver);
            driver.Setup();

            if(driver.Options.Verbose)
                Log.Level = DiagnosticKind.Debug;

            if (!options.Quiet)
                Log.Message("Parsing libraries...");

            if (!driver.ParseLibraries())
                return;

            if (!options.Quiet)
                Log.Message("Indexing library symbols...");

            driver.Symbols.IndexSymbols();

            if (!options.Quiet)
                Log.Message("Parsing code...");

            driver.BuildParseOptions();

            if (!driver.ParseCode())
                return;

            if (!options.Quiet)
                Log.Message("Processing code...");

            library.Preprocess(driver, driver.ASTContext);

            driver.SetupPasses(library);

            driver.ProcessCode();
            library.Postprocess(driver, driver.ASTContext);

            if (!options.Quiet)
                Log.Message("Generating code...");

            var outputs = driver.GenerateCode();

            foreach (var output in outputs)
            {
                foreach (var pass in driver.GeneratorOutputPasses.Passes)
                {
                    pass.Driver = driver;
                    pass.VisitGeneratorOutput(output);
                }
            }

            if (!driver.Options.DryRun)
                driver.WriteCode(outputs);

            if (driver.Options.IsCSharpGenerator)
                driver.CompileCode();
        }
    }
}