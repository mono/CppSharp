using CppSharp.AST;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Parser;

namespace CppSharp.Generators
{
    public class BindingContext
    {
        public DriverOptions Options { get; private set; }
        public ParserOptions ParserOptions { get; set; }

        public ASTContext ASTContext { get; set; }
        public ParserTargetInfo TargetInfo { get; set; }

        public SymbolContext Symbols { get; private set; }
        public TypeMapDatabase TypeMaps { get; private set; }

        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; private set; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; private set; }

        public BindingContext(DriverOptions options, ParserOptions parserOptions = null)
        {
            Options = options;
            ParserOptions = parserOptions;

            Symbols = new SymbolContext();
            TypeMaps = new TypeMapDatabase();

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