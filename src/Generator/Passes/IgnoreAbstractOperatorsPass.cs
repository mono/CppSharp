using CppSharp.AST;

namespace CppSharp.Passes
{
    public class IgnoreAbstractOperatorsPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || method.TranslationUnit.IsSystemHeader)
                return false;

            if (method.IsPure && method.IsOperator)
                method.ExplicitlyIgnore();

            return true;
        }
    }
}
