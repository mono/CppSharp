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
    public class GenerateInlinesPass : TranslationUnitPass
    {
        public GenerateInlinesPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
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
            GenerateInlines();
            return result;
        }

        public event EventHandler<InlinesCodeEventArgs> InlinesCodeGenerated;

        private void GenerateInlines()
        {
            var modules = (from module in Options.Modules
                           where inlinesCodeGenerators.ContainsKey(module)
                           select module).ToList();
            remainingCompilationTasks = modules.Count;
            foreach (var module in modules)
            {
                var inlinesCodeGenerator = inlinesCodeGenerators[module];
                var cpp = $"{module.InlinesLibraryName}.{inlinesCodeGenerator.FileExtension}";
                Directory.CreateDirectory(Options.OutputDir);
                var path = Path.Combine(Options.OutputDir, cpp);
                File.WriteAllText(path, inlinesCodeGenerator.Generate());

                var e = new InlinesCodeEventArgs(module);
                InlinesCodeGenerated?.Invoke(this, e);
                if (string.IsNullOrEmpty(e.CustomCompiler))
                    RemainingCompilationTasks--;
                else
                    InvokeCompiler(e.CustomCompiler, e.CompilerArguments,
                        e.OutputDir, module.InlinesLibraryName);
            }
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            var module = function.TranslationUnit.Module;
            if (module == Options.SystemModule)
            {
                GetInlinesCodeGenerator(module);
                return false;
            }

            if (!NeedsSymbol(function))
                return false;

            var inlinesCodeGenerator = GetInlinesCodeGenerator(module);
            return function.Visit(inlinesCodeGenerator);
        }

        public class InlinesCodeEventArgs : EventArgs
        {
            public InlinesCodeEventArgs(Module module)
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

        private InlinesCodeGenerator GetInlinesCodeGenerator(Module module)
        {
            if (inlinesCodeGenerators.ContainsKey(module))
                return inlinesCodeGenerators[module];
            
            var inlinesCodeGenerator = new InlinesCodeGenerator(Context, module.Units);
            inlinesCodeGenerators[module] = inlinesCodeGenerator;
            inlinesCodeGenerator.Process();

            return inlinesCodeGenerator;
        }

        private void InvokeCompiler(string compiler, string arguments, string outputDir, string inlines)
        {
            new Thread(() =>
                {
                    int error;
                    string errorMessage;
                    ProcessHelper.Run(compiler, arguments, out error, out errorMessage);
                    if (string.IsNullOrEmpty(errorMessage))
                        CollectInlinedSymbols(outputDir, inlines);
                    else
                        Diagnostics.Error(errorMessage);
                    RemainingCompilationTasks--;
                }).Start();
        }

        private void CollectInlinedSymbols(string outputDir, string inlines)
        {
            using (var parserOptions = new ParserOptions())
            {
                parserOptions.AddLibraryDirs(outputDir);
                var output = Path.GetFileName($@"{(Platform.IsWindows ?
                    string.Empty : "lib")}{inlines}.{
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

        private Dictionary<Module, InlinesCodeGenerator> inlinesCodeGenerators =
            new Dictionary<Module, InlinesCodeGenerator>();
    }
}
