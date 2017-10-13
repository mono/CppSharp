using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class MarkUsedClassInternalsPass : TranslationUnitPass
    {
        public MarkUsedClassInternalsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
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

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || @class.Ignore || @class.IsDependent)
                return false;

            MarkUsedFieldTypes(@class, new HashSet<Class>());

            return true;
        }

        private static void MarkUsedFieldTypes(Class @class, HashSet<Class> visitedClasses)
        {
            if (visitedClasses.Contains(@class))
                return;

            visitedClasses.Add(@class);

            Class type = null;
            foreach (var field in @class.Layout.Fields.Where(
                f => f.QualifiedType.Type.TryGetClass(out type)))
            {
                DeclarationContext declarationContext = type;
                do
                {
                    if (declarationContext.Ignore)
                    {
                        declarationContext.GenerationKind = GenerationKind.Internal;

                        var specialization = declarationContext as ClassTemplateSpecialization;
                        if (specialization?.TemplatedDecl.TemplatedClass.Ignore == true)
                            specialization.TemplatedDecl.TemplatedClass.GenerationKind = GenerationKind.Internal;
                    }
                    declarationContext = declarationContext.Namespace;
                } while (declarationContext != null);

                MarkUsedFieldTypes(type, visitedClasses);
            }
        }
    }
}
