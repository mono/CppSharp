using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FixDefaultParamValuesOfOverridesPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (method.IsOverride && !method.IsSynthetized)
            {
                Method rootBaseMethod = ((Class) method.Namespace).GetBaseMethod(method);
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var rootBaseParameter = rootBaseMethod.Parameters[i];
                    var parameter = method.Parameters[i];
                    if (rootBaseParameter.DefaultArgument == null)
                    {
                        parameter.DefaultArgument = null;
                    }
                    else
                    {
                        parameter.DefaultArgument = rootBaseParameter.DefaultArgument.Clone();
                    }
                }
            }
            return base.VisitMethodDecl(method);
        }
    }
}
