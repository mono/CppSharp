using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using System.Collections.Generic;
using System.IO;
using Microsoft.CSharp;

#if !OLD_PARSER
using Std;
#endif

namespace CppSharp
{
    public class Driver
    {
        public DriverOptions Options { get; private set; }
        public IDiagnosticConsumer Diagnostics { get; private set; }
        public Project Project { get; private set; }

        public TypeMapDatabase TypeDatabase { get; private set; }
        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; private set; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; private set; }
        public Generator Generator { get; private set; }

        public ASTContext ASTContext { get; private set; }
        public SymbolContext Symbols { get; private set; }

        public Driver(DriverOptions options, IDiagnosticConsumer diagnostics)
        {
            Options = options;
            Diagnostics = diagnostics;
            Project = new Project();
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
            TypeDatabase.SetupTypeMaps();
            Generator = CreateGeneratorFromKind(Options.GeneratorKind);
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

        ParserOptions BuildParseOptions(SourceFile file)
        {
            var options = new ParserOptions
            {
                FileName = file.Path,
#if !OLD_PARSER
                IncludeDirs = Options.IncludeDirs.ToStd(),
                SystemIncludeDirs = Options.SystemIncludeDirs.ToStd(),
                Defines = Options.Defines.ToStd(),
                LibraryDirs = Options.LibraryDirs.ToStd(),
#else
                IncludeDirs = Options.IncludeDirs,
                SystemIncludeDirs = Options.SystemIncludeDirs,
                Defines = Options.Defines,
                LibraryDirs = Options.LibraryDirs,
#endif
                Abi = Options.Abi,
                ToolSetToUse = Options.ToolSetToUse,
                TargetTriple = Options.TargetTriple,
                NoStandardIncludes = Options.NoStandardIncludes,
                NoBuiltinIncludes = Options.NoBuiltinIncludes,
                MicrosoftMode = Options.MicrosoftMode,
                Verbose = Options.Verbose,
            };

            return options;
        }

        public bool ParseCode()
        {
            foreach (var header in Options.Headers)
            {
                var source = Project.AddFile(header);
                source.Options = BuildParseOptions(source);
            }

            var parser = new ClangParser();
            parser.SourceParsed += OnSourceFileParsed;
            parser.LibraryParsed += OnFileParsed;

            parser.ParseProject(Project, Options);

#if !OLD_PARSER
            ASTContext = ClangParser.ConvertASTContext(parser.ASTContext);
#else
            ASTContext = parser.ASTContext;
#endif

            return true;
        }

        public bool ParseLibraries()
        {
            foreach (var library in Options.Libraries)
            {
                var parser = new ClangParser();
                var res = parser.ParseLibrary(library, Options);

                if (res.Kind != ParserResultKind.Success)
                    continue;

#if !OLD_PARSER
                Symbols.Libraries.Add(ClangParser.ConvertLibrary(res.Library));
#else
                Symbols.Libraries.Add(res.Library);
#endif
            }

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

            Generator.SetupPasses();
            TranslationUnitPasses.AddPass(new FieldToPropertyPass());
            TranslationUnitPasses.AddPass(new CleanInvalidDeclNamesPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());
            TranslationUnitPasses.AddPass(new CheckFlagEnumsPass());
            TranslationUnitPasses.AddPass(new CheckDuplicatedNamesPass());
            if (Options.GenerateAbstractImpls)
                TranslationUnitPasses.AddPass(new GenerateAbstractImplementationsPass());
            if (Options.GenerateInterfacesForMultipleInheritance)
            {
                TranslationUnitPasses.AddPass(new MultipleInheritancePass());
                TranslationUnitPasses.AddPass(new ParamTypeToInterfacePass());
            }
            if (Options.GenerateVirtualTables)
                TranslationUnitPasses.AddPass(new CheckVTableComponentsPass());
            if (Options.GenerateProperties)
                TranslationUnitPasses.AddPass(new GetterSetterToPropertyAdvancedPass());
        }

        public void ProcessCode()
        {
            TranslationUnitPasses.RunPasses(pass => pass.VisitLibrary(ASTContext));
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
                    string file = Path.GetFullPath(filePath);
                    File.WriteAllText(file, template.Generate());
                    Options.CodeFiles.Add(file);
                }
            }
        }

        public void CompileCode()
        {
            string assemblyFile;
            if (string.IsNullOrEmpty(Options.LibraryName))
                assemblyFile = "out.dll";
            else
                assemblyFile = Options.LibraryName + ".dll";

            var compilerOptions = new StringBuilder();
            compilerOptions.Append(" /doc:" + Path.ChangeExtension(Path.GetFileName(assemblyFile), ".xml"));
            compilerOptions.Append(" /debug:pdbonly");
            compilerOptions.Append(" /unsafe");

            var compilerParameters = new CompilerParameters();
            compilerParameters.GenerateExecutable = false;
            compilerParameters.TreatWarningsAsErrors = false;
            compilerParameters.OutputAssembly = assemblyFile;
            compilerParameters.GenerateInMemory = false;
            compilerParameters.CompilerOptions = compilerOptions.ToString();
            compilerParameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
            var location = Assembly.GetExecutingAssembly().Location;
            var locationRuntime = Path.Combine(Path.GetDirectoryName(location), "CppSharp.Runtime.dll");
            compilerParameters.ReferencedAssemblies.Add(locationRuntime);

            var providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v4.0");
            var csharp = new CSharpCodeProvider(providerOptions);
            var cr = csharp.CompileAssemblyFromFile(compilerParameters, Options.CodeFiles.ToArray());

            foreach (var error in cr.Errors.Cast<CompilerError>().Where(error => !error.IsWarning))
                Diagnostics.EmitError(error.ToString());
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
            var driver = new Driver(options, new TextDiagnosticPrinter());
            library.Setup(driver);
            driver.Setup();
            var Log = driver.Diagnostics;

            if (!options.Quiet)
                Log.EmitMessage("Parsing libraries...");

            if (!driver.ParseLibraries())
                return;

            if (!options.Quiet)
                Log.EmitMessage("Indexing library symbols...");

            driver.Symbols.IndexSymbols();

            if (!options.Quiet)
                Log.EmitMessage("Parsing code...");

            if (!driver.ParseCode())
                return;

            if (!options.Quiet)
                Log.EmitMessage("Processing code...");

            library.Preprocess(driver, driver.ASTContext);

            driver.SetupPasses(library);

            driver.ProcessCode();
            library.Postprocess(driver, driver.ASTContext);

            if (!options.Quiet)
                Log.EmitMessage("Generating code...");

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
            if (driver.Options.IsCSharpGenerator)
                driver.CompileCode();
        }
    }
}