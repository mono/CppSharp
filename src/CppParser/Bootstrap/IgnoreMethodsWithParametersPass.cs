using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Passes;
using System.Linq;

namespace CppSharp
{
    public class IgnoreMethodsWithParametersPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method))
                return false;

            if (!method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void) &&
                method.Parameters.Count(
                    p => p.Kind == ParameterKind.Regular && p.DefaultValue == null) > 0)
                method.ExplicitlyIgnore();

            return true;
        }
    }
}
