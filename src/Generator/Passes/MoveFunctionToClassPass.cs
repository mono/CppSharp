using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// Moves a function to a class, if any, named after the function's header.
    /// </summary>
    public class MoveFunctionToClassPass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (!VisitDeclaration(function))
                return false;

            if (!function.IsGenerated || function.Namespace is Class)
                return false;

            var @class = FindClassToMoveFunctionTo(function.Namespace);
            if (@class != null)
            {
                MoveFunction(function, @class);
                Log.Debug("Function moved to class: {0}::{1}", @class.Name, function.Name);
            }

            if (function.IsOperator)
                function.ExplicitlyIgnore();

            return true;
        }

        private Class FindClassToMoveFunctionTo(INamedDecl @namespace)
        {
            var unit = @namespace as TranslationUnit;
            if (unit == null)
            {
                return Driver.ASTContext.FindClass(
                    @namespace.Name, ignoreCase: true).FirstOrDefault();
            }
            return Driver.ASTContext.FindCompleteClass(
                unit.FileNameWithoutExtension.ToLowerInvariant(), true);
        }

        private static void MoveFunction(Function function, Class @class)
        {
            var method = new Method(function)
            {
                Namespace = @class,
                IsStatic = true
            };

            function.ExplicitlyIgnore();

            if (method.OperatorKind != CXXOperatorKind.None)
            {
                var param = function.Parameters[0];
                Class type;
                if (!FunctionToInstanceMethodPass.GetClassParameter(param, out type))
                    return;
                method.Kind = CXXMethodKind.Operator;
                method.IsNonMemberOperator = true;
                method.OriginalFunction = null;
            }

            @class.Methods.Add(method);
        }
    }
}
