﻿using System;
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
        public GenerateSymbolsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassTemplateSpecializations = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            var result = base.VisitASTContext(context);
            var findSymbolsPass = Context.TranslationUnitPasses.FindPass<FindSymbolsPass>();
            GenerateSymbols();
            if (remainingCompilationTasks > 0)
                findSymbolsPass.Wait = true;
            return result;
        }

        public event EventHandler<SymbolsCodeEventArgs> SymbolsCodeGenerated;

        private void GenerateSymbols()
        {
            var modules = Options.Modules.Where(symbolsCodeGenerators.ContainsKey).ToList();
            remainingCompilationTasks = modules.Count;
            foreach (var module in modules)
            {
                var symbolsCodeGenerator = symbolsCodeGenerators[module];
                if (specializations.ContainsKey(module))
                {
                    symbolsCodeGenerator.NewLine();
                    foreach (var specialization in specializations[module])
                    {
                        Func<Method, bool> exportable = m => !m.IsDependent &&
                            !m.IsImplicit && !m.IsDeleted && !m.IsDefaulted;
                        if (specialization.Methods.Any(m => m.IsInvalid && exportable(m)))
                            foreach (var method in specialization.Methods.Where(
                                m => m.IsGenerated && (m.InstantiatedFrom == null || m.InstantiatedFrom.IsGenerated) &&
                                     exportable(m)))
                                symbolsCodeGenerator.VisitMethodDecl(method);
                        else
                            symbolsCodeGenerator.VisitClassTemplateSpecializationDecl(specialization);
                    }
                }

                var cpp = $"{module.SymbolsLibraryName}.{symbolsCodeGenerator.FileExtension}";
                Directory.CreateDirectory(Options.OutputDir);
                var path = Path.Combine(Options.OutputDir, cpp);
                File.WriteAllText(path, symbolsCodeGenerator.Generate());

                var e = new SymbolsCodeEventArgs(module);
                SymbolsCodeGenerated?.Invoke(this, e);
                if (string.IsNullOrEmpty(e.CustomCompiler))
                    RemainingCompilationTasks--;
                else
                    InvokeCompiler(e.CustomCompiler, e.CompilerArguments,
                        e.OutputDir, module);
            }
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

            var module = function.TranslationUnit.Module;

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

            var symbolsCodeGenerator = GetSymbolsCodeGenerator(module);
            return function.Visit(symbolsCodeGenerator);
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
            var mangled = function.Mangled;
            var method = function as Method;
            return function.IsGenerated && !function.IsDeleted &&
                !function.IsDependent && !function.IsPure &&
                (!string.IsNullOrEmpty(function.Body) || function.IsImplicit) &&
                !(function.Namespace is ClassTemplateSpecialization) &&
                // we don't need symbols for virtual functions anyway
                (method == null || (!method.IsVirtual && !method.IsSynthetized &&
                 (!method.IsConstructor || !((Class) method.Namespace).IsAbstract))) &&
                // we cannot handle nested anonymous types
                (!(function.Namespace is Class) || !string.IsNullOrEmpty(function.Namespace.OriginalName)) &&
                !Context.Symbols.FindSymbol(ref mangled);
        }

        private SymbolsCodeGenerator GetSymbolsCodeGenerator(Module module)
        {
            if (symbolsCodeGenerators.ContainsKey(module))
                return symbolsCodeGenerators[module];
            
            var symbolsCodeGenerator = new SymbolsCodeGenerator(Context, module.Units);
            symbolsCodeGenerators[module] = symbolsCodeGenerator;
            symbolsCodeGenerator.Process();

            return symbolsCodeGenerator;
        }

        private void InvokeCompiler(string compiler, string arguments, string outputDir, Module module)
        {
            new Thread(() =>
                {
                    int error;
                    string errorMessage;
                    ProcessHelper.Run(compiler, arguments, out error, out errorMessage);
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
                var specialization = @base.Class as ClassTemplateSpecialization;
                if (specialization != null && !specialization.IsExplicitlyGenerated &&
                    specialization.SpecializationKind != TemplateSpecializationKind.ExplicitSpecialization)
                    ASTUtils.CheckTypeForSpecialization(@base.Type, @class, Add, Context.TypeMaps);
                CheckBasesForSpecialization(@base.Class);
            }
        }

        private void Add(ClassTemplateSpecialization specialization)
        {
            ICollection<ClassTemplateSpecialization> specs;
            if (specializations.ContainsKey(specialization.TranslationUnit.Module))
                specs = specializations[specialization.TranslationUnit.Module];
            else specs = specializations[specialization.TranslationUnit.Module] =
                new HashSet<ClassTemplateSpecialization>();
            specs.Add(specialization);
            GetSymbolsCodeGenerator(specialization.TranslationUnit.Module);
        }

        private int RemainingCompilationTasks
        {
            get { return remainingCompilationTasks; }
            set
            {
                if (remainingCompilationTasks != value)
                {
                    remainingCompilationTasks = value;
                    if (remainingCompilationTasks == 0)
                    {
                        foreach (var module in Context.Options.Modules.Where(compiledLibraries.ContainsKey))
                        {
                            CompiledLibrary compiledLibrary = compiledLibraries[module];
                            CollectSymbols(compiledLibrary.OutputDir, compiledLibrary.Library);
                        }
                        var findSymbolsPass = Context.TranslationUnitPasses.FindPass<FindSymbolsPass>();
                        findSymbolsPass.Wait = false;
                    }
                }
            }
        }

        private void CollectSymbols(string outputDir, string library)
        {
            using (var parserOptions = new ParserOptions())
            {
                parserOptions.AddLibraryDirs(outputDir);
                var output = GetOutputFile(library);
                parserOptions.LibraryFile = output;
                using (var parserResult = Parser.ClangParser.ParseLibrary(parserOptions))
                {
                    if (parserResult.Kind == ParserResultKind.Success)
                    {
                        var nativeLibrary = ClangParser.ConvertLibrary(parserResult.Library);
                        lock (@lock)
                        {
                            Context.Symbols.Libraries.Add(nativeLibrary);
                            Context.Symbols.IndexSymbols();
                        }
                    }
                    else
                        Diagnostics.Error($"Parsing of {Path.Combine(outputDir, output)} failed.");
                }
            }
        }

        private static string GetOutputFile(string library)
        {
            return Path.GetFileName($@"{(Platform.IsWindows ?
                string.Empty : "lib")}{library}.{
                (Platform.IsMacOS ? "dylib" : Platform.IsWindows ? "dll" : "so")}");
        }

        private int remainingCompilationTasks;
        private static readonly object @lock = new object();

        private Dictionary<Module, SymbolsCodeGenerator> symbolsCodeGenerators =
            new Dictionary<Module, SymbolsCodeGenerator>();
        private Dictionary<Module, HashSet<ClassTemplateSpecialization>> specializations =
            new Dictionary<Module, HashSet<ClassTemplateSpecialization>>();
        private Dictionary<Module, CompiledLibrary> compiledLibraries = new Dictionary<Module, CompiledLibrary>();

        private class CompiledLibrary
        {
            public string OutputDir { get; set; }
            public string Library { get; set; }
        }
    }
}
