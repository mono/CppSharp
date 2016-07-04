using CppSharp.AST;

namespace CppSharp.Passes
{
    public class IgnoreSystemDeclarationsPass : TranslationUnitPass
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.Namespace != null && decl.TranslationUnit.IsSystemHeader)
                decl.ExplicitlyIgnore();
            return base.VisitDeclaration(decl);
        }
    }
}
