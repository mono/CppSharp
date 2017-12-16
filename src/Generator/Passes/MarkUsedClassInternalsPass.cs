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

            MarkUsedFieldTypes(@class, new HashSet<DeclarationContext>());

            return true;
        }

        private static void MarkUsedFieldTypes(DeclarationContext declContext,
            HashSet<DeclarationContext> visitedDeclarationContexts)
        {
            if (visitedDeclarationContexts.Contains(declContext))
                return;

            visitedDeclarationContexts.Add(declContext);

            DeclarationContext decl = null;
            var @class = declContext as Class;
            if (@class == null)
                return;

            foreach (var field in @class.Layout.Fields.Where(
                f => f.QualifiedType.Type.TryGetDeclaration(out decl)))
            {
                DeclarationContext declarationContext = decl;
                do
                {
                    if (declarationContext.Ignore)
                        declarationContext.GenerationKind = GenerationKind.Internal;

                    var specialization = declarationContext as ClassTemplateSpecialization;
                    Class template = specialization?.TemplatedDecl.TemplatedClass;
                    if (template?.Ignore == true)
                        template.GenerationKind = GenerationKind.Internal;

                    Class nested = template?.Classes.Find(c => c.OriginalName == decl.OriginalName);
                    if (nested?.Ignore == true)
                        nested.GenerationKind = GenerationKind.Internal;

                    declarationContext = declarationContext.Namespace;
                } while (declarationContext != null);

                MarkUsedFieldTypes(decl, visitedDeclarationContexts);
            }
        }
    }
}
