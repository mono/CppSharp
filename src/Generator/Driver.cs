using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.CSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Parser;
using CppSharp.Utils;

namespace CppSharp
{
    public class Driver
    {
        public IDiagnostics Diagnostics { get; private set; }
        public DriverOptions Options { get; private set; }
        public ParserOptions ParserOptions { get; set; }
        public Project Project { get; private set; }
        public BindingContext Context { get; private set; }
        public Generator Generator { get; private set; }

        public bool HasCompilationErrors { get; set; }

        public Driver(DriverOptions options, IDiagnostics diagnostics)
        {
            Options = options;
            Diagnostics = diagnostics;
            Project = new Project();
            ParserOptions = new ParserOptions();
        }

        Generator CreateGeneratorFromKind(GeneratorKind kind)
        {
            switch (kind)
            {
                case GeneratorKind.CLI:
                    return new CLIGenerator(Context);
                case GeneratorKind.CSharp:
                    return new CSharpGenerator(Context);
            }

            return null;
        }

        void ValidateOptions()
        {
            foreach (var module in Options.Modules)
            {
                if (string.IsNullOrWhiteSpace(module.LibraryName))
                    throw new InvalidOptionException("One of your modules has no library name.");

                if (module.OutputNamespace == null)
                    module.OutputNamespace = module.LibraryName;
            }

            if (Options.NoGenIncludeDirs != null)
                foreach (var incDir in Options.NoGenIncludeDirs)
                    ParserOptions.addIncludeDirs(incDir);
        }

        public void Setup()
        {
            ValidateOptions();
            ParserOptions.SetupIncludes();
            Context = new BindingContext(Diagnostics, Options, ParserOptions);
            Generator = CreateGeneratorFromKind(Options.GeneratorKind);
        }

        void OnSourceFileParsed(IList<SourceFile> files, ParserResult result)
        {
            OnFileParsed(files.Select(f => f.Path), result);
        }

        void OnFileParsed(string file, ParserResult result)
        {
            OnFileParsed(new[] { file }, result);
        }

