using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class StripUnusedSystemTypesPass : TranslationUnitPass
    {
        public StripUnusedSystemTypesPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassFields | VisitFlags.ClassTemplateSpecializations |
            VisitFlags.NamespaceTemplates | VisitFlags.NamespaceTypedefs);

        public override bool VisitASTContext(ASTContext context)
        {
            // we need this one for marshalling std::string
            foreach (var name in new[] { "allocator", "char_traits" })
                foreach (var usedStdType in context.FindClass(name, false, true).Where(
                    a => a.TranslationUnit.IsSystemHeader))
                    usedStdTypes.Add(usedStdType);

            var result = base.VisitASTContext(context);

            foreach (var unit in Options.SystemModule.Units)
                RemoveUnusedStdTypes(unit);

            return result;
        }

        public override bool VisitFieldDecl(Field field)
        {
            var desugared = field.Type.Desugar();

            if (TryMarkType(desugared))
                return true;

            var arrayType = desugared as ArrayType;
            return arrayType != null && TryMarkType(arrayType.Type.Desugar());
        }

        private bool TryMarkType(Type desugared)
        {
            var templateType = desugared as TemplateSpecializationType;
            var tagType = desugared as TagType ?? templateType?.Desugared.Type as TagType;
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
                return true;
            }

            if (templateType != null)
            {
                var template = templateType.Template;
                if (template.TemplatedDecl is TypeAlias typeAlias &&
                    typeAlias.Type.Desugar() is TemplateSpecializationType specializationType)
                {
                    MarkAsUsed(template);
                    MarkAsUsed(template.TemplatedDecl);
                    template = specializationType.Template;
                }
                MarkAsUsed(template);
                MarkAsUsed(template.TemplatedDecl);
                return true;
            }

            return false;
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
                else if (!this.usedStdTypes.Contains(declaration) &&
                    !declaration.IsExplicitlyGenerated)
                    context.Declarations.RemoveAt(i);
            }
        }

        private readonly HashSet<Declaration> usedStdTypes = new HashSet<Declaration>();
    }
}
