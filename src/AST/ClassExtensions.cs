using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public static class ClassExtensions 
    {
        public static IEnumerable<Method> FindOperator(this Class @class,
            CXXOperatorKind kind)
        {
            return @class.Operators.Where(method => method.OperatorKind == kind);
        }

        public static IEnumerable<Method> FindMethodByOriginalName(this Class @class,
            string name)
        {
            return @class.Methods.Where(method => method.OriginalName == name);
        }

        public static IEnumerable<Variable> FindVariableByOriginalName(this Class @class,
            string originalName)
        {
            return @class.Variables.Where(v => v.OriginalName == originalName);
        }

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

        public static Method GetBaseMethod(this Class @class, Method @override,
            bool onlyPrimaryBase = false, bool getTopmost = false)
        {
            foreach (var @base in @class.Bases.Where(
                b => b.IsClass && b.Class.OriginalClass != @class && (!onlyPrimaryBase || !b.Class.IsInterface)))
            {
                Method baseMethod;
                if (!getTopmost)
                {
                    baseMethod = @base.Class.GetBaseMethod(@override, onlyPrimaryBase);
                    if (baseMethod != null)
                        return baseMethod;
                }

                baseMethod = (from method in @base.Class.Methods
                    where @override.CanOverride(method)
                    select method).FirstOrDefault();
                if (baseMethod != null)
                    return baseMethod;

                if (getTopmost)
                {
                    baseMethod = (@base.Class.GetBaseMethod(@override, onlyPrimaryBase, true));
                    if (baseMethod != null)
                        return baseMethod;
                }
            }
            return null;
        }

        public static bool HasCallableBaseMethodInPrimaryBase(this Class @class, Method method)
        {
            var baseMethod = @class.GetBaseMethod(method, true, true);
            return baseMethod != null && !baseMethod.IsPure && baseMethod.IsGenerated &&
                !((Class) baseMethod.OriginalNamespace).IsInterface;
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

        public static Method GetMethodByName(this Class @class, string methodName)
        {
            var method = @class.Methods.FirstOrDefault(m => m.Name == methodName);
            if (method != null)
                return method;

            foreach (var @base in @class.Bases.Where(b => b.Type.IsClass()))
            {
                method = @base.Class.GetMethodByName(methodName);
                if (method != null)
                    return method;
            }
            return null;
        }

        public static bool HasRefBase(this Class @class)
        {
            Class @base = null;

            if (@class.HasBaseClass)
                @base = @class.Bases[0].Class;

            var hasRefBase = @base != null && @base.IsRefType && @base.IsDeclared;

            return hasRefBase;
        }

        public static IEnumerable<TranslationUnit> GetGenerated(this IEnumerable<TranslationUnit> units)
        {
            return units.Where(u => u.IsGenerated && (u.HasDeclarations || u.IsSystemHeader) && u.IsValid);
        }
    }
}