        void OnFileParsed(IEnumerable<string> files, ParserResult result)
        {
            switch (result.Kind)
            {
                case ParserResultKind.Success:
                    Diagnostics.Message("Parsed '{0}'", string.Join(", ", files));
                    break;
                case ParserResultKind.Error:
                    Diagnostics.Error("Error parsing '{0}'", string.Join(", ", files));
                    hasParsingErrors = true;
                    break;
                case ParserResultKind.FileNotFound:
                    Diagnostics.Error("A file from '{0}' was not found", string.Join(",", files));
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

        public ParserOptions BuildParserOptions(SourceFile file = null)
        {
            var options = new ParserOptions
            {
                Abi = ParserOptions.Abi,
                ToolSetToUse = ParserOptions.ToolSetToUse,
                TargetTriple = ParserOptions.TargetTriple,
                NoStandardIncludes = ParserOptions.NoStandardIncludes,
                NoBuiltinIncludes = ParserOptions.NoBuiltinIncludes,
                MicrosoftMode = ParserOptions.MicrosoftMode,
                Verbose = ParserOptions.Verbose,
                LanguageVersion = ParserOptions.LanguageVersion
            };

            // This eventually gets passed to Clang's MSCompatibilityVersion, which
            // is in turn used to derive the value of the built-in define _MSC_VER.
            // It used to receive a 4-digit based identifier but now expects a full
            // version MSVC digit, so check if we still have the old version and 
            // convert to the right format.

            if (ParserOptions.ToolSetToUse.ToString(CultureInfo.InvariantCulture).Length == 4)
                ParserOptions.ToolSetToUse *= 100000;

            for (uint i = 0; i < ParserOptions.ArgumentsCount; ++i)
            {
                var arg = ParserOptions.getArguments(i);
                options.addArguments(arg);
            }

            for (uint i = 0; i < ParserOptions.IncludeDirsCount; ++i)
            {
                var include = ParserOptions.getIncludeDirs(i);
                options.addIncludeDirs(include);
            }

            for (uint i = 0; i < ParserOptions.SystemIncludeDirsCount; ++i)
            {
                var include = ParserOptions.getSystemIncludeDirs(i);
                options.addSystemIncludeDirs(include);
            }

            for (uint i = 0; i < ParserOptions.DefinesCount; ++i)
            {
                var define = ParserOptions.getDefines(i);
                options.addDefines(define);
            }

            for (uint i = 0; i < ParserOptions.UndefinesCount; ++i)
            {
                var define = ParserOptions.getUndefines(i);
                options.addUndefines(define);
            }

            for (uint i = 0; i < ParserOptions.LibraryDirsCount; ++i)
            {
                var lib = ParserOptions.getLibraryDirs(i);
                options.addLibraryDirs(lib);
            }

            foreach (var module in Options.Modules.Where(
                m => file == null || m.Headers.Contains(file.Path)))
            {
                foreach (var include in module.IncludeDirs)
                    options.addIncludeDirs(include);

                foreach (var define in module.Defines)
                    options.addDefines(define);

                foreach (var undefine in module.Undefines)
                    options.addUndefines(undefine);

                foreach (var libraryDir in module.LibraryDirs)
                    options.addLibraryDirs(libraryDir);
            }

            return options;
        }

        public bool ParseCode()
        {
            var parser = new ClangParser(new Parser.AST.ASTContext());

            parser.SourcesParsed += OnSourceFileParsed;
            parser.ParseProject(Project, Options.UnityBuild);
           
            Context.TargetInfo = parser.GetTargetInfo(ParserOptions);
            Context.ASTContext = ClangParser.ConvertASTContext(parser.ASTContext);

            return !hasParsingErrors;
        }

        public void BuildParseOptions()
        {
            foreach (var header in Options.Modules.SelectMany(m => m.Headers))
            {
                var source = Project.AddFile(header);
                if (!Options.UnityBuild)
                    source.Options = BuildParserOptions(source);
            }
            if (Options.UnityBuild)
                Project.Sources[0].Options = BuildParserOptions();
        }

        public void SortModulesByDependencies()
        {
            if (Options.Modules.All(m => m.Libraries.Any() || m == Options.SystemModule))
            {
                var sortedModules = Options.Modules.TopologicalSort(m =>
                {
                    return from library in Context.Symbols.Libraries
                           where m.Libraries.Contains(library.FileName)
                           from module in Options.Modules
                           where library.Dependencies.Intersect(module.Libraries).Any()
                           select module;
                });
                Options.Modules.Clear();
                Options.Modules.AddRange(sortedModules);
            }
        }

        public bool ParseLibraries()
        {
            foreach (var module in Options.Modules)
            {
                foreach (var libraryDir in module.LibraryDirs)
                    ParserOptions.addLibraryDirs(libraryDir);

                foreach (var library in module.Libraries)
                {
                    if (Context.Symbols.Libraries.Any(l => l.FileName == library))
                        continue;

                    var parser = new ClangParser();
                    parser.LibraryParsed += OnFileParsed;

                    using (var res = parser.ParseLibrary(library, ParserOptions))
                    {
                        if (res.Kind != ParserResultKind.Success)
                            continue;

                        Context.Symbols.Libraries.Add(ClangParser.ConvertLibrary(res.Library));

                        res.Library.Dispose();
                    }
                }
            }

            Context.Symbols.IndexSymbols();
            SortModulesByDependencies();

            return true;
        }

        public void SetupPasses(ILibrary library)
        { 
            var TranslationUnitPasses = Context.TranslationUnitPasses;

            TranslationUnitPasses.AddPass(new SortDeclarationsPass());
            TranslationUnitPasses.AddPass(new ResolveIncompleteDeclsPass());
            if (Options.IsCSharpGenerator)
                TranslationUnitPasses.AddPass(new MarkSupportedSpecializationsPass());
            TranslationUnitPasses.AddPass(new IgnoreSystemDeclarationsPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());

            if (Options.IsCSharpGenerator)
            {
                if (Options.GenerateInlines)
                    TranslationUnitPasses.AddPass(new GenerateInlinesCodePass());
                TranslationUnitPasses.AddPass(new TrimSpecializationsPass());
                TranslationUnitPasses.AddPass(new GenerateTemplatesCodePass());
            }

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
            if (Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new GenerateAbstractImplementationsPass());
                if (Options.GenerateDefaultValuesForArguments)
                {
                    TranslationUnitPasses.AddPass(new FixDefaultParamValuesOfOverridesPass());
                    TranslationUnitPasses.AddPass(new HandleDefaultParamValuesPass());
                }
            }

            if (Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new MultipleInheritancePass());
                TranslationUnitPasses.AddPass(new ParamTypeToInterfacePass());
            }

            TranslationUnitPasses.AddPass(new CheckVTableComponentsPass());

            if (Options.IsCSharpGenerator)
                TranslationUnitPasses.AddPass(new DelegatesPass());

            if (Options.GenerateProperties)
                TranslationUnitPasses.AddPass(new GetterSetterToPropertyPass());

            if (Options.GeneratePropertiesAdvanced)
                TranslationUnitPasses.AddPass(new GetterSetterToPropertyAdvancedPass());
        }

        public void ProcessCode()
        {
            Context.RunPasses();
            Generator.Process();
        }

        public List<GeneratorOutput> GenerateCode()
        {
            return Generator.Generate();
        }

        public void SaveCode(IEnumerable<GeneratorOutput> outputs)
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

