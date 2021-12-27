using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Utils;

namespace CppSharp.Passes
{
    public class TrimSpecializationsPass : TranslationUnitPass
    {
        public TrimSpecializationsPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassFields | VisitFlags.ClassMethods |
            VisitFlags.ClassProperties | VisitFlags.NamespaceVariables);

        public override bool VisitASTContext(ASTContext context)
        {
            var result = base.VisitASTContext(context);
            foreach (var template in templates)
                CleanSpecializations(template);
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore && !base.VisitClassDecl(@class))
                return false;

            if (@class.IsTemplate)
            {
                templates.Add(@class);
                foreach (var specialization in @class.Specializations.Where(
                    s => s.IsExplicitlyGenerated))
                {
                    specialization.Visit(this);
                    foreach (var type in from a in specialization.Arguments
                                         where a.Type.Type != null
                                         select a.Type.Type.Desugar())
                        CheckForInternalSpecialization(specialization, type);
                }
            }
            else
                CheckBasesForSpecialization(@class);

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            if (function.IsGenerated)
            {
                ASTUtils.CheckTypeForSpecialization(function.OriginalReturnType.Type,
                    function, AddSpecialization, Context.TypeMaps);
                foreach (var parameter in function.Parameters)
                    ASTUtils.CheckTypeForSpecialization(parameter.Type, function,
                        AddSpecialization, Context.TypeMaps);
            }

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!base.VisitDeclaration(field))
                return false;

            if (field.Access == AccessSpecifier.Private)
            {
                CheckForInternalSpecialization(field, field.Type);
                return true;
            }

            if (!ASTUtils.CheckTypeForSpecialization(field.Type,
                    field, AddSpecialization, Context.TypeMaps))
                CheckForInternalSpecialization(field, field.Type);

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!base.VisitVariableDecl(variable))
                return false;

            if (variable.Access == AccessSpecifier.Public)
            {
                ASTUtils.CheckTypeForSpecialization(variable.Type,
                    variable, AddSpecialization, Context.TypeMaps);
                return true;
            }

            return true;
        }

        private void AddSpecialization(ClassTemplateSpecialization specialization)
        {
            if (specializations.Contains(specialization))
                return;
            internalSpecializations.Remove(specialization);
            specializations.Add(specialization);
            foreach (var field in specialization.Fields)
                field.Visit(this);
            foreach (var method in specialization.Methods)
                method.Visit(this);
        }

        private void CleanSpecializations(Class template)
        {
            template.Specializations.RemoveAll(s =>
                {
                    if (s.SpecializationKind == TemplateSpecializationKind.Undeclared ||
                        (!specializations.Contains(s) && !internalSpecializations.Contains(s)))
                    {
                        s.ExplicitlyIgnore();
                        return true;
                    }
                    return false;
                });

            foreach (var specialization in template.Specializations.Where(
                s => s is ClassTemplatePartialSpecialization))
                specialization.ExplicitlyIgnore();

            foreach (var specialization in template.Specializations.Where(
                s => !s.IsExplicitlyGenerated && !s.Ignore && internalSpecializations.Contains(s)))
                specialization.GenerationKind = GenerationKind.Internal;

            for (int i = template.Specializations.Count - 1; i >= 0; i--)
            {
                var specialization = template.Specializations[i];
                if (specialization is ClassTemplatePartialSpecialization)
                    template.Specializations.RemoveAt(i);
            }

            if (!template.IsExplicitlyGenerated &&
                template.Specializations.All(s => s.Ignore))
                template.ExplicitlyIgnore();

            if (template.Specializations.Any() &&
                (template.HasDependentValueFieldInLayout() ||
                 template.Classes.Any(c => c.HasDependentValueFieldInLayout())))
                TryMoveExternalSpecializations(template);
        }

        /// <summary>
        /// Moves specializations which use in their arguments types located outside
        /// the library their template is located in, to the module of said external types.
        /// </summary>
        /// <param name="template">The template to check for external specializations.</param>
        private static void TryMoveExternalSpecializations(Class template)
        {
            for (int i = template.Specializations.Count - 1; i >= 0; i--)
            {
                var specialization = template.Specializations[i];
                if (specialization.Ignore)
                {
                    continue;
                }
                Module module = GetExternalModule(specialization);
                if (module != null)
                {
                    module.ExternalClassTemplateSpecializations.Add(specialization);
                    template.Specializations.RemoveAt(i);
                }
            }
        }

        private static Module GetExternalModule(ClassTemplateSpecialization specialization)
        {
            Module currentModule = specialization.TemplatedDecl.TemplatedClass.TranslationUnit.Module;
            List<Module> modules = new List<Module>();
            foreach (TemplateArgument arg in specialization.Arguments.Where(arg => arg.Type.Type != null))
            {
                if (ASTUtils.IsTypeExternal(currentModule, arg.Type.Type))
                {
                    Module module = arg.Type.Type.GetModule();
                    if (module != null)
                        modules.Add(module);
                }
                if (arg.Type.Type.TryGetDeclaration(out ClassTemplateSpecialization nestedSpecialization))
                {
                    Module module = GetExternalModule(nestedSpecialization);
                    if (module != null)
                        modules.Add(module);
                }
            }
            return modules.TopologicalSort(m => m.Dependencies).LastOrDefault();
        }

        private void CheckForInternalSpecialization(Declaration container, AST.Type type)
        {
            ASTUtils.CheckTypeForSpecialization(type, container,
                specialization =>
                {
                    if (!specializations.Contains(specialization))
                    {
                        internalSpecializations.Add(specialization);
                        CheckLayoutFields(specialization);
                    }
                }, Context.TypeMaps, true);
        }

        private void CheckLayoutFields(Class @class)
        {
            foreach (var field in @class.Fields)
                field.Visit(this);
            foreach (var @base in from @base in @class.Bases
                                  where @base.IsClass
                                  select @base.Class)
                CheckLayoutFields(@base);
        }

        private void CheckBasesForSpecialization(Class @class)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass))
            {
                var specialization = @base.Class as ClassTemplateSpecialization;
                if (specialization != null &&
                    !ASTUtils.CheckTypeForSpecialization(@base.Type, @class,
                        AddSpecialization, Context.TypeMaps))
                    CheckForInternalSpecialization(@class, @base.Type);
                CheckBasesForSpecialization(@base.Class);
            }
        }

        private HashSet<ClassTemplateSpecialization> specializations = new HashSet<ClassTemplateSpecialization>();
        private HashSet<ClassTemplateSpecialization> internalSpecializations = new HashSet<ClassTemplateSpecialization>();
        private HashSet<Class> templates = new HashSet<Class>();
    }
}
