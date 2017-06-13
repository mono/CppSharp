using System.Linq;
using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Passes
{
    public class ResolveIncompleteDeclsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;
            
            EnsureCompleteDeclaration(@class);

            return true;
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!base.VisitClassTemplateDecl(template))
                return false;

            EnsureCompleteDeclaration(template.TemplatedDecl);

            if (template.Redeclarations.Count > 0 && template.TemplatedDecl.IsIncomplete && template.TemplatedDecl.CompleteDeclaration == null)
            {
                Diagnostics.Message($"Template {template} has {template.Redeclarations.Count} redeclarations");
                var contains = 
                     template.Redeclarations.Find(decl => !(decl as ClassTemplate).TemplatedClass.IsIncomplete);
                Diagnostics.Message($"Template {template}, has full decl {contains}");
                if (contains != null)
                {
                    var parent = (contains as ClassTemplate).TemplatedClass;
                    foreach (var specialization in template.Specializations.Where(
                        s => !s.IsIncomplete && !parent.Specializations.Contains(s)))
                        parent.Specializations.Add(specialization);
                    if (parent.TemplateParameters.Count == 0)
                        parent.TemplateParameters.AddRange(template.Parameters);
                }
                else
                {
                    template.TemplatedDecl = template.TemplatedDecl.CompleteDeclaration ?? template.TemplatedDecl;
                    // store all spesializations in the real template class because ClassTemplateDecl only forwards
                    foreach (var specialization in template.Specializations.Where(
                        s => !s.IsIncomplete && !template.TemplatedClass.Specializations.Contains(s)))
                        template.TemplatedClass.Specializations.Add(specialization);

                    if (template.TemplatedClass.TemplateParameters.Count == 0)
                        template.TemplatedClass.TemplateParameters.AddRange(template.Parameters);
                }
            }
            else
            {
                template.TemplatedDecl = template.TemplatedDecl.CompleteDeclaration ?? template.TemplatedDecl;
                // store all spesializations in the real template class because ClassTemplateDecl only forwards
                foreach (var specialization in template.Specializations.Where(
                    s => !s.IsIncomplete && !template.TemplatedClass.Specializations.Contains(s)))
                    template.TemplatedClass.Specializations.Add(specialization);

                if (template.TemplatedClass.TemplateParameters.Count == 0)
                    template.TemplatedClass.TemplateParameters.AddRange(template.Parameters);

            }

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

            var @class = declaration as Class;
            var redecls = @class?.Redeclarations;
            if (@class != null && @class.IsOpaque)
            {
                if (redecls.Count == 0 ||
                   (redecls.Last() == @class && !redecls.Exists(decl => !decl.IsIncomplete)))
                    return;
                duplicateClasses.Add(@class);
            }
                
            
            declaration.CompleteDeclaration =
                ASTContext.FindCompleteClass(declaration.QualifiedName);

            if (declaration.CompleteDeclaration == null)
            {
                declaration.GenerationKind = GenerationKind.Internal;
                Diagnostics.Debug("Unresolved declaration: {0}",
                    declaration.Name);
            }
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
