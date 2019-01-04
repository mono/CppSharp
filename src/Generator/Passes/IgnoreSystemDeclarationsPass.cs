﻿using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class IgnoreSystemDeclarationsPass : TranslationUnitPass
    {
        public IgnoreSystemDeclarationsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassTemplateSpecializations = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsValid)
                return false;

            if (ClearVisitedDeclarations)
                Visited.Clear();

            VisitDeclarationContext(unit);

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || Options.IsCLIGenerator)
                return false;

            if (!@class.TranslationUnit.IsSystemHeader)
                return false;

            @class.ExplicitlyIgnore();

            if (!@class.IsDependent || @class.Specializations.Count == 0)
                return false;

            foreach (var specialization in @class.Specializations)
                specialization.ExplicitlyIgnore();

            // we only need a few members for marshalling so strip the rest
            switch (@class.Name)
            {
                case "basic_string":
                    foreach (var method in @class.Methods.Where(m => !m.IsDestructor &&
                        m.OriginalName != "c_str" && m.OriginalName != "assign"))
                        method.ExplicitlyIgnore();
                    foreach (var basicString in GetCharSpecializations(@class))
                    {
                        basicString.GenerationKind = GenerationKind.Generate;
                        foreach (var method in basicString.Methods)
                        {
                            if (method.IsDestructor || method.OriginalName == "c_str" ||
                                (method.OriginalName == "assign" &&
                                 method.Parameters.Count == 1 &&
                                 method.Parameters[0].Type.IsPointerToPrimitiveType()) ||
                                (method.IsConstructor &&
                                 !method.Parameters.Where(p => p.Kind == ParameterKind.Regular).Any()))
                            {
                                method.GenerationKind = GenerationKind.Generate;
                                method.Namespace.GenerationKind = GenerationKind.Generate;
                                method.InstantiatedFrom.GenerationKind = GenerationKind.Generate;
                                method.InstantiatedFrom.Namespace.GenerationKind = GenerationKind.Generate;
                            }
                            else
                            {
                                method.ExplicitlyIgnore();
                            }
                        }
                    }
                    break;
                case "allocator":
                    foreach (var method in @class.Methods)
                        method.ExplicitlyIgnore();
                    foreach (var allocator in GetCharSpecializations(@class))
                    {
                        foreach (var method in allocator.Methods)
                            method.ExplicitlyIgnore();
                        allocator.GenerationKind = GenerationKind.Generate;
                        allocator.TemplatedDecl.TemplatedDecl.GenerationKind = GenerationKind.Generate;
                        allocator.GenerationKind = GenerationKind.Generate;
                    }
                    break;
                case "char_traits":
                    foreach (var method in @class.Methods)
                        method.ExplicitlyIgnore();
                    foreach (var charTraits in GetCharSpecializations(@class))
                    {
                        foreach (var method in charTraits.Methods)
                            method.ExplicitlyIgnore();
                        charTraits.GenerationKind = GenerationKind.Generate;
                        charTraits.TemplatedDecl.TemplatedDecl.GenerationKind = GenerationKind.Generate;
                    }
                    break;
            }
            return true;
        }

        private static IEnumerable<ClassTemplateSpecialization> GetCharSpecializations(Class @class)
        {
            return @class.Specializations.Where(s =>
                s.Arguments[0].Type.Type.Desugar().IsPrimitiveType(PrimitiveType.Char));
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            if (@enum.TranslationUnit.IsSystemHeader)
                @enum.ExplicitlyIgnore();

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            if (function.TranslationUnit.IsSystemHeader)
                function.ExplicitlyIgnore();

            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!base.VisitTypedefDecl(typedef))
                return false;

            if (typedef.TranslationUnit.IsSystemHeader)
                typedef.ExplicitlyIgnore();

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!base.VisitDeclaration(variable))
                return false;

            if (variable.TranslationUnit.IsSystemHeader)
                variable.ExplicitlyIgnore();

            return true;
        }
    }
}
