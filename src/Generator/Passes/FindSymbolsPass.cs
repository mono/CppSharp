using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FindSymbolsPass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            string symbol = function.Mangled;
            if (!Driver.LibrarySymbols.FindSymbol(ref symbol))
            {
                Driver.Diagnostics.EmitWarning(DiagnosticId.SymbolNotFound,
                    "Symbol not found: {0}", symbol);
                function.ExplicityIgnored = true;
                return false;
            }
            function.Mangled = symbol;
            return base.VisitFunctionDecl(function);
        }
    }
}
