using CppSharp.AST;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Parser;

namespace CppSharp.Generators
{
    public class BindingContext
    {
        public DriverOptions Options { get; }
        public ParserOptions ParserOptions { get; set; }
        public LinkerOptions LinkerOptions { get; set; }

        public ASTContext ASTContext { get; set; }
        public ParserTargetInfo TargetInfo { get; set; }

        public SymbolContext Symbols { get; }

        public TypeMapDatabase TypeMaps { get; set; }
        public DeclMapDatabase DeclMaps { get; set; }

        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; }

        public BindingContext(DriverOptions options, ParserOptions parserOptions = null)
        {
            Options = options;
            ParserOptions = parserOptions;
            LinkerOptions = new LinkerOptions();

            Symbols = new SymbolContext();

            TranslationUnitPasses = new PassBuilder<TranslationUnitPass>(this);
            GeneratorOutputPasses = new PassBuilder<GeneratorOutputPass>(this);
        }

        public void RunPasses()
        {
            TranslationUnitPasses.RunPasses(pass =>
                {
                    Diagnostics.Debug("Pass '{0}'", pass);

                    Diagnostics.PushIndent();
                    pass.Context = this;
                    pass.VisitASTContext(ASTContext);
                    Diagnostics.PopIndent();
                });
        }
    }
}