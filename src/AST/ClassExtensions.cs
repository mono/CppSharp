using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public static class ClassExtensions 
    {
        public static IEnumerable<Method> FindOperator(this Class c, CXXOperatorKind kind)
        {
            return c.Operators.Where(method => method.OperatorKind == kind);
        }

        public static IEnumerable<Method> FindMethodByOriginalName(this Class c, string name)
        {
            return c.Methods.Where(method => method.OriginalName == name);
        }

        public static IEnumerable<Variable> FindVariableByOriginalName(this Class c, string originalName)
        {
            return c.Variables.Where(v => v.OriginalName == originalName);
        }

        public static IEnumerable<Function> GetFunctionOverloads(this Class c, Function function)
        {
            if (function.IsOperator)
                return c.FindOperator(function.OperatorKind);
            return c.Methods.Where(method => method.Name == function.Name);
        }

        public static IEnumerable<T> FindHierarchy<T>(this Class c, Func<Class, IEnumerable<T>> func)
            where T : Declaration
        {
            foreach (var elem in func(c))
                yield return elem;

            foreach (var @base in c.Bases)
            {
                if (!@base.IsClass) continue;
                foreach(var elem in @base.Class.FindHierarchy<T>(func))
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

        public static Method GetBaseMethod(this Class c, Method @override, bool onlyPrimaryBase = false, bool getTopmost = false)
        {
            foreach (var @base in c.Bases)
            {
                if (!@base.IsClass || @base.Class.OriginalClass == c || (onlyPrimaryBase && @base.Class.IsInterface))
                    continue;

                Method baseMethod;
                if (!getTopmost)
                {
                    baseMethod = (@base.Class.GetBaseMethod(@override, onlyPrimaryBase));
                    if (baseMethod != null)
                        return baseMethod;
                }

                baseMethod = (from method in @base.Class.Methods
                    where
                        (method.OriginalName == @override.OriginalName && method.ReturnType == @override.ReturnType &&
                         method.Parameters.SequenceEqual(@override.Parameters, new ParameterTypeComparer())) ||
                        (@override.IsDestructor && method.IsDestructor && method.IsVirtual)
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

        public static bool HasNonAbstractBaseMethod(this Class @class, Method method)
        {
            var baseMethod = @class.GetBaseMethod(method, true, true);
            return baseMethod != null && !baseMethod.IsPure;
        }

        public static Property GetBaseProperty(this Class c, Property @override, bool onlyFirstBase = false, bool getTopmost = false)
        {
            foreach (var @base in c.Bases)
            {
                if (!@base.IsClass || @base.Class.OriginalClass == c || (onlyFirstBase && @base.Class.IsInterface))
                    continue;

                Property baseProperty;
                if (!getTopmost)
                {
                    baseProperty = @base.Class.GetBaseProperty(@override, onlyFirstBase);
                    if (baseProperty != null)
                        return baseProperty;
                }

                baseProperty = (from property in @base.Class.Properties
                    where
                        property.OriginalName == @override.OriginalName &&
                        property.Parameters.SequenceEqual(@override.Parameters, new ParameterTypeComparer())
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

        public static bool HasNonAbstractBaseProperty(this Class @class, Property property)
        {
            var baseProperty = @class.GetBaseProperty(property, true, true);
            return baseProperty != null && !baseProperty.IsPure;
        }

        public static Property GetPropertyByName(this Class c, string propertyName)
        {
            Property property = c.Properties.FirstOrDefault(m => m.Name == propertyName);
            if (property != null)
                return property;

            foreach (var baseClassSpecifier in c.Bases.Where(
                b => b.Type.IsClass() && b.Class.IsDeclared))
            {
                property = baseClassSpecifier.Class.GetPropertyByName(propertyName);
                if (property != null)
                    return property;
            }
            return null;
        }

        public static Property GetPropertyByConstituentMethod(this Class c, Method method)
        {
            var property = c.Properties.FirstOrDefault(p => p.GetMethod == method);
            if (property != null)
                return property;
            property = c.Properties.FirstOrDefault(p => p.SetMethod == method);
            if (property != null)
                return property;

            foreach (BaseClassSpecifier @base in c.Bases.Where(b => b.Type.IsClass()))
            {
                property = @base.Class.GetPropertyByConstituentMethod(method);
                if (property != null)
                    return property;
            }
            return null;
        }

        public static Method GetMethodByName(this Class c, string methodName)
        {
            var method = c.Methods.FirstOrDefault(m => m.Name == methodName);
            if (method != null)
                return method;

            foreach (BaseClassSpecifier @base in c.Bases.Where(b => b.Type.IsClass()))
            {
                method = @base.Class.GetMethodByName(methodName);
                if (method != null)
                    return method;
            }
            return null;
        }

        public static bool HasRefBase(this Class @class)
        {
            Class baseClass = null;

            if (@class.HasBaseClass)
                baseClass = @class.Bases[0].Class;

            var hasRefBase = baseClass != null && baseClass.IsRefType
                             && baseClass.IsDeclared;

            return hasRefBase;
        }

        private static bool ComputeClassPath(this Class current, Class target, IList<BaseClassSpecifier> path)
        {
            if (target == current)
                return true;

            foreach (var @base in current.Bases)
            {
                path.Add(@base);

                var @class = @base.Class.OriginalClass ?? @base.Class;
                if (@class != current && @class.ComputeClassPath(target, path))
                    return false;
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        public static int ComputeNonVirtualBaseClassOffsetTo(this Class from, Class to)
        {
            var path = new List<BaseClassSpecifier>();
            @from.ComputeClassPath(to, path);
            return path.Sum(@base => @base.Offset);
        }
    }
}