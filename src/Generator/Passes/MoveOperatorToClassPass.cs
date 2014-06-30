using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MoveOperatorToClassPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            // Ignore methods as they are not relevant for this pass.
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated || !function.IsOperator)
                return false;

            Class @class = null;
            foreach (var param in function.Parameters)
            {
                FunctionToInstanceMethodPass.GetClassParameter(
                    param, out @class);

                if (@class != null) break;
            }

            if (@class == null)
                return false;

            // Create a new fake method so it acts as a static method.
            var method = new Method(function)
            {
                Namespace = @class,
                Kind = CXXMethodKind.Operator,
                OperatorKind = function.OperatorKind,
                IsNonMemberOperator = true,
                OriginalFunction = null,
                IsStatic = true
            };

            function.ExplicitlyIgnore();

            @class.Methods.Add(method);

            Driver.Diagnostics.Debug("Function converted to operator: {0}::{1}",
                @class.Name, function.Name);

            return true;
        }
    }
}
