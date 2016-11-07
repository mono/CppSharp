using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class StripUnusedSystemTypesPass : TranslationUnitPass
    {
        public StripUnusedSystemTypesPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            // we need this one for marshalling std::string
            foreach (var allocator in context.FindClass("allocator", false, true).Where(
                a => a.Namespace.Name == "std"))
                usedStdTypes.Add(allocator);

            var result = base.VisitASTContext(context);

            foreach (var unit in Options.SystemModule.Units)
                RemoveUnusedStdTypes(unit);

            return result;
        }

        public override bool VisitFieldDecl(Field field)
        {
            var result = base.VisitFieldDecl(field);

            var desugared = field.Type.Desugar();
            var tagType = desugared as TagType;
            if (tagType != null)
            {
                var specialization = tagType.Declaration as ClassTemplateSpecialization;
                if (specialization != null)
                {
                    MarkAsUsed(specialization.TemplatedDecl);
                    MarkAsUsed(specialization.TemplatedDecl.TemplatedDecl);
                }
                else
                {
                    MarkAsUsed(tagType.Declaration);
                }
            }
            else
            {
                var template = desugared as TemplateSpecializationType;
                if (template != null)
                {
                    MarkAsUsed(template.Template);
                    MarkAsUsed(template.Template.TemplatedDecl);
                }
            }

            return result;
        }

        private void MarkAsUsed(Declaration declaration)
        {
            while (declaration != null && !(declaration is Namespace))
            {
                usedStdTypes.Add(declaration);
                declaration = declaration.Namespace;
            }
        }

        private void RemoveUnusedStdTypes(DeclarationContext context)
        {
            for (int i = context.Declarations.Count - 1; i >= 0; i--)
            {
                var declaration = context.Declarations[i];
                var nestedContext = declaration as Namespace;
                if (nestedContext != null)
                    RemoveUnusedStdTypes(nestedContext);
                else if (!this.usedStdTypes.Contains(declaration))
                    context.Declarations.RemoveAt(i);
            }
        }

        private HashSet<Declaration> usedStdTypes = new HashSet<Declaration>();
    }
}
