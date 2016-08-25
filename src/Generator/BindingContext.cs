using CppSharp.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Parser;
using CppSharp.Utils;
using System.Collections.Generic;

namespace CppSharp.Generators
{
    public class BindingContext
    {
        public IDiagnostics Diagnostics { get; set; }
        public DriverOptions Options { get; private set; }

        public ASTContext ASTContext { get; set; }
        public ParserTargetInfo TargetInfo { get; set; }

        public SymbolContext Symbols { get; private set; }
        public TypeMapDatabase TypeDatabase { get; private set; }

        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; private set; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; private set; }

        public Dictionary<Function, DelegatesPass.DelegateDefinition> Delegates { get; private set; }

        private static readonly Dictionary<string, string> libraryMappings = new Dictionary<string, string>();

        public BindingContext(IDiagnostics diagnostics, DriverOptions options)
        {
            Options = options;
            Diagnostics = diagnostics;

            Symbols = new SymbolContext();
            Delegates = new Dictionary<Function, DelegatesPass.DelegateDefinition>();

            TypeDatabase = new TypeMapDatabase();
            TypeDatabase.SetupTypeMaps(Options.GeneratorKind);

            TranslationUnitPasses = new PassBuilder<TranslationUnitPass>(this);
            GeneratorOutputPasses = new PassBuilder<GeneratorOutputPass>(this);
        }

        public void RunPasses()
        {
            TranslationUnitPasses.RunPasses(pass =>
                {
                    Diagnostics.Debug("Pass '{0}'", pass);

                    Diagnostics.PushIndent();
                    pass.VisitLibrary(ASTContext);
                    Diagnostics.PopIndent();
                });            
        }
    }
}