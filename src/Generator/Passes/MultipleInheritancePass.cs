﻿using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class MultipleInheritancePass : TranslationUnitPass
    {
        /// <summary>
        /// Collects all interfaces in a unit to be added at the end 
        /// because the unit cannot be changed while it's being iterated though.
        /// We also need it to check if a class already has a complementary interface
        /// because different classes may have the same secondary bases.
        /// </summary>
        private readonly Dictionary<Class, Class> interfaces = new Dictionary<Class, Class>();

        public MultipleInheritancePass()
        {
            Options.VisitClassFields = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceVariables = false;
            Options.VisitTemplateArguments = false;
            Options.VisitClassMethods = false;
            Options.VisitClassProperties = false;
            Options.VisitFunctionReturnType = false;
            Options.VisitFunctionParameters = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            bool result = base.VisitTranslationUnit(unit);
            foreach (var @interface in interfaces)
                @interface.Key.Namespace.Classes.Add(@interface.Value);
            interfaces.Clear();
            return result;
        }

        public static bool ComputeClassPath(Class current, Class target,
            List<BaseClassSpecifier> path)
        {
            if (target == current)
                return true;

            foreach (var @base in current.Bases)
            {
                path.Add(@base);

                if (ComputeClassPath(@base.Class, target, path))
                    return false;
            }

            path.RemoveAt(path.Count-1);
            return false;
        }

        public static List<BaseClassSpecifier> ComputeClassPath(Class from, Class to)
        {
            var path = new List<BaseClassSpecifier>();

            ComputeClassPath(from, to, path);
            return path;
        }

        int ComputeNonVirtualBaseClassOffset(Class from, Class to)
        {
            var bases = ComputeClassPath(from, to);
            return bases.Sum(@base => @base.Offset);
        }

        public void CheckNonVirtualInheritedFunctions(Class @class, Class originalClass = null)
        {
            if (originalClass == null)
                originalClass = @class;

            foreach (BaseClassSpecifier baseSpecifier in @class.Bases)
            {
                var @base = baseSpecifier.Class;
                if (@base.IsInterface) continue;

                var nonVirtualOffset = ComputeNonVirtualBaseClassOffset(originalClass, @base);
                if (nonVirtualOffset == 0)
                    continue;

                foreach (var method in @base.Methods.Where(method =>
                    !method.IsVirtual && (method.Kind == CXXMethodKind.Normal)))
                {
                    Console.WriteLine(method);

                    var adjustedMethod = new Method(method)
                    {
                        SynthKind = FunctionSynthKind.AdjustedMethod,
                        AdjustedOffset = nonVirtualOffset,
                    };

                    originalClass.Methods.Add(adjustedMethod);
                }

                CheckNonVirtualInheritedFunctions(@base, originalClass);
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            //CheckNonVirtualInheritedFunctions(@class);

            // skip the first base because we can inherit from one class
            for (var i = 1; i < @class.Bases.Count; i++)
            {
                var @base = @class.Bases[i].Class;
                if (@base.IsInterface) continue;

                var @interface = GetInterface(@base);
                @class.Bases[i] = new BaseClassSpecifier { Type = new TagType(@interface) };
                ImplementInterfaceMethods(@class, @interface);
                ImplementInterfaceProperties(@class, @interface);
            }
            return true;
        }

        private Class GetInterface(Class @base)
        {
            if (@base.CompleteDeclaration != null)
                @base = (Class) @base.CompleteDeclaration;
            var name = "I" + @base.Name;
            if (interfaces.ContainsKey(@base))
                return interfaces[@base];

            return @base.Namespace.Classes.FirstOrDefault(c => c.Name == name) ??
                GetNewInterface(name, @base);
        }

        private Class GetNewInterface(string name, Class @base)
        {
            var @interface = new Class
                {
                    Name = name,
                    Namespace = @base.Namespace,
                    Access = @base.Access,
                    Type = ClassType.Interface,
                    OriginalClass = @base
                };

            @interface.Bases.AddRange(
                from b in @base.Bases
                let i = GetInterface(b.Class)
                select new BaseClassSpecifier { Type = new TagType(i) });

            @interface.Methods.AddRange(
                from m in @base.Methods
                where !m.IsConstructor && !m.IsDestructor && !m.IsStatic && m.IsDeclared && !m.IsOperator
                select new Method(m) { Namespace = @interface, OriginalFunction = m });

            @interface.Properties.AddRange(
                from property in @base.Properties
                where property.IsDeclared
                select new Property(property) { Namespace = @interface });

            @interface.Fields.AddRange(@base.Fields);
            // avoid conflicts when potentially renaming later
            @interface.Declarations.AddRange(@base.Declarations);

            if (@interface.Bases.Count == 0)
            {
                var instance = new Property
                {
                    Namespace = @interface,
                    Name = Helpers.InstanceIdentifier,
                    QualifiedType = new QualifiedType(new BuiltinType(PrimitiveType.IntPtr)),
                    GetMethod = new Method
                    {
                        SynthKind = FunctionSynthKind.InterfaceInstance,
                        Namespace = @interface
                    }
                };

                @interface.Properties.Add(instance);
            }

            @interface.Events.AddRange(@base.Events);

            if (@base.Bases.All(b => b.Class != @interface))
                @base.Bases.Add(new BaseClassSpecifier { Type = new TagType(@interface) });

            interfaces.Add(@base, @interface);
            return @interface;
        }

        private static void ImplementInterfaceMethods(Class @class, Class @interface)
        {
            var parameterTypeComparer = new ParameterTypeComparer();
            foreach (var method in @interface.Methods)
            {
                if (@class.Methods.Any(m => m.OriginalName == method.OriginalName &&
                        m.Parameters.SequenceEqual(method.Parameters.Where(p => !p.Ignore),
                            parameterTypeComparer)))
                    continue;
                var impl = new Method(method) { Namespace = @class, OriginalFunction = method.OriginalFunction };
                var rootBaseMethod = @class.GetBaseMethod(method, true);
                if (rootBaseMethod != null && rootBaseMethod.IsDeclared)
                    impl.ExplicitInterfaceImpl = @interface;
                @class.Methods.Add(impl);
            }
            foreach (var @base in @interface.Bases)
                ImplementInterfaceMethods(@class, @base.Class);
        }

        private static void ImplementInterfaceProperties(Class @class, Class @interface)
        {
            foreach (var property in @interface.Properties.Where(p => p.Name != Helpers.InstanceIdentifier))
            {
                var impl = new Property(property) { Namespace = @class };
                if (impl.GetMethod != null)
                    impl.GetMethod.Namespace = @class;
                if (impl.SetMethod != null)
                    impl.SetMethod.Namespace = @class;
                var rootBaseProperty = @class.GetBaseProperty(property, true);
                if (rootBaseProperty != null && rootBaseProperty.IsDeclared)
                    impl.ExplicitInterfaceImpl = @interface;
                @class.Properties.Add(impl);
            }
            foreach (var @base in @interface.Bases)
                ImplementInterfaceProperties(@class, @base.Class);
        }
    }
}
