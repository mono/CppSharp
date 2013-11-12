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
            if (AlreadyVisited(function) || function.Ignore || function.Namespace is Class)
                return base.VisitFunctionDecl(function);

            Class @class = FindClassToMoveFunctionTo(function.Namespace);
            if (@class != null)
            {
                MoveFunction(function, @class);
            }
            return base.VisitFunctionDecl(function);
        }

        private Class FindClassToMoveFunctionTo(INamedDecl @namespace)
        {
            TranslationUnit unit = @namespace as TranslationUnit;
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

            if (method.OperatorKind != CXXOperatorKind.None)
            {
                var param = function.Parameters[0];
                Class type;
                if (!FunctionToInstanceMethodPass.GetClassParameter(param, out type))
                    return;
                method.Kind = CXXMethodKind.Operator;
                method.SynthKind = FunctionSynthKind.NonMemberOperator;
                method.OriginalFunction = null;
            }

            function.ExplicityIgnored = true;

            @class.Methods.Add(method);
        }
    }
}
