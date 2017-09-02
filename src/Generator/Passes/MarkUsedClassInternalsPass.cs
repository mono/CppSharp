using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class MarkUsedClassInternalsPass : TranslationUnitPass
    {
        public MarkUsedClassInternalsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!base.VisitDeclaration(field))
                return false;

            Class @class;
            if (field.Type.TryGetClass(out @class) && @class.Ignore)
            {
                DeclarationContext declarationContext = @class;
                do
                {
                    if (declarationContext.Ignore)
                        declarationContext.GenerationKind = GenerationKind.Internal;
                    declarationContext = declarationContext.Namespace;
                } while (declarationContext != null);

                var specialization = @class as ClassTemplateSpecialization;
                if (specialization?.TemplatedDecl.TemplatedClass.Ignore == true)
                    specialization.TemplatedDecl.TemplatedClass.GenerationKind = GenerationKind.Internal;
            }

            return true;
        }
    }
}
