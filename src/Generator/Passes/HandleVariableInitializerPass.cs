using CppSharp.AST;
using CppSharp.Internal;

namespace CppSharp.Passes
{
    public class HandleVariableInitializerPass : TranslationUnitPass
    {
        public HandleVariableInitializerPass()
            => VisitOptions.ResetFlags(VisitFlags.NamespaceVariables);

        public override bool VisitVariableDecl(Variable variable)
        {
            if (AlreadyVisited(variable) || variable.Ignore || variable.Initializer == null)
                return false;

            string initializerString = variable.Initializer.String;
            ExpressionHelper.PrintExpression(Context, null, variable.Type, variable.Initializer,
                allowDefaultLiteral: true, ref initializerString);

            if (string.IsNullOrWhiteSpace(initializerString))
                variable.Initializer = null;
            else
                variable.Initializer.String = initializerString;

            return true;
        }
    }
}
