using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class ResolveIncompleteDeclsPass : TranslationUnitPass
    {
        public ResolveIncompleteDeclsPass()
        {
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitClassMethods = false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            EnsureCompleteDeclaration(@class);

            if (@class.Namespace is ClassTemplateSpecialization &&
                @class.IsIncomplete && @class.CompleteDeclaration == null &&
                @class.IsGenerated)
                @class.GenerationKind = GenerationKind.Internal;

            return true;
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!base.VisitClassTemplateDecl(template))
                return false;

            bool complete = !template.TemplatedDecl.IsIncomplete;
            EnsureCompleteDeclaration(template.TemplatedDecl);

            template.TemplatedDecl = template.TemplatedDecl.CompleteDeclaration ?? template.TemplatedDecl;
            Class templatedClass = template.TemplatedClass;
            var parentSpecialization = templatedClass.Namespace as ClassTemplateSpecialization;
            if (parentSpecialization != null)
                templatedClass = parentSpecialization.TemplatedDecl.TemplatedClass.Classes.FirstOrDefault(
                    c => c.OriginalName == template.OriginalName) ?? template.TemplatedClass;
            // store all specializations in the real template class because ClassTemplateDecl only forwards
            foreach (var specialization in template.Specializations.Where(
                s => !templatedClass.Specializations.Contains(s)))
            {
                templatedClass.Specializations.Add(specialization);

                // TODO: Move this to the AST converter layer?
                if (specialization.IsAbstract)
                    templatedClass.IsAbstract = true;
            }

            if (templatedClass.TemplateParameters.Count == 0 || complete)
            {
                templatedClass.TemplateParameters.Clear();
                templatedClass.TemplateParameters.AddRange(template.Parameters);
            }

            return true;
        }

        public override bool VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            if (!base.VisitClassTemplateSpecializationDecl(specialization))
                return false;

            if (specialization.IsIncomplete &&
                specialization.CompleteDeclaration == null && specialization.IsGenerated)
                specialization.GenerationKind = GenerationKind.Internal;

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            if (!@enum.IsIncomplete)
                goto Out;

            if (@enum.CompleteDeclaration != null)
                goto Out;

            @enum.CompleteDeclaration =
                ASTContext.FindCompleteEnum(@enum.QualifiedName);

            if (@enum.CompleteDeclaration == null)
            {
                @enum.GenerationKind = GenerationKind.Internal;
                Diagnostics.Warning("Unresolved declaration: {0}", @enum.Name);
            }

        Out:

            return base.VisitEnumDecl(@enum);
        }

        private void EnsureCompleteDeclaration(Declaration declaration)
        {
            if (!declaration.IsIncomplete)
                return;

            if (declaration.CompleteDeclaration != null)
                return;

            declaration.CompleteDeclaration =
                ASTContext.FindCompleteClass(declaration.QualifiedName);

            if (declaration.CompleteDeclaration != null)
                return;

            if (Context.ParserOptions.UnityBuild)
            {
                if (declaration.IsGenerated)
                    foreach (var redecl in declaration.Redeclarations)
                        redecl.GenerationKind = GenerationKind.None;
                return;
            }

            var @class = declaration as Class;
            if (CheckForDuplicateForwardClass(@class))
                return;

            if (declaration.IsGenerated)
                declaration.GenerationKind = GenerationKind.Internal;
            Diagnostics.Debug("Unresolved declaration: {0}",
                declaration.Name);
        }

        bool CheckForDuplicateForwardClass(Class @class)
        {
            var redecls = @class?.Redeclarations;
            if (@class != null && @class.IsOpaque)
            {
                if (redecls.Count == 0 ||
                   (redecls.Last() == @class && !redecls.Exists(decl => !decl.IsIncomplete)))
                    return true;

                duplicateClasses.Add(@class);
            }

            return false;
        }

        public override bool VisitASTContext(ASTContext c)
        {
            base.VisitASTContext(c);

            foreach (var duplicateClass in duplicateClasses)
                duplicateClass.Namespace.Declarations.Remove(duplicateClass);

            return true;
        }

        private HashSet<Declaration> duplicateClasses = new HashSet<Declaration>();
    }
}
