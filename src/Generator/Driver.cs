using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;
using CppSharp.Generators.Cpp;
using CppSharp.Generators.CSharp;
using CppSharp.Generators.Emscripten;
using CppSharp.Generators.TS;
using CppSharp.Parser;
using CppSharp.Passes;
using CppSharp.Utils;
using CppSharp.Types;

namespace CppSharp
{
    public class Driver : IDisposable
    {
        public DriverOptions Options { get; }
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
                case GeneratorKind.C:
                    return new CGenerator(Context);
                case GeneratorKind.CPlusPlus:
                    return new CppGenerator(Context);
                case GeneratorKind.CLI:
                    return new CLIGenerator(Context);
                case GeneratorKind.CSharp:
                    return new CSharpGenerator(Context);
                case GeneratorKind.Emscripten:
                    return new EmscriptenGenerator(Context);
                case GeneratorKind.QuickJS:
                    return new QuickJSGenerator(Context);
                case GeneratorKind.NAPI:
                    return new NAPIGenerator(Context);
                case GeneratorKind.TypeScript:
                    return new TSGenerator(Context);
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

                for (int i = 0; i < module.IncludeDirs.Count; i++)
                {
                    var dir = new DirectoryInfo(module.IncludeDirs[i]);
                    module.IncludeDirs[i] = dir.FullName;
                }
            }

