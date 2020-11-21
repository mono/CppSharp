using System.Linq;
using System.Threading;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FindSymbolsPass : TranslationUnitPass
    {
        public FindSymbolsPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassMethods | VisitFlags.ClassProperties);

        public bool Wait
        {
            get { return wait; }
            set
            {
                if (wait == value)
                    return;

                wait = value;

                if (wait || manualResetEvent == null)
                    return;

                manualResetEvent.Set();
                manualResetEvent.Dispose();
                manualResetEvent = null;
            }
        }

        public override bool VisitASTContext(ASTContext context)
        {
            if (Wait)
            {
                manualResetEvent = new ManualResetEvent(false);
                manualResetEvent.WaitOne();
            }

            return base.VisitASTContext(context);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            if (!Options.CheckSymbols || Options.IsCLIGenerator)
                return false;

            var @class = decl.Namespace as Class;
            if (@class?.IsDependent == true)
            {
                // Search for a match but always go through all
                switch (decl)
                {
                    case Method _:
                        return (from specialization in @class.Specializations
                                from specializedFunction in specialization.Methods
                                select specializedFunction.InstantiatedFrom == decl &&
                                    CheckForSymbol(specializedFunction)).DefaultIfEmpty().Max();
                    case Variable _:
                        return (from specialization in @class.Specializations
                                from specializedVariable in specialization.Variables
                                select specializedVariable.Name == decl.Name &&
                                    CheckForSymbol(specializedVariable)).DefaultIfEmpty().Max();
                }
            }
            return CheckForSymbol(decl);
        }

        private bool CheckForSymbol(Declaration decl)
        {
            var mangledDecl = decl as IMangledDecl;
            var method = decl as Method;
            if (decl.IsGenerated && mangledDecl != null &&
                method?.NeedsSymbol() == true &&
                !VisitMangledDeclaration(mangledDecl))
            {
                decl.ExplicitlyIgnore();
                return false;
            }

            return true;
        }

        private bool VisitMangledDeclaration(IMangledDecl mangledDecl)
        {
            if (!Context.Symbols.FindLibraryBySymbol(mangledDecl.Mangled, out _))
            {
                if (mangledDecl is Variable variable && variable.IsConstExpr)
                    return true;

                Diagnostics.Warning("Symbol not found: {0}", mangledDecl.Mangled);
                return false;
            }
            return true;
        }

        private bool wait;
        private ManualResetEvent manualResetEvent;
    }
}
