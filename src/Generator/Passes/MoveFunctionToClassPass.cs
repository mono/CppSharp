using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MoveFunctionToClassPass : TranslationUnitPass
    {
        public MoveFunctionToClassPass()
        {
            VisitOptions.VisitClassBases = VisitOptions.VisitClassFields =
            VisitOptions.VisitClassMethods = VisitOptions.VisitClassProperties =
            VisitOptions.VisitClassTemplateSpecializations = VisitOptions.VisitEventParameters =
            VisitOptions.VisitFunctionParameters = VisitOptions.VisitFunctionReturnType =
            VisitOptions.VisitNamespaceEnums = VisitOptions.VisitNamespaceEvents =
            VisitOptions.VisitNamespaceTemplates = VisitOptions.VisitNamespaceTypedefs =
            VisitOptions.VisitNamespaceVariables = VisitOptions.VisitPropertyAccessors =
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated)
                return false;

            Class @class = FindClassToMoveFunctionTo(function);

            if (@class == null ||
                @class.TranslationUnit.Module != function.TranslationUnit.Module)
                return false;

            // Create a new fake method so it acts as a static method.
            var method = new Method(function)
            {
                Namespace = @class,
                OperatorKind = function.OperatorKind,
                OriginalFunction = null,
                IsStatic = true
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
                var unit = function.Namespace as TranslationUnit;
                @class = unit == null
                    ? ASTContext.FindClass(
                        function.Namespace.Name, ignoreCase: true).FirstOrDefault()
                    : ASTContext.FindCompleteClass(
                        unit.FileNameWithoutExtension.ToLowerInvariant(), true);
            }

            return @class;
        }
    }
}
