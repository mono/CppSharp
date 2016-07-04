using CppSharp.AST;

namespace CppSharp.Passes
{
    public class IgnoreAbstractOperatorsPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method))
                return false;

            if (method.IsPure && method.IsOperator)
                method.ExplicitlyIgnore();

            return true;
        }
    }
}
