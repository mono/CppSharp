using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CppSharp.AST;
using CppSharp.Parser;
using CppSharp.Utils;

namespace CppSharp.Passes
{
    public class GenerateSymbolsPass : TranslationUnitPass
    {
        public GenerateSymbolsPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassMethods | VisitFlags.ClassProperties |
            VisitFlags.ClassTemplateSpecializations | VisitFlags.NamespaceVariables);

        public override bool VisitASTContext(ASTContext context)
        {
            var result = base.VisitASTContext(context);
            var findSymbolsPass = Context.TranslationUnitPasses.FindPass<FindSymbolsPass>();
            
            GenerateSymbols();

            if (RemainingCompilationTasks > 0)
                findSymbolsPass.Wait = true;

            return result;
        }

        public event EventHandler<SymbolsCodeEventArgs> SymbolsCodeGenerated;

        private void GenerateSymbols()
        {
            var modules = Options.Modules.Where(symbolsCodeGenerators.ContainsKey).ToList();
            RemainingCompilationTasks = modules.Count;

            foreach (var module in modules)
            {
                var symbolsCodeGenerator = symbolsCodeGenerators[module];

                var cpp = $"{module.SymbolsLibraryName}.{symbolsCodeGenerator.FileExtension}";
                Directory.CreateDirectory(Options.OutputDir);
                var path = Path.Combine(Options.OutputDir, cpp);
                File.WriteAllText(path, symbolsCodeGenerator.Generate());

                var e = new SymbolsCodeEventArgs(module);
                SymbolsCodeGenerated?.Invoke(this, e);
                if (!string.IsNullOrEmpty(e.CustomCompiler))
                {
                    InvokeCompiler(e.CustomCompiler, e.CompilerArguments,
                        e.OutputDir, module);
                    continue;
                }

                if (Options.CheckSymbols && (module == Options.SystemModule ||
                    // if the user's provided no libraries, he only wants to generate code
                    (module.LibraryDirs.Count > 0 && module.Libraries.Count > 0)))
                {
                    using var linkerOptions = new LinkerOptions(Context.LinkerOptions);

                    foreach (var libraryDir in module.Dependencies
                                 .Union(new[] { module })
                                 .SelectMany(d => d.LibraryDirs))
                    {
                        linkerOptions.AddLibraryDirs(libraryDir);
                    }

                    foreach (var library in module.Dependencies
                                 .Union(new[] { module })
                                 .SelectMany(d => d.Libraries))
                    {
                        linkerOptions.AddLibraries(library);
                    }

                    compiledLibraries[module] = Build(linkerOptions, path, module);
                }

                --RemainingCompilationTasks;
            }
        }

        private CompiledLibrary Build(LinkerOptions linkerOptions, string path, Module module)
        {
            var useBuiltinToolchain = Platform.IsWindows;
            if (useBuiltinToolchain)
            {
                linkerOptions.Setup(Context.ParserOptions.TargetTriple, Context.ParserOptions.LanguageVersion);
                using var result = Parser.ClangParser.Build(
                    Context.ParserOptions, linkerOptions, path,
                    Last: RemainingCompilationTasks == 1);

                if (!PrintDiagnostics(result))
                    return null;
            }
            else
            {
                using var result = Parser.ClangParser.Compile(Context.ParserOptions, path);
                if (result != null)
                {
                    if (!PrintDiagnostics(result))
                        return null;
                }

                linkerOptions.Setup(Context.ParserOptions.TargetTriple, Context.ParserOptions.LanguageVersion);
                linkerOptions.AddArguments("-L" + Path.GetDirectoryName(path));

                var objectFile = Path.ChangeExtension(path, "o");
                linkerOptions.AddArguments(objectFile);

                var targetPlatform = Options.Compilation.Platform.GetValueOrDefault(Platform.Host);
                var sharedObjectFile = LinkerOptions.GetSharedObjectName(path, targetPlatform);
                linkerOptions.AddArguments("-o " + sharedObjectFile);
                linkerOptions.SetupLibraryArguments();

                var linker = LinkerOptions.GetLinkerExecutableName(targetPlatform);
                var invocation = linkerOptions.GetLinkerInvocation();

                Diagnostics.Message($"Linking library {Path.GetFileName(sharedObjectFile)}...");
                if (Options.Verbose)
                    Diagnostics.Message($"Invoking the linker with: {linker} {invocation}");

                var outMessage = ProcessHelper.Run(
                    linker, invocation, out var errorCode, out var errorMessage);

                if (errorCode != 0)
                {
                    Diagnostics.Error($"Linking failed with: {outMessage} {errorMessage}");
                }
                else
                {
                    Diagnostics.Message("Linking success.");
                }
            }

            return new CompiledLibrary
            {
                OutputDir = Options.OutputDir,
                Library = module.SymbolsLibraryName
            };
        }

