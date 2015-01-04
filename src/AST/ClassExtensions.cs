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

        public static Method GetRootBaseMethod(this Class c, Method @override, bool onlyFirstBase = false)
        {
            return (from @base in c.Bases
                where @base.IsClass && (!onlyFirstBase || !@base.Class.IsInterface)
                let baseMethod = (
                    from method in @base.Class.Methods
                    where
                        method.Name == @override.Name &&
                        method.ReturnType == @override.ReturnType &&
                        method.Parameters.SequenceEqual(@override.Parameters,
                            new ParameterTypeComparer())
                    select method).FirstOrDefault()
                let rootBaseMethod = @base.Class.GetRootBaseMethod(@override) ?? baseMethod
                where rootBaseMethod != null || onlyFirstBase
                select rootBaseMethod).FirstOrDefault();
        }

        public static Property GetRootBaseProperty(this Class c, Property @override, bool onlyFirstBase = false)
        {
            return (from @base in c.Bases
                where (!onlyFirstBase || !@base.Class.IsInterface) && @base.IsClass
                let baseProperty = (
                    from property in @base.Class.Properties
                    where
                        property.Name == @override.Name &&
                        property.Parameters.SequenceEqual(@override.Parameters,
                            new ParameterTypeComparer())
                    select property).FirstOrDefault()
                let rootBaseProperty = @base.Class.GetRootBaseProperty(@override) ?? baseProperty
                where rootBaseProperty != null || onlyFirstBase
                select rootBaseProperty).FirstOrDefault();
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
            var method = c.Methods.FirstOrDefault(
                // HACK: because of the non-shared v-table entries bug one copy may have been renamed and the other not
                m => string.Compare(m.Name, methodName, StringComparison.OrdinalIgnoreCase) == 0);
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
    }
}