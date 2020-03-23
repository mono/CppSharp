using System.Collections.Generic;
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

        public override bool VisitASTContext(ASTContext context)
        {
            bool result = base.VisitASTContext(context);
            foreach (Function movedFunction in movedFunctions)
                movedFunction.OriginalNamespace.Declarations.Remove(movedFunction);
            return result;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated)
                return false;

            Class @class = FindClassToMoveFunctionTo(function);

            if (@class == null)
                return false;

            function.Namespace = @class;
            @class.Declarations.Add(function);
            movedFunctions.Add(function);

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
                string name = (function.Namespace as TranslationUnit)?.FileNameWithoutExtension ??
                    function.Namespace.Name;
                @class = ASTContext.FindClass(
                    name, ignoreCase: true).FirstOrDefault(
                        c => c.TranslationUnit.Module == function.TranslationUnit.Module &&
                            !c.IsIncomplete);
            }

            return @class;
        }

        private HashSet<Function> movedFunctions = new HashSet<Function>();
    }
}
