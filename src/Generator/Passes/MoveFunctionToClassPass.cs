using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MoveFunctionToClassPass : TranslationUnitPass
    {
        public MoveFunctionToClassPass()
            => VisitOptions.ResetFlags(VisitFlags.Default);

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated)
                return false;

            Class @class = FindClassToMoveFunctionTo(function);
            if (@class == null)
                return false;

            // Create a new fake method so it acts as a static method.
            var method = new Method(function)
            {
                Namespace = @class,
                OperatorKind = function.OperatorKind,
                OriginalFunction = null,
                Conversion = MethodConversionKind.FunctionToStaticMethod,
                IsStatic = true,
            };

            if (method.IsOperator)
            {
                method.IsNonMemberOperator = true;
                method.Kind = CXXMethodKind.Operator;
            }

            function.ExplicitlyIgnore();

            @class.Methods.Add(method);

            Diagnostics.Debug($"Function {function.Name} moved to class {@class.Name}");

            return true;
        }

        private Class FindClassToMoveFunctionTo(Function function)
        {
            Class @class = null;
            if (function.IsOperator)
            {
                foreach (var param in function.Parameters)
                {
                    if (FunctionToInstanceMethodPass.GetClassParameter(param, out @class))
                        break;
                }
                if (@class == null)
                    function.ExplicitlyIgnore();
            }
            else
            {
                var tu = function.Namespace as TranslationUnit;
                string name = tu != null ? Options.GenerateFreeStandingFunctionsClassName(tu) :
                    function.Namespace.Name;
                @class = ASTContext.FindClass(
                    name, ignoreCase: true).FirstOrDefault(
                        c => c.TranslationUnit.Module == function.TranslationUnit.Module &&
                            !c.IsIncomplete);
            }

            return @class;
        }
    }
}
