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

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            // skip the first base because we can inherit from one class
            for (var i = 1; i < @class.Bases.Count; i++)
            {
                var @base = @class.Bases[i];
                var baseClass = @base.Class;
                if (baseClass.IsInterface) continue;

                var @interface = GetInterface(baseClass);
                @class.Bases[i] = new BaseClassSpecifier(@base) { Type = new TagType(@interface) };
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
                select new BaseClassSpecifier(b) { Type = new TagType(i) });

            @interface.Methods.AddRange(
                from m in @base.Methods
                where !m.IsConstructor && !m.IsDestructor && !m.IsStatic && m.IsDeclared && !m.IsOperator
                select new Method(m) { Namespace = @interface, OriginalFunction = m });

            @interface.Properties.AddRange(
                from property in @base.Properties
                where property.IsDeclared
                select CreateInterfaceProperty(property, @interface));

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

        private static Property CreateInterfaceProperty(Property property, DeclarationContext @namespace)
        {
            var interfaceProperty = new Property(property) { Namespace = @namespace };
            if (property.GetMethod != null)
                interfaceProperty.GetMethod = new Method(property.GetMethod)
                    {
                        OriginalFunction = property.GetMethod,
                        Namespace = @namespace
                    };
            if (property.SetMethod != null)
                // handle indexers
                interfaceProperty.SetMethod = property.GetMethod == property.SetMethod ?
                    interfaceProperty.GetMethod : new Method(property.SetMethod)
                        {
                            OriginalFunction = property.SetMethod,
                            Namespace = @namespace
                        };
            return interfaceProperty;
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
                var impl = new Method(method)
                    {
                        Namespace = @class,
                        OriginalNamespace = @interface,
                        OriginalFunction = method.OriginalFunction
                    };
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
                var impl = CreateInterfaceProperty(property, @class);
                impl.OriginalNamespace = @interface;
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
