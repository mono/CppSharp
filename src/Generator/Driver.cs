using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Parser;
using CppSharp.Passes;
using CppSharp.Utils;
using Microsoft.CSharp;
using CppSharp.Types;
using CppSharp.Generators.Cpp;

namespace CppSharp
{
    public class Driver
    {
        public DriverOptions Options { get; private set; }
        public ParserOptions ParserOptions { get; set; }
        public BindingContext Context { get; private set; }
        public Generator Generator { get; private set; }

        public bool HasCompilationErrors { get; set; }

        public Driver(DriverOptions options)
        {
            Options = options;
            ParserOptions = new ParserOptions();
        }

        Generator CreateGeneratorFromKind(GeneratorKind kind)
        {
            switch (kind)
            {
                case GeneratorKind.CPlusPlus:
                    return new CppGenerator(Context);
                case GeneratorKind.CLI:
                    return new CLIGenerator(Context);
                case GeneratorKind.CSharp:
                    return new CSharpGenerator(Context);
            }

            throw new NotImplementedException();
        }

        void ValidateOptions()
        {
            if (!Options.Compilation.Platform.HasValue)
                Options.Compilation.Platform = Platform.Host;

            foreach (var module in Options.Modules)
            {
                if (string.IsNullOrWhiteSpace(module.LibraryName))
                    throw new InvalidOptionException("One of your modules has no library name.");

                if (module.OutputNamespace == null)
                    module.OutputNamespace = module.LibraryName;
            }

            if (Options.NoGenIncludeDirs != null)
                foreach (var incDir in Options.NoGenIncludeDirs)
                    ParserOptions.AddIncludeDirs(incDir);
        }

        public void Setup()
        {
            ValidateOptions();
            ParserOptions.Setup();
            Context = new BindingContext(Options, ParserOptions);
            Generator = CreateGeneratorFromKind(Options.GeneratorKind);
        }

        public void SetupTypeMaps() =>
            Context.TypeMaps = new TypeMapDatabase(Context);

        void OnSourceFileParsed(IEnumerable<string> files, ParserResult result)
        {
            OnFileParsed(files, result);
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
                    Diagnostics.Error("File{0} not found: '{1}'",
                        (files.Count() > 1) ? "s" : "",  string.Join(",", files));
                    hasParsingErrors = true;
                    break;
            }

            for (uint i = 0; i < result.DiagnosticsCount; ++i)
            {
                var diag = result.GetDiagnostics(i);

                if (diag.Level == ParserDiagnosticLevel.Warning &&
                    !Options.Verbose)
                    continue;

                if (diag.Level == ParserDiagnosticLevel.Note)
                    continue;

                Diagnostics.Message("{0}({1},{2}): {3}: {4}",
                    diag.FileName, diag.LineNumber, diag.ColumnNumber,
                    diag.Level.ToString().ToLower(), diag.Message);
            }
        }

        public bool ParseCode()
        {
            var astContext = new Parser.AST.ASTContext();

            var parser = new ClangParser(astContext);
            parser.SourcesParsed += OnSourceFileParsed;

            var sourceFiles = Options.Modules.SelectMany(m => m.Headers);

            using (ParserOptions parserOptions = ParserOptions.BuildForSourceFile(
                Options.Modules))
            {
                using (ParserResult result = parser.ParseSourceFiles(
                    sourceFiles, parserOptions))
                    Context.TargetInfo = result.TargetInfo;

                if (string.IsNullOrEmpty(ParserOptions.TargetTriple))
                    ParserOptions.TargetTriple = parserOptions.TargetTriple;
            }

            Context.ASTContext = ClangParser.ConvertASTContext(astContext);

            return !hasParsingErrors;
        }

        public void SortModulesByDependencies()
        {
            var sortedModules = Options.Modules.TopologicalSort(m =>
            {
                var dependencies = (from library in Context.Symbols.Libraries
                                    where m.Libraries.Contains(library.FileName)
                                    from module in Options.Modules
                                    where library.Dependencies.Intersect(module.Libraries).Any()
                                    select module).ToList();
                if (m != Options.SystemModule)
                    m.Dependencies.Add(Options.SystemModule);
                m.Dependencies.AddRange(dependencies);
                return m.Dependencies;
            });
            Options.Modules.Clear();
            Options.Modules.AddRange(sortedModules);
        }

        public bool ParseLibraries()
        {
            foreach (var module in Options.Modules)
            {
                foreach (var libraryDir in module.LibraryDirs)
                    ParserOptions.AddLibraryDirs(libraryDir);

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

            TranslationUnitPasses.AddPass(new ResolveIncompleteDeclsPass());
            TranslationUnitPasses.AddPass(new IgnoreSystemDeclarationsPass());

            if (Options.IsCSharpGenerator)
                TranslationUnitPasses.AddPass(new EqualiseAccessOfOverrideAndBasePass());

            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());

            if (Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new TrimSpecializationsPass());
                TranslationUnitPasses.AddPass(new CheckAmbiguousFunctions());
                TranslationUnitPasses.AddPass(new GenerateSymbolsPass());
                TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());
            }

            library.SetupPasses(this);

            TranslationUnitPasses.AddPass(new FindSymbolsPass());
            TranslationUnitPasses.AddPass(new CheckMacroPass());
            TranslationUnitPasses.AddPass(new CheckStaticClass());

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new MoveFunctionToClassPass());
            }

            TranslationUnitPasses.AddPass(new CheckAmbiguousFunctions());
            TranslationUnitPasses.AddPass(new ConstructorToConversionOperatorPass());
            TranslationUnitPasses.AddPass(new MarshalPrimitivePointersAsRefTypePass());

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new CheckOperatorsOverloadsPass());
            }

            TranslationUnitPasses.AddPass(new CheckVirtualOverrideReturnCovariance());
            TranslationUnitPasses.AddPass(new CleanCommentsPass());

            Generator.SetupPasses();

            TranslationUnitPasses.AddPass(new FlattenAnonymousTypesToFields());
            TranslationUnitPasses.AddPass(new CleanInvalidDeclNamesPass());
            TranslationUnitPasses.AddPass(new FieldToPropertyPass());
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());
            TranslationUnitPasses.AddPass(new CheckFlagEnumsPass());
            TranslationUnitPasses.AddPass(new MakeProtectedNestedTypesPublicPass());

            if (Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new GenerateAbstractImplementationsPass());
                TranslationUnitPasses.AddPass(new MultipleInheritancePass());
            }

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new DelegatesPass());
            }

            TranslationUnitPasses.AddPass(new GetterSetterToPropertyPass());
            TranslationUnitPasses.AddPass(new StripUnusedSystemTypesPass());

            if (Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.AddPass(new SpecializationMethodsWithDependentPointersPass());
                TranslationUnitPasses.AddPass(new ParamTypeToInterfacePass());
            }

            TranslationUnitPasses.AddPass(new CheckDuplicatedNamesPass());

            TranslationUnitPasses.AddPass(new MarkUsedClassInternalsPass());

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any & ~RenameTargets.Parameter);
                TranslationUnitPasses.AddPass(new CheckKeywordNamesPass());
            }
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

                foreach (var template in output.Outputs)
                {
                    var fileRelativePath = $"{fileBase}.{template.FileExtension}";

                    var file = Path.Combine(outputPath, fileRelativePath);
                    File.WriteAllText(file, template.Generate());
                    output.TranslationUnit.Module.CodeFiles.Add(file);

                    Diagnostics.Message("Generated '{0}'", fileRelativePath);
                }
            }
        }

        private static readonly Dictionary<Module, string> libraryMappings = new Dictionary<Module, string>();

        public void CompileCode(Module module)
        {
            var assemblyFile = Path.Combine(Options.OutputDir, module.LibraryName + ".dll");

            var docFile = Path.ChangeExtension(assemblyFile, ".xml");

            var compilerOptions = new StringBuilder();
            compilerOptions.Append($" /doc:\"{docFile}\"");
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
                    Path.Combine(Options.OutputDir, $"{Options.SystemModule.LibraryName}.dll"));
            // add a reference to System.Core
            compilerParameters.ReferencedAssemblies.Add(typeof(Enumerable).Assembly.Location);

            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var outputDir = Path.GetDirectoryName(location);
            var locationRuntime = Path.Combine(outputDir, "CppSharp.Runtime.dll");
            compilerParameters.ReferencedAssemblies.Add(locationRuntime);

            compilerParameters.ReferencedAssemblies.AddRange(
                (from dependency in module.Dependencies
                 where libraryMappings.ContainsKey(dependency)
                 select libraryMappings[dependency]).ToArray());

            compilerParameters.ReferencedAssemblies.AddRange(module.ReferencedAssemblies.ToArray());

            Diagnostics.Message($"Compiling {module.LibraryName}...");
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
                libraryMappings[module] = Path.Combine(outputDir, assemblyFile);
                Diagnostics.Message("Compilation succeeded.");
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
            var driver = new Driver(options);

            library.Setup(driver);

            driver.Setup();

            if(driver.Options.Verbose)
                Diagnostics.Level = DiagnosticKind.Debug;

            if (!options.Quiet)
                Diagnostics.Message("Parsing libraries...");

            if (!driver.ParseLibraries())
                return;

            if (!options.Quiet)
                Diagnostics.Message("Parsing code...");

            if (!driver.ParseCode())
            {
                Diagnostics.Error("CppSharp has encountered an error while parsing code.");
                return;
            }

            new CleanUnitPass { Context = driver.Context }.VisitASTContext(driver.Context.ASTContext);
            options.Modules.RemoveAll(m => m != options.SystemModule && !m.Units.GetGenerated().Any());

            if (!options.Quiet)
                Diagnostics.Message("Processing code...");

            driver.SetupPasses(library);
            driver.SetupTypeMaps();

            library.Preprocess(driver, driver.Context.ASTContext);

            driver.ProcessCode();
            library.Postprocess(driver, driver.Context.ASTContext);

            if (!options.Quiet)
                Diagnostics.Message("Generating code...");

            if (!options.DryRun)
            {
                var outputs = driver.GenerateCode();

                foreach (var output in outputs)
                {
                    foreach (var pass in driver.Context.GeneratorOutputPasses.Passes)
                    {
                        pass.VisitGeneratorOutput(output);
                    }
                }

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
            driver.ParserOptions.Dispose();
        }
    }
}
