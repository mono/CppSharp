using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Parser;
using CppSharp.Types;
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
            findSymbolsPass.Wait = true;
            GenerateSymbols();
            return result;
        }

        public event EventHandler<SymbolsCodeEventArgs> SymbolsCodeGenerated;

        private void GenerateSymbols()
        {
            var modules = (from module in Options.Modules
                           where symbolsCodeGenerators.ContainsKey(module)
                           select module).ToList();
            remainingCompilationTasks = modules.Count;
            foreach (var module in modules.Where(symbolsCodeGenerators.ContainsKey))
            {
                var symbolsCodeGenerator = symbolsCodeGenerators[module];
                if (specializations.ContainsKey(module))
                {
                    symbolsCodeGenerator.NewLine();
                    foreach (var specialization in specializations[module])
                        foreach (var method in specialization.Methods.Where(
                            m => m.IsGenerated && !m.IsDependent && !m.IsImplicit))
                            symbolsCodeGenerator.VisitMethodDecl(method);
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
                        e.OutputDir, module.SymbolsLibraryName);
            }
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            var module = function.TranslationUnit.Module;

            if (function.IsGenerated)
            {
                CheckTypeForSpecialization(function.OriginalReturnType.Type);
                foreach (var parameter in function.Parameters)
                    CheckTypeForSpecialization(parameter.Type);
            }

            if (!NeedsSymbol(function))
                return false;

            var symbolsCodeGenerator = GetSymbolsCodeGenerator(module);
            return function.Visit(symbolsCodeGenerator);
        }

        private void CheckTypeForSpecialization(AST.Type type)
        {
            type = type.Desugar();
            ClassTemplateSpecialization specialization;
            type = type.GetFinalPointee() ?? type;
            if (!type.TryGetDeclaration(out specialization))
                return;

            if (specialization.Ignore ||
                specialization.TemplatedDecl.TemplatedClass.Ignore ||
                specialization.IsIncomplete ||
                specialization.TemplatedDecl.TemplatedClass.IsIncomplete ||
                specialization is ClassTemplatePartialSpecialization ||
                specialization.Arguments.Any(a => UnsupportedTemplateArgument(specialization, a)))
                return;

            TypeMap typeMap;
            if (Context.TypeMaps.FindTypeMap(specialization, out typeMap))
            {
                var typePrinterContext = new TypePrinterContext { Type = type };
                var mappedTo = typeMap.CSharpSignatureType(typePrinterContext);
                mappedTo = mappedTo.Desugar();
                mappedTo = (mappedTo.GetFinalPointee() ?? mappedTo);
                if (mappedTo.IsPrimitiveType() || mappedTo.IsPointerToPrimitiveType() || mappedTo.IsEnum())
                    return;
            }

            HashSet<ClassTemplateSpecialization> list;
            if (specializations.ContainsKey(specialization.TranslationUnit.Module))
                list = specializations[specialization.TranslationUnit.Module];
            else
                specializations[specialization.TranslationUnit.Module] =
                    list = new HashSet<ClassTemplateSpecialization>();
            list.Add(specialization);
        }

        private bool UnsupportedTemplateArgument(ClassTemplateSpecialization specialization, TemplateArgument a)
        {
            if (a.Type.Type == null ||
                CheckIgnoredDeclsPass.IsTypeExternal(
                    specialization.TranslationUnit.Module, a.Type.Type))
                return true;

            var typeIgnoreChecker = new TypeIgnoreChecker(Context.TypeMaps);
            a.Type.Type.Visit(typeIgnoreChecker);
            return typeIgnoreChecker.IsIgnored;
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
            return function.IsGenerated && !function.IsDeleted && !function.IsDependent &&
                !function.IsPure && (!string.IsNullOrEmpty(function.Body) || function.IsImplicit) &&
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

        private void InvokeCompiler(string compiler, string arguments, string outputDir, string symbols)
        {
            new Thread(() =>
                {
                    int error;
                    string errorMessage;
                    ProcessHelper.Run(compiler, arguments, out error, out errorMessage);
                    if (string.IsNullOrEmpty(errorMessage))
                        CollectSymbols(outputDir, symbols);
                    else
                        Diagnostics.Error(errorMessage);
                    RemainingCompilationTasks--;
                }).Start();
        }

        private void CollectSymbols(string outputDir, string symbols)
        {
            using (var parserOptions = new ParserOptions())
            {
                parserOptions.AddLibraryDirs(outputDir);
                var output = Path.GetFileName($@"{(Platform.IsWindows ?
                    string.Empty : "lib")}{symbols}.{
                    (Platform.IsMacOS ? "dylib" : Platform.IsWindows ? "dll" : "so")}");
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
                        parserResult.Library.Dispose();
                    }
                    else
                        Diagnostics.Error($"Parsing of {Path.Combine(outputDir, output)} failed.");
                }
            }
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
                        var findSymbolsPass = Context.TranslationUnitPasses.FindPass<FindSymbolsPass>();
                        findSymbolsPass.Wait = false;
                    }
                }
            }
        }

        private int remainingCompilationTasks;
        private static readonly object @lock = new object();

        private Dictionary<Module, SymbolsCodeGenerator> symbolsCodeGenerators =
            new Dictionary<Module, SymbolsCodeGenerator>();
        private Dictionary<Module, HashSet<ClassTemplateSpecialization>> specializations =
            new Dictionary<Module, HashSet<ClassTemplateSpecialization>>();
    }
}