        public override bool VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            if (!specialization.IsGenerated ||
                specialization.Arguments.Any(
                    a => specialization.UnsupportedTemplateArgument(a, Context.TypeMaps)))
                return false;

            return base.VisitClassTemplateSpecializationDecl(specialization);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            if (@class.IsDependent)
                foreach (var specialization in @class.Specializations.Where(
                    s => s.IsExplicitlyGenerated))
                    specialization.Visit(this);
            else
                CheckBasesForSpecialization(@class);

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            if (function.IsGenerated)
            {
                ASTUtils.CheckTypeForSpecialization(function.OriginalReturnType.Type,
                    function, Add, Context.TypeMaps);
                foreach (var parameter in function.Parameters)
                    ASTUtils.CheckTypeForSpecialization(parameter.Type, function,
                        Add, Context.TypeMaps);
            }

            if (!NeedsSymbol(function))
                return false;

            Module module = function.TranslationUnit.Module;
            return function.Visit(GetSymbolsCodeGenerator(module));
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!base.VisitVariableDecl(variable) ||
                variable.Namespace is not ClassTemplateSpecialization specialization ||
                specialization.SpecializationKind == TemplateSpecializationKind.ExplicitSpecialization)
                return false;

            Module module = variable.TranslationUnit.Module;
            return variable.Visit(GetSymbolsCodeGenerator(module));
        }

        public class SymbolsCodeEventArgs : EventArgs
        {
            public SymbolsCodeEventArgs(Module module)
            {
                this.Module = module;
            }

            public Module Module { get; set; }
            public string CustomCompiler { get; set; }
            public string CompilerArguments { get; set; }
            public string OutputDir { get; set; }
        }

        private bool NeedsSymbol(Function function)
        {
            var method = function as Method;
            bool isInImplicitSpecialization;
            var declarationContext = function.Namespace;
            do
            {
                isInImplicitSpecialization =
                    declarationContext is ClassTemplateSpecialization specialization &&
                    specialization.SpecializationKind !=
                        TemplateSpecializationKind.ExplicitSpecialization;
                declarationContext = declarationContext.Namespace;
            } while (!isInImplicitSpecialization && declarationContext != null);

            return function.IsGenerated && !function.IsDeleted &&
                !function.IsDependent && !function.IsPure && function.Namespace.IsGenerated &&
                (!string.IsNullOrEmpty(function.Body) ||
                 isInImplicitSpecialization || function.IsImplicit) &&
                (method?.NeedsSymbol() != false) &&
                // we cannot handle nested anonymous types
                (!(function.Namespace is Class) || !string.IsNullOrEmpty(function.Namespace.OriginalName)) &&
                !Context.Symbols.FindLibraryBySymbol(function.Mangled, out _);
        }

        private SymbolsCodeGenerator GetSymbolsCodeGenerator(Module module)
        {
            if (symbolsCodeGenerators.TryGetValue(module, out SymbolsCodeGenerator generator))
                return generator;

            var symbolsCodeGenerator = new SymbolsCodeGenerator(Context, module.Units);
            symbolsCodeGenerators[module] = symbolsCodeGenerator;
            symbolsCodeGenerator.Process();

            return symbolsCodeGenerator;
        }

        private static bool PrintDiagnostics(ParserResult result)
        {
            bool success = true;
            for (uint i = 0; i < result.DiagnosticsCount; i++)
            {
                var diag = result.GetDiagnostics(i);
                switch (diag.Level)
                {
                    case ParserDiagnosticLevel.Ignored:
                    case ParserDiagnosticLevel.Note:
                        Diagnostics.Message("{0}({1},{2}): {3}: {4}",
                            diag.FileName, diag.LineNumber, diag.ColumnNumber,
                            diag.Level.ToString().ToLower(), diag.Message);
                        break;
                    case ParserDiagnosticLevel.Warning:
                        Diagnostics.Warning("{0}({1},{2}): {3}: {4}",
                            diag.FileName, diag.LineNumber, diag.ColumnNumber,
                            diag.Level.ToString().ToLower(), diag.Message);
                        break;
                    case ParserDiagnosticLevel.Error:
                        Diagnostics.Error("{0}({1},{2}): {3}: {4}",
                            diag.FileName, diag.LineNumber, diag.ColumnNumber,
                            diag.Level.ToString().ToLower(), diag.Message);
                        success = false;
                        break;
                    case ParserDiagnosticLevel.Fatal:
                        Diagnostics.Debug("{0}({1},{2}): {3}: {4}",
                            diag.FileName, diag.LineNumber, diag.ColumnNumber,
                            diag.Level.ToString().ToLower(), diag.Message);
                        success = false;
                        break;
                }
            }
            return success;
        }

        private void InvokeCompiler(string compiler, string arguments, string outputDir, Module module)
        {
            new Thread(() =>
                {
                    ProcessHelper.Run(compiler, arguments, out _, out var errorMessage);
                    var output = GetOutputFile(module.SymbolsLibraryName);
                    if (!File.Exists(Path.Combine(outputDir, output)))
                        Diagnostics.Error(errorMessage);
                    else
                        compiledLibraries[module] = new CompiledLibrary
                        { OutputDir = outputDir, Library = module.SymbolsLibraryName };
                    RemainingCompilationTasks--;
                }).Start();
        }

        private void CheckBasesForSpecialization(Class @class)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass))
            {
                if (@base.Class is ClassTemplateSpecialization { IsExplicitlyGenerated: false } specialization &&
                    specialization.SpecializationKind != TemplateSpecializationKind.ExplicitSpecialization)
                    ASTUtils.CheckTypeForSpecialization(@base.Type, @class, Add, Context.TypeMaps);
                CheckBasesForSpecialization(@base.Class);
            }
        }

        private void Add(ClassTemplateSpecialization specialization)
        {
            if (!specializations.TryGetValue(specialization.TranslationUnit.Module, out var specs))
            {
                specs = new HashSet<ClassTemplateSpecialization>();
                specializations[specialization.TranslationUnit.Module] = specs;
            }

            if (specs.Add(specialization))
            {
                foreach (Method method in specialization.Methods)
                    method.Visit(this);
            }

            GetSymbolsCodeGenerator(specialization.TranslationUnit.Module);
        }

        private int _remainingCompilationTasks;
        private int RemainingCompilationTasks
        {
            get => _remainingCompilationTasks;
            set
            {
                if (_remainingCompilationTasks == value) 
                    return;

                _remainingCompilationTasks = value;
                if (_remainingCompilationTasks != 0) 
                    return;

                foreach (var module in Context.Options.Modules.Where(compiledLibraries.ContainsKey))
                {
                    CompiledLibrary compiledLibrary = compiledLibraries[module];
                    CollectSymbols(compiledLibrary.OutputDir, compiledLibrary.Library);
                }
                var findSymbolsPass = Context.TranslationUnitPasses.FindPass<FindSymbolsPass>();
                findSymbolsPass.Wait = false;
            }
        }

        private void CollectSymbols(string outputDir, string library)
        {
            using var linkerOptions = new LinkerOptions(Context.LinkerOptions);
            linkerOptions.AddLibraryDirs(outputDir);
            var output = GetOutputFile(library);
            linkerOptions.AddLibraries(output);

            using var parserResult = Parser.ClangParser.ParseLibrary(linkerOptions);
            if (parserResult.Kind != ParserResultKind.Success)
            {
                Diagnostics.Error($"Parsing of {Path.Combine(outputDir, output)} failed.");
                return;
            }

            var results = parserResult.Libraries.AsParallel()
                .Select(ClangParser.ConvertLibrary);

            lock (symbolsLock)
            {
                foreach (var nativeLibrary in results)
                {
                    Context.Symbols.Libraries.Add(nativeLibrary);
                    Context.Symbols.IndexSymbols();
                }
            }
        }

        private static string GetOutputFile(string library)
        {
            var extension = Platform.Host switch
            {
                TargetPlatform.Windows => "lib",
                TargetPlatform.Linux => "so",
                TargetPlatform.MacOS => "dylib",
                _ => throw new NotImplementedException()
            };

            return Path.GetFileName($"{(Platform.IsWindows ? string.Empty : "lib")}{library}.{extension}");
        }

        private static readonly object symbolsLock = new();

        private readonly Dictionary<Module, SymbolsCodeGenerator> symbolsCodeGenerators = new();
        private readonly Dictionary<Module, HashSet<ClassTemplateSpecialization>> specializations = new();
        private readonly Dictionary<Module, CompiledLibrary> compiledLibraries = new();

        private class CompiledLibrary
        {
            public string OutputDir { get; init; }
            public string Library { get; init; }
        }
    }
}
