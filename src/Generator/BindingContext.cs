using CppSharp.AST;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Parser;
using System.Collections.Generic;

namespace CppSharp.Generators
{
    public class BindingContext
    {
        public IDiagnostics Diagnostics { get; set; }

        public DriverOptions Options { get; private set; }
        public ParserOptions ParserOptions { get; set; }

        public ASTContext ASTContext { get; set; }
        public ParserTargetInfo TargetInfo { get; set; }

        public SymbolContext Symbols { get; private set; }
        public TypeMapDatabase TypeMaps { get; private set; }

        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; private set; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; private set; }

        public Dictionary<Function, DelegatesPass.DelegateDefinition> Delegates { get; private set; }

        private static readonly Dictionary<string, string> libraryMappings = new Dictionary<string, string>();

        public BindingContext(IDiagnostics diagnostics, DriverOptions options,
            ParserOptions parserOptions = null)
        {
            Options = options;
            Diagnostics = diagnostics;
            ParserOptions = parserOptions;

            Symbols = new SymbolContext();
            Delegates = new Dictionary<Function, DelegatesPass.DelegateDefinition>();

            TypeMaps = new TypeMapDatabase();
            TypeMaps.SetupTypeMaps(Options.GeneratorKind);

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