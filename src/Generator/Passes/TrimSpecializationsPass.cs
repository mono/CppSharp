using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
using CppSharp.Utils;

namespace CppSharp.Passes
{
    public class TrimSpecializationsPass : TranslationUnitPass
    {
        public TrimSpecializationsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassTemplateSpecializations = false;
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
            var result = base.VisitASTContext(context);
            foreach (var template in templates)
                CleanSpecializations(template);
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
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

            TypeMap typeMap;
            if (!Context.TypeMaps.FindTypeMap(field.Type, out typeMap) &&
                !ASTUtils.CheckTypeForSpecialization(field.Type,
                    field, AddSpecialization, Context.TypeMaps))
                CheckForInternalSpecialization(field, field.Type);

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
                s => !s.IsExplicitlyGenerated && internalSpecializations.Contains(s)))
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

            if (template.Fields.Any(f => f.Type.Desugar() is TemplateParameterType))
                MoveExternalSpecializations(template);
        }

        /// <summary>
        /// Moves specializations which use in their arguments types located outside
        /// the library their template is located in, to the module of said external types.
        /// </summary>
        /// <param name="template">The template to check for external specializations.</param>
        private static void MoveExternalSpecializations(Class template)
        {
            for (int i = template.Specializations.Count - 1; i >= 0; i--)
            {
                var specialization = template.Specializations[i];
                var modules = (from arg in specialization.Arguments
                               where arg.Type.Type != null
                                   && ASTUtils.IsTypeExternal(
                                          template.TranslationUnit.Module, arg.Type.Type)
                               let module = arg.Type.Type.GetModule()
                               where module != null
                               select module).ToList().TopologicalSort(m => m.Dependencies);
                if (modules.Any())
                {
                    var module = modules.Last();
                    module.ExternalClassTemplateSpecializations.Add(specialization);
                    template.Specializations.RemoveAt(i);
                }
            }
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
