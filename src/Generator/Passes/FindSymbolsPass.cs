using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FindSymbolsPass : TranslationUnitPass
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            var options = Driver.Options;
            if (!options.CheckSymbols || options.IsCLIGenerator)
                return false;

            var mangledDecl = decl as IMangledDecl;
            var method = decl as Method;
            if (mangledDecl != null && !(method != null && (method.IsPure || method.IsSynthetized)) &&
                !VisitMangledDeclaration(mangledDecl))
            {
                decl.ExplicitlyIgnore();
                return false;
            }

            return base.VisitDeclaration(decl);
        }

        private bool VisitMangledDeclaration(IMangledDecl mangledDecl)
        {
            var symbol = mangledDecl.Mangled;

            if (!Driver.Symbols.FindSymbol(ref symbol))
            {
                Driver.Diagnostics.EmitWarning(DiagnosticId.SymbolNotFound,
                    "Symbol not found: {0}", symbol);
                return false;
            }

            mangledDecl.Mangled = symbol;
            return true;
        }
    }
}
