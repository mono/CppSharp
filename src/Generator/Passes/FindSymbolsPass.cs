using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FindSymbolsPass : TranslationUnitPass
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            IMangledDecl mangledDecl = decl as IMangledDecl;
            if (mangledDecl != null && !VisitMangledDeclaration(mangledDecl))
            {
                decl.ExplicityIgnored = true;
                return false;
            }
            return base.VisitDeclaration(decl);
        }

        private bool VisitMangledDeclaration(IMangledDecl mangledDecl)
        {
            string symbol = mangledDecl.Mangled;
            if (!Driver.LibrarySymbols.FindSymbol(ref symbol))
            {
                Driver.Diagnostics.EmitWarning(DiagnosticId.SymbolNotFound, "Symbol not found: {0}", symbol);
                return false;
            }
            mangledDecl.Mangled = symbol;
            return true;
        }
    }
}