                    var file = Path.Combine(outputPath, fileRelativePath);
                    File.WriteAllText(file, template.Generate());
                    output.TranslationUnit.Module.CodeFiles.Add(file);

                    Diagnostics.Message("Generated '{0}'", fileRelativePath);
                }
            }
        }

        private static readonly Dictionary<string, string> libraryMappings = new Dictionary<string, string>();

        public void CompileCode(AST.Module module)
        {
            var assemblyFile = string.IsNullOrEmpty(module.LibraryName) ?
                "out.dll" : module.LibraryName + ".dll";

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

            if (module != Options.SystemModule)
                compilerParameters.ReferencedAssemblies.Add(
                    string.Format("{0}.dll", Options.SystemModule.LibraryName));
            // add a reference to System.Core
            compilerParameters.ReferencedAssemblies.Add(typeof(Enumerable).Assembly.Location);

            var location = Assembly.GetExecutingAssembly().Location;
            var outputDir = Path.GetDirectoryName(location);
            var locationRuntime = Path.Combine(outputDir, "CppSharp.Runtime.dll");
            compilerParameters.ReferencedAssemblies.Add(locationRuntime);

            compilerParameters.ReferencedAssemblies.AddRange(Context.Symbols.Libraries.SelectMany(
                lib => lib.Dependencies.Where(
                    d => libraryMappings.ContainsKey(d) &&
                         !compilerParameters.ReferencedAssemblies.Contains(libraryMappings[d]))
                    .Select(l => libraryMappings[l])).ToArray());

            Diagnostics.Message("Compiling {0}...", module.LibraryName);
            CompilerResults compilerResults;
            using (var codeProvider = new CSharpCodeProvider(
                new Dictionary<string, string> {
                    { "CompilerDirectoryPath", ManagedToolchain.FindCSharpCompilerDir() } }))
            {
                compilerResults = codeProvider.CompileAssemblyFromFile(
                    compilerParameters, module.CodeFiles.ToArray());
            }

            var errors = compilerResults.Errors.Cast<CompilerError>().Where(e => !e.IsWarning &&
                // HACK: auto-compiling on OS X produces "errors" which do not affect compilation so we ignore them
                (!Platform.IsMacOS || !e.ErrorText.EndsWith("(Location of the symbol related to previous warning)", StringComparison.Ordinal))).ToList();
            foreach (var error in errors)
                Diagnostics.Error(error.ToString());

            HasCompilationErrors = errors.Count > 0;
            if (!HasCompilationErrors)
            {
                Diagnostics.Message("Compilation succeeded.");
                var wrapper = Path.Combine(outputDir, assemblyFile);
                foreach (var library in module.Libraries)
                    libraryMappings[library] = wrapper;
            }
        }

        public void AddTranslationUnitPass(TranslationUnitPass pass)
        {
            Context.TranslationUnitPasses.AddPass(pass);
        }

        public void AddGeneratorOutputPass(GeneratorOutputPass pass)
        {
            Context.GeneratorOutputPasses.AddPass(pass);
        }

        private bool hasParsingErrors;
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

            if(driver.ParserOptions.Verbose)
                Log.Level = DiagnosticKind.Debug;

            if (!options.Quiet)
                Log.Message("Parsing libraries...");

            if (!driver.ParseLibraries())
                return;

            if (!options.Quiet)
                Log.Message("Parsing code...");

            driver.BuildParseOptions();

            if (!driver.ParseCode())
            {
                Log.Error("CppSharp has encountered an error while parsing code.");
                return;
            }

            new CleanUnitPass { Context = driver.Context }.VisitASTContext(driver.Context.ASTContext);
            options.Modules.RemoveAll(m => m != options.SystemModule && !m.Units.GetGenerated().Any());

            if (!options.Quiet)
                Log.Message("Processing code...");

            library.Preprocess(driver, driver.Context.ASTContext);

            driver.SetupPasses(library);

            driver.ProcessCode();
            library.Postprocess(driver, driver.Context.ASTContext);

            if (!options.Quiet)
                Log.Message("Generating code...");

            var outputs = driver.GenerateCode();

            foreach (var output in outputs)
            {
                foreach (var pass in driver.Context.GeneratorOutputPasses.Passes)
                {
                    pass.VisitGeneratorOutput(output);
                }
            }

            if (!driver.Options.DryRun)
            {
                driver.SaveCode(outputs);
                if (driver.Options.IsCSharpGenerator && driver.Options.CompileCode)
                    foreach (var module in driver.Options.Modules)
                    {
                        driver.CompileCode(module);
                        if (driver.HasCompilationErrors)
                            break;
                    }
            }

            driver.Generator.Dispose();
            driver.Context.TargetInfo.Dispose();
        }
    }
}