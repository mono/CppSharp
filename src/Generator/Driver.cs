﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
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

#if !OLD_PARSER
using CppSharp.Parser;
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

#if OLD_PARSER
            for (var i = 0; i < options.IncludeDirs.Count; i++)
                options.IncludeDirs[i] = Path.GetFullPath(options.IncludeDirs[i]);

            for (var i = 0; i < options.LibraryDirs.Count; i++)
                options.LibraryDirs[i] = Path.GetFullPath(options.LibraryDirs[i]);
#endif
            if (options.NoGenIncludeDirs != null)
                for (var i = 0; i < options.NoGenIncludeDirs.Count; i++)
                    options.NoGenIncludeDirs[i] = Path.GetFullPath(options.NoGenIncludeDirs[i]);

            if (string.IsNullOrWhiteSpace(options.OutputNamespace))
                options.OutputNamespace = options.LibraryName;
        }

        public void Setup()
        {
            ValidateOptions(Options);

            if (Options.NoGenIncludeDirs != null)
                foreach (var incDir in Options.NoGenIncludeDirs)
                    if (!Options.IncludeDirs.Contains(incDir))
                        Options.IncludeDirs.Add(incDir);

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

#if OLD_PARSER
            foreach (var diag in result.Diagnostics)
            {
                if (Options.IgnoreParseWarnings
                    && diag.Level == ParserDiagnosticLevel.Warning)
                    continue;

                if (diag.Level == ParserDiagnosticLevel.Note)
                    continue;

                Diagnostics.EmitMessage(DiagnosticId.ParserDiagnostic,
                    "{0}({1},{2}): {3}: {4}", diag.FileName, diag.LineNumber,
                    diag.ColumnNumber, diag.Level.ToString().ToLower(),
                    diag.Message);
            }
#else
            for (uint i = 0; i < result.DiagnosticsCount; ++i)
            {
                var diag = result.getDiagnostics(i);

                if (Options.IgnoreParseWarnings
                    && diag.Level == ParserDiagnosticLevel.Warning)
                    continue;

                if (diag.Level == ParserDiagnosticLevel.Note)
                    continue;

                Diagnostics.EmitMessage(DiagnosticId.ParserDiagnostic,
                    "{0}({1},{2}): {3}: {4}", diag.FileName, diag.LineNumber,
                    diag.ColumnNumber, diag.Level.ToString().ToLower(),
                    diag.Message);
            }

#endif
        }

        ParserOptions BuildParseOptions(SourceFile file)
        {
            var options = new ParserOptions
            {
                FileName = file.Path,
#if OLD_PARSER
                Arguments = Options.Arguments,
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

#if !OLD_PARSER
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

            for (uint i = 0; i < Options.LibraryDirsCount; ++i)
            {
                var lib = Options.getLibraryDirs(i);
                options.addLibraryDirs(lib);
            }
#endif

            return options;
        }

        public bool ParseCode()
        {
            foreach (var header in Options.Headers)
            {
                var source = Project.AddFile(header);
                source.Options = BuildParseOptions(source);
            }

#if !OLD_PARSER
            var parser = new ClangParser(new Parser.AST.ASTContext());
#else
            var parser = new ClangParser(ASTContext);
#endif

            parser.SourceParsed += OnSourceFileParsed;
            parser.ParseProject(Project, Options);
           
            TargetInfo = parser.GetTargetInfo(Options);

#if !OLD_PARSER
            ASTContext = ClangParser.ConvertASTContext(parser.ASTContext);
#endif

            return true;
        }

        public ParserTargetInfo TargetInfo { get; set; }

        public bool ParseLibraries()
        {
            foreach (var library in Options.Libraries)
            {
                var parser = new ClangParser();
                parser.LibraryParsed += OnFileParsed;

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
            TranslationUnitPasses.AddPass(new CheckIgnoredDeclsPass());

            if (Options.IsCSharpGenerator)
                TranslationUnitPasses.AddPass(new GenerateInlinesCodePass());

            library.SetupPasses(this);

            TranslationUnitPasses.AddPass(new FindSymbolsPass());
            TranslationUnitPasses.AddPass(new CheckStaticClass());
            TranslationUnitPasses.AddPass(new MoveOperatorToClassPass());
            TranslationUnitPasses.AddPass(new MoveFunctionToClassPass());
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
                    Diagnostics.EmitMessage("Generated '{0}'", fileRelativePath);

                    var file = Path.Combine(outputPath, fileRelativePath);
                    File.WriteAllText(file, template.Generate());
                    Options.CodeFiles.Add(file);
                }
            }
        }

        public void CompileCode()
        {
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

            compilerParameters.ReferencedAssemblies.Add(typeof (object).Assembly.Location);
            var location = Assembly.GetExecutingAssembly().Location;
            var locationRuntime = Path.Combine(Path.GetDirectoryName(location),
                "CppSharp.Runtime.dll");
            compilerParameters.ReferencedAssemblies.Add(locationRuntime);

            var codeProvider = new CSharpCodeProvider(
                new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var compilerResults = codeProvider.CompileAssemblyFromFile(
                compilerParameters, Options.CodeFiles.ToArray());

            var errors = compilerResults.Errors.Cast<CompilerError>();
            foreach (var error in errors.Where(error => !error.IsWarning))
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

            var Log = new TextDiagnosticPrinter();
            var driver = new Driver(options, Log);

            library.Setup(driver);
            driver.Setup();

            Log.Verbose = driver.Options.Verbose;

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