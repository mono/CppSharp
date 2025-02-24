using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class FixDefaultParamValuesOfOverridesPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!method.IsOverride || method.IsSynthesized)
                return true;

            Method rootBaseMethod = method.GetRootBaseMethod();
            var rootBaseParameters = rootBaseMethod.Parameters.Where(
                p => p.Kind != ParameterKind.IndirectReturnType).ToList();
            var parameters = method.Parameters.Where(
                p => p.Kind != ParameterKind.IndirectReturnType).ToList();
            for (int i = 0; i < parameters.Count; i++)
            {
                var rootBaseParameter = rootBaseParameters[i];
                var parameter = parameters[i];
                if (rootBaseParameter.DefaultArgument == null)
                    parameter.DefaultArgument = null;
                else
                    parameter.DefaultArgument = rootBaseParameter.DefaultArgument.Clone();
            }

            return true;
        }
    }
}
