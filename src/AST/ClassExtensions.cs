using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public static class ClassExtensions 
    {
        public static IEnumerable<Function> GetFunctionOverloads(this Class @class,
            Function function)
        {
            if (function.IsOperator)
                return @class.FindOperator(function.OperatorKind);
            return @class.Methods.Where(method => method.Name == function.Name);
        }

        public static IEnumerable<T> FindHierarchy<T>(this Class @class,
            Func<Class, IEnumerable<T>> func)
            where T : Declaration
        {
            foreach (var elem in func(@class))
                yield return elem;

            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass) continue;
                foreach(var elem in @base.Class.FindHierarchy(func))
                    yield return elem;
            }
        }

        public static Class GetNonIgnoredRootBase(this Class @class)
        {
            while (true)
            {
                if (!@class.HasNonIgnoredBase || @class.BaseClass == null)
                    return @class;

                @class = @class.BaseClass;
            }
        }

        public static Method GetBaseMethod(this Class @class, Method @override)
        {
            if (@class.BaseClass == null || @class.BaseClass.IsInterface)
                return null;

            var baseClass = @class.BaseClass.OriginalClass ?? @class.BaseClass;
            Method baseMethod = baseClass.GetBaseMethod(@override);
            if (baseMethod != null)
                return baseMethod;

            return baseClass.Methods.FirstOrDefault(@override.CanOverride);
        }

        public static Property GetBaseProperty(this Class @class, Property @override,
            bool onlyFirstBase = false, bool getTopmost = false)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.OriginalClass == @class ||
                    (onlyFirstBase && @base.Class.IsInterface))
                    continue;

                Property baseProperty;
                if (!getTopmost)
                {
                    baseProperty = @base.Class.GetBaseProperty(@override, onlyFirstBase);
                    if (baseProperty != null)
                        return baseProperty;
                }

                baseProperty = (from property in @base.Class.Properties
                    where property.OriginalName == @override.OriginalName &&
                        property.Parameters.SequenceEqual(@override.Parameters,
                            ParameterTypeComparer.Instance)
                    select property).FirstOrDefault();
                if (baseProperty != null)
                    return baseProperty;

                if (getTopmost)
                {
                    baseProperty = @base.Class.GetBaseProperty(@override, onlyFirstBase, true);
                    if (baseProperty != null)
                        return baseProperty;
                }
            }
            return null;
        }

        public static bool HasNonAbstractBasePropertyInPrimaryBase(this Class @class, Property property)
        {
            var baseProperty = @class.GetBaseProperty(property, true, true);
            return baseProperty != null && !baseProperty.IsPure &&
                !((Class) baseProperty.OriginalNamespace).IsInterface;
        }

        public static Property GetPropertyByName(this Class @class, string propertyName)
        {
            Property property = @class.Properties.FirstOrDefault(m => m.Name == propertyName);
            if (property != null)
                return property;

            foreach (var baseClassSpecifier in @class.Bases.Where(
                b => b.Type.IsClass() && b.Class.IsDeclared))
            {
                property = baseClassSpecifier.Class.GetPropertyByName(propertyName);
                if (property != null)
                    return property;
            }
            return null;
        }

        public static Property GetPropertyByConstituentMethod(this Class @class, Method method)
        {
            var property = @class.Properties.FirstOrDefault(p => p.GetMethod == method);
            if (property != null)
                return property;
            property = @class.Properties.FirstOrDefault(p => p.SetMethod == method);
            if (property != null)
                return property;

            foreach (BaseClassSpecifier @base in @class.Bases.Where(b => b.Type.IsClass()))
            {
                property = @base.Class.GetPropertyByConstituentMethod(method);
                if (property != null)
                    return property;
            }
            return null;
        }

        public static bool HasRefBase(this Class @class)
        {
            Class @base = null;

            if (@class.HasBaseClass)
                @base = @class.Bases[0].Class;

            var hasRefBase = @base != null && @base.IsRefType && @base.IsGenerated;

            return hasRefBase;
        }

        public static IEnumerable<TranslationUnit> GetGenerated(this IEnumerable<TranslationUnit> units)
        {
            return units.Where(u => u.IsGenerated && (u.HasDeclarations || u.IsSystemHeader) && u.IsValid);
        }

        public static List<ClassTemplateSpecialization> GetSpecializationsToGenerate(
            this Class classTemplate)
        {
            if (classTemplate.Fields.Any(
                f => f.Type.Desugar() is TemplateParameterType))
                return classTemplate.Specializations;

            var specializations = new List<ClassTemplateSpecialization>();
            var specialization = classTemplate.Specializations.FirstOrDefault(s => !s.Ignore);
            if (specialization == null)
                specializations.Add(classTemplate.Specializations[0]);
            else
                specializations.Add(specialization);
            return specializations;
        }
    }
}