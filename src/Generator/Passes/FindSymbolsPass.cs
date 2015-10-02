using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FindSymbolsPass : TranslationUnitPass
    {
        public FindSymbolsPass()
        {
            Options.VisitClassBases = false;
            Options.VisitFunctionParameters = false;
            Options.VisitFunctionReturnType = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitNamespaceTypedefs = false;
            Options.VisitTemplateArguments = false;
            Options.VisitClassFields = false;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            var options = Driver.Options;
            if (!options.CheckSymbols || options.IsCLIGenerator)
                return false;

            var mangledDecl = decl as IMangledDecl;
            var method = decl as Method;
            if (decl.IsGenerated && mangledDecl != null &&
                !(method != null && (method.IsPure || method.IsSynthetized)) &&
                !VisitMangledDeclaration(mangledDecl))
            {
                decl.ExplicitlyIgnore();
                return false;
            }

            return true;
        }

        private bool VisitMangledDeclaration(IMangledDecl mangledDecl)
        {
            var symbol = mangledDecl.Mangled;

            if (!Driver.Symbols.FindSymbol(ref symbol))
            {
                Driver.Diagnostics.Warning("Symbol not found: {0}", symbol);
                return false;
            }

            mangledDecl.Mangled = symbol;
            return true;
        }
    }
}