            if (Options.NoGenIncludeDirs != null)
                foreach (var incDir in Options.NoGenIncludeDirs)
                    ParserOptions.AddIncludeDirs(incDir);
        }

        public void Setup()
        {
            ValidateOptions();
            ParserOptions.Setup(Platform.Host);
            Context = new BindingContext(Options, ParserOptions);
            Context.LinkerOptions.Setup(ParserOptions.TargetTriple, ParserOptions.LanguageVersion);
            Generator = CreateGeneratorFromKind(Options.GeneratorKind);
        }

        public void SetupTypeMaps() =>
            Context.TypeMaps = new TypeMapDatabase(Context);

        public void SetupDeclMaps() =>
            Context.DeclMaps = new DeclMapDatabase(Context);

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
                    if (!Options.Quiet)
                        Diagnostics.Message("Parsed '{0}'", string.Join(", ", files));
                    break;
                case ParserResultKind.Error:
                    Diagnostics.Error("Error parsing '{0}'", string.Join(", ", files));
                    hasParsingErrors = true;
                    break;
                case ParserResultKind.FileNotFound:
                    Diagnostics.Error("File{0} not found: '{1}'",
                        (files.Count() > 1) ? "s" : "", string.Join(",", files));
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
            ClangParser.SourcesParsed += OnSourceFileParsed;

            var sourceFiles = Options.Modules.SelectMany(m => m.Headers);

            ParserOptions.BuildForSourceFile(Options.Modules);
            using (ParserResult result = ClangParser.ParseSourceFiles(
                sourceFiles, ParserOptions))
                Context.TargetInfo = result.TargetInfo;

            Context.ASTContext = ClangParser.ConvertASTContext(ParserOptions.ASTContext);

            ClangParser.SourcesParsed -= OnSourceFileParsed;

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
            ClangParser.LibraryParsed += OnFileParsed;
            foreach (var module in Options.Modules)
            {
                using var linkerOptions = new LinkerOptions(Context.LinkerOptions);
                foreach (var libraryDir in module.LibraryDirs)
                    linkerOptions.AddLibraryDirs(libraryDir);

                foreach (var library in module.Libraries.Where(library =>
                             Context.Symbols.Libraries.All(l => l.FileName != library)))
                {
                    linkerOptions.AddLibraries(library);
                }

                using var res = ClangParser.ParseLibrary(linkerOptions);
                if (res.Kind != ParserResultKind.Success)
                    continue;

                for (uint i = 0; i < res.LibrariesCount; i++)
                    Context.Symbols.Libraries.Add(ClangParser.ConvertLibrary(res.GetLibraries(i)));
            }
            ClangParser.LibraryParsed -= OnFileParsed;

            Context.Symbols.IndexSymbols();
            SortModulesByDependencies();

            return true;
        }

        public void SetupPasses(ILibrary library)
        {
            var passes = Context.TranslationUnitPasses;

            passes.AddPass(new ResolveIncompleteDeclsPass());
            passes.AddPass(new IgnoreSystemDeclarationsPass());
            passes.AddPass(new MatchParamNamesWithInstantiatedFromPass());

            if (Options.IsCSharpGenerator)
                passes.AddPass(new EqualiseAccessOfOverrideAndBasePass());

            passes.AddPass(new FlattenAnonymousTypesToFields());
            passes.AddPass(new CheckIgnoredDeclsPass());
            passes.AddPass(new MarkUsedClassInternalsPass());

            if (Options.IsCSharpGenerator)
            {
                passes.AddPass(new TrimSpecializationsPass());
                passes.AddPass(new CheckAmbiguousFunctions());
                passes.AddPass(new GenerateSymbolsPass());
                passes.AddPass(new CheckIgnoredDeclsPass());
            }

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                passes.AddPass(new MoveFunctionToClassPass());
                passes.AddPass(new ValidateOperatorsPass());
            }

            library.SetupPasses(this);

            passes.AddPass(new FindSymbolsPass());
            passes.AddPass(new CheckMacroPass());
            passes.AddPass(new CheckStaticClass());

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator || Options.IsCppGenerator)
            {
                passes.AddPass(new CheckAmbiguousFunctions());
            }

            passes.AddPass(new ConstructorToConversionOperatorPass());
            passes.AddPass(new MarshalPrimitivePointersAsRefTypePass());

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                passes.AddPass(new CheckOperatorsOverloadsPass());
            }

            passes.AddPass(new CheckVirtualOverrideReturnCovariance());
            passes.AddPass(new CleanCommentsPass());

            Generator.SetupPasses();

            passes.AddPass(new CleanInvalidDeclNamesPass());
            passes.AddPass(new FastDelegateToDelegatesPass());
            passes.AddPass(new FieldToPropertyPass());
            passes.AddPass(new CheckIgnoredDeclsPass());
            passes.AddPass(new CheckFlagEnumsPass());
            passes.AddPass(new MakeProtectedNestedTypesPublicPass());

            if (Options.IsCSharpGenerator)
            {
                passes.AddPass(new GenerateAbstractImplementationsPass());
                passes.AddPass(new MultipleInheritancePass());
            }

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                passes.AddPass(new DelegatesPass());
            }

            if (Options.GeneratorKind != GeneratorKind.C)
            {
                passes.AddPass(new GetterSetterToPropertyPass());
            }

            passes.AddPass(new StripUnusedSystemTypesPass());

            if (Options.IsCSharpGenerator)
            {
                passes.AddPass(new SpecializationMethodsWithDependentPointersPass());
                passes.AddPass(new ParamTypeToInterfacePass());
            }

            passes.AddPass(new CheckDuplicatedNamesPass());

            if (Options.IsCLIGenerator || Options.IsCSharpGenerator)
            {
                passes.RenameDeclsUpperCase(RenameTargets.Any & ~RenameTargets.Parameter);
                passes.AddPass(new CheckKeywordNamesPass());
            }

            passes.AddPass(new HandleVariableInitializerPass());

            passes.AddPass(new MarkEventsWithUniqueIdPass());
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
                    WriteGeneratedCodeToFile(file, template.Generate());

                    output.TranslationUnit.Module?.CodeFiles.Add(file);

                    if (!Options.Quiet)
                        Diagnostics.Message("Generated '{0}'", fileRelativePath);
                }
            }
        }

        private void WriteGeneratedCodeToFile(string file, string generatedCode)
        {
            var fi = new FileInfo(file);

            if (fi.Directory != null && !fi.Directory.Exists)
                fi.Directory.Create();

            if (!fi.Exists || fi.Length != generatedCode.Length ||
                File.ReadAllText(file) != generatedCode)
                File.WriteAllText(file, generatedCode);
        }

        public bool CompileCode(Module module)
        {
            var msBuildGenerator = new MSBuildGenerator(Context, module, LibraryMappings);
            msBuildGenerator.Process();
            string csproj = Path.Combine(Options.OutputDir,
                $"{module.LibraryName}.{msBuildGenerator.FileExtension}");
            File.WriteAllText(csproj, msBuildGenerator.Generate());

            string output = ProcessHelper.Run("dotnet", $"build \"{csproj}\"",
                out int error, out string errorMessage);
            if (error == 0)
            {
                Diagnostics.Message($@"Compilation succeeded: {
                    LibraryMappings[module] = Path.Combine(
                        Options.OutputDir, $"{module.LibraryName}.dll")}.");
                return true;
            }

            Diagnostics.Error(output);
            Diagnostics.Error(errorMessage);
            return false;
        }

        public void AddTranslationUnitPass(TranslationUnitPass pass)
        {
            Context.TranslationUnitPasses.AddPass(pass);
        }

        public void AddGeneratorOutputPass(GeneratorOutputPass pass)
        {
            Context.GeneratorOutputPasses.AddPass(pass);
        }

        public void Dispose()
        {
            Generator?.Dispose();
            Context?.TargetInfo?.Dispose();
            ParserOptions.Dispose();
            Context?.LinkerOptions.Dispose();
        }

        private bool hasParsingErrors;
        private static readonly Dictionary<Module, string> LibraryMappings = new();
    }

    public static class ConsoleDriver
    {
        public static void Run(ILibrary library)
        {
            var options = new DriverOptions();
            using var driver = new Driver(options);
            library.Setup(driver);

            driver.Setup();

            if (driver.Options.Verbose)
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
            foreach (var module in options.Modules.Where(
                         m => m != options.SystemModule && !m.Units.GetGenerated().Any()))
            {
                Diagnostics.Message($"Removing module {module} because no translation units are generated...");
                options.Modules.Remove(module);
            }

            if (!options.Quiet)
                Diagnostics.Message("Processing code...");

            driver.SetupPasses(library);
            driver.SetupTypeMaps();
            driver.SetupDeclMaps();

            library.Preprocess(driver, driver.Context.ASTContext);

            driver.ProcessCode();
            library.Postprocess(driver, driver.Context.ASTContext);

            if (!options.Quiet)
                Diagnostics.Message("Generating code...");

            if (options.DryRun)
                return;

            var outputs = driver.GenerateCode();

            library.GenerateCode(driver, outputs);

            foreach (var output in outputs)
            {
                foreach (var pass in driver.Context.GeneratorOutputPasses.Passes)
                    pass.VisitGeneratorOutput(output);
            }

            driver.SaveCode(outputs);
            if (driver.Options.IsCSharpGenerator && driver.Options.CompileCode)
                driver.Options.Modules.Any(m => !driver.CompileCode(m));
        }
    }
}
