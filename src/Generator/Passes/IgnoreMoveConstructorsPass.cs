using CppSharp.AST;
using CppSharp.Passes;

namespace CppSharp.Passes
{
    public class IgnoreMoveConstructorsPass : TranslationUnitPass
    {
        public IgnoreMoveConstructorsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method))
                return false;

            if (method.IsMoveConstructor)
            {
                method.ExplicitlyIgnore();
                return true;
            }

            return false;
        }
    }
}
