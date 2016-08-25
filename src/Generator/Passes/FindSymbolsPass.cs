using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FindSymbolsPass : TranslationUnitPass
    {
        public FindSymbolsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitTemplateArguments = false;
            VisitOptions.VisitClassFields = false;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            if (!Options.CheckSymbols || Options.IsCLIGenerator)
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

            if (!Context.Symbols.FindSymbol(ref symbol))
            {
                Diagnostics.Warning("Symbol not found: {0}", symbol);
                return false;
            }

            mangledDecl.Mangled = symbol;
            return true;
        }
    }
}
