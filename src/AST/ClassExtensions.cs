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

            var methods = baseClass.Methods.Concat(baseClass.Declarations.OfType<Method>());
            return methods.FirstOrDefault(@override.CanOverride);
        }

        public static Property GetBaseProperty(this Class @class, Property @override)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass))
            {
                Class baseClass = @base.Class.OriginalClass ?? @base.Class;
                Property baseProperty = baseClass.Properties.Find(p =>
                    (@override.GetMethod?.IsOverride == true &&
                     @override.GetMethod.BaseMethod == p.GetMethod) ||
                    (@override.SetMethod?.IsOverride == true &&
                     @override.SetMethod.BaseMethod == p.SetMethod) ||
                    (@override.Field != null && @override.Field == p.Field));
                if (baseProperty != null)
                    return baseProperty;

                baseProperty = GetBaseProperty(@base.Class, @override);
                if (baseProperty != null)
                    return baseProperty;
            }
            return null;
        }

        public static Property GetBasePropertyByName(this Class @class, Property @override,
            bool onlyFirstBase = false)
        {
            foreach (var @base in @class.Bases)
            {
                if (@base.Ignore) continue;

                if (!@base.IsClass || @base.Class.OriginalClass == @class ||
                    (onlyFirstBase && @base.Class.IsInterface))
                    continue;

                var properties = @base.Class.Properties.Concat(@base.Class.Declarations.OfType<Property>());
                Property baseProperty = (from property in properties
                                         where property.OriginalName == @override.OriginalName &&
                                             property.Parameters.SequenceEqual(@override.Parameters,
                                                 ParameterTypeComparer.Instance)
                                         select property).FirstOrDefault();

                if (baseProperty != null)
                    return baseProperty;

                baseProperty = @base.Class.GetBasePropertyByName(@override, onlyFirstBase);
                if (baseProperty != null)
                    return baseProperty;
            }
            return null;
        }

        public static Property GetPropertyByName(this Class @class, string propertyName)
        {
            Property property = @class.Properties.Find(m => m.Name == propertyName);
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
            var property = @class.Properties.Find(p => p.GetMethod == method);
            if (property != null)
                return property;
            property = @class.Properties.Find(p => p.SetMethod == method);
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
                @base = @class.BaseClass;

            return @base?.IsRefType == true && @base.IsGenerated;
        }

        public static IEnumerable<TranslationUnit> GetGenerated(this IEnumerable<TranslationUnit> units)
        {
            return units.Where(u => u.IsGenerated && (u.HasDeclarations || u.IsSystemHeader) && u.IsValid);
        }

        public static IEnumerable<Class> GetSpecializedClassesToGenerate(
            this Class dependentClass)
        {
            IEnumerable<Class> specializedClasses = GetSpecializedClassesOf(dependentClass);
            if (!specializedClasses.Any() || dependentClass.HasDependentValueFieldInLayout())
                return specializedClasses;

            var specializations = new List<Class>();
            var specialization = specializedClasses.FirstOrDefault(s => s.IsGenerated);
            if (specialization == null)
                specializations.Add(specializedClasses.First());
            else
                specializations.Add(specialization);
            return specializations;
        }

        private static IEnumerable<Class> GetSpecializedClassesOf(this Class dependentClass)
        {
            if (dependentClass.IsTemplate)
                return dependentClass.Specializations;

            Class template = dependentClass.Namespace as Class;
            if (template == null || !template.IsTemplate)
                // just one level of nesting supported for the time being
                return Enumerable.Empty<Class>();

            return template.Specializations.SelectMany(s => s.Classes.Where(
                c => c.Name == dependentClass.Name)).ToList();
        }

        public static Class GetInterface(this Class @class)
        {
            var specialization = @class as ClassTemplateSpecialization;
            Class @interface = null;
            if (specialization == null)
            {
                @interface = @class.Namespace.Classes.FirstOrDefault(
                    c => c.OriginalClass == @class && c.IsInterface);
            }
            else
            {
                Class template = specialization.TemplatedDecl.TemplatedClass;
                Class templatedInterface = @class.Namespace.Classes.FirstOrDefault(
                   c => c.OriginalClass == template && c.IsInterface);
                if (templatedInterface != null)
                    @interface = templatedInterface.Specializations.FirstOrDefault(
                        s => s.OriginalClass == specialization && s.IsInterface);
            }

            return @interface;
        }

        public static bool HasDependentValueFieldInLayout(this Class @class)
        {
            if (@class.Fields.Any(f => IsValueDependent(f.Type)))
                return true;

            return @class.Bases.Where(b => b.IsClass).Select(
                b => b.Class).Any(HasDependentValueFieldInLayout);
        }

        public static int GetSize(this ClassLayout layout) =>
            /// There's at least one ABI (System V) that gives to empty structs
            /// size 1 in C++ and size 0 in C. The former causes crashes in older versions of Mono.
            layout.Size == 1 && layout.DataSize == 0 ? 0 : layout.Size;

        private static bool IsValueDependent(Type type)
        {
            var desugared = type.Desugar();
            if (desugared is TemplateParameterType)
                return true;
            var tagType = desugared as TagType;
            if (tagType?.IsDependent == true)
                return true;
            var templateType = desugared as TemplateSpecializationType;
            if (templateType?.Arguments.Any(
                a => a.Type.Type?.Desugar().IsDependent == true) == true)
                return true;
            var arrayType = desugared as ArrayType;
            return arrayType != null && IsValueDependent(arrayType.Type);
        }
    }
}