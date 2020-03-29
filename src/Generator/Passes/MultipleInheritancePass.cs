using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

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
        private readonly HashSet<Class> interfaces = new HashSet<Class>();

        /// <summary>
        /// Change and implement secondaryy bases at the end to avoid processing implementations.
        /// </summary>
        private readonly HashSet<Class> classesWithSecondaryBases = new HashSet<Class>();

        public MultipleInheritancePass()
        {
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitFunctionParameters = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            bool result = base.VisitASTContext(context);
            foreach (var @interface in interfaces.Where(i => !(i is ClassTemplateSpecialization)))
            {
                int index = @interface.Namespace.Declarations.IndexOf(@interface.OriginalClass);
                @interface.Namespace.Declarations.Insert(index, @interface);
            }

            foreach (Class @class in classesWithSecondaryBases)
            {
                for (var i = 1; i < @class.Bases.Count; i++)
                {
                    var @base = @class.Bases[i];
                    Class @interface = interfaces.FirstOrDefault(iface => iface.OriginalClass == @base.Class);
                    if (@interface == null)
                        continue;
                    @class.Bases[i] = new BaseClassSpecifier(@base) { Type = new TagType(@interface) };
                    ImplementInterfaceMethods(@class, @interface);
                    ImplementInterfaceProperties(@class, @interface);
                }
            }

            interfaces.Clear();
            classesWithSecondaryBases.Clear();
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || !@class.IsGenerated || @class.Bases.Count == 1)
                return false;

            classesWithSecondaryBases.Add(@class);

            // skip the first base because we can inherit from one class
            for (var i = 1; i < @class.Bases.Count; i++)
            {
                var @base = @class.Bases[i];
                var baseClass = @base.Class;
                if (baseClass == null || baseClass.IsInterface || !baseClass.IsGenerated) continue;
                GetInterface(baseClass);
            }
            return true;
        }

        private Class GetInterface(Class @base)
        {
            if (@base.CompleteDeclaration != null)
                @base = (Class) @base.CompleteDeclaration;

            return interfaces.FirstOrDefault(i => i.OriginalClass == @base) ??
                GetNewInterface("I" + @base.Name, @base);
        }

        private Class GetNewInterface(string name, Class @base)
        {
            var specialization = @base as ClassTemplateSpecialization;
            Class @interface;
            if (specialization == null)
            {
                @interface = new Class();
            }
            else
            {
                Class template = specialization.TemplatedDecl.TemplatedClass;
                Class templatedInterface = GetInterface(template);
                @interface = interfaces.FirstOrDefault(i => i.OriginalClass == @base);
                if (@interface != null)
                    return @interface;
                var specializedInterface = new ClassTemplateSpecialization();
                specializedInterface.Arguments.AddRange(specialization.Arguments);
                specializedInterface.TemplatedDecl = new ClassTemplate { TemplatedDecl = templatedInterface };
                @interface = specializedInterface;
            }
            @interface.Name = name;
            @interface.USR = @base.USR;
            @interface.Namespace = @base.Namespace;
            @interface.Access = @base.Access;
            @interface.Type = ClassType.Interface;
            @interface.OriginalClass = @base;

            @interface.Bases.AddRange(
                from b in @base.Bases
                where b.Class != null
                let i = GetInterface(b.Class)
                select new BaseClassSpecifier(b) { Type = new TagType(i) });

            @interface.Methods.AddRange(
                from m in @base.Methods
                where !m.IsConstructor && !m.IsDestructor && !m.IsStatic &&
                    (m.IsGenerated || (m.IsInvalid && specialization != null)) && !m.IsOperator
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
                QualifiedType intPtr = new QualifiedType(
                    new BuiltinType(PrimitiveType.IntPtr));

                var instance = new Property
                {
                    Namespace = @interface,
                    Name = Helpers.InstanceIdentifier,
                    QualifiedType = intPtr,
                    GetMethod = new Method
                    {
                        Name = Helpers.InstanceIdentifier,
                        SynthKind = FunctionSynthKind.InterfaceInstance,
                        Namespace = @interface,
                        OriginalReturnType = intPtr
                    }
                };

                @interface.Properties.Add(instance);

                var dispose = new Method
                {
                    Namespace = @interface,
                    Name = "Dispose",
                    ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Void)),
                    SynthKind = FunctionSynthKind.InterfaceDispose,
                    Mangled = string.Empty
                };

                @interface.Methods.Add(dispose);
            }

            @interface.Declarations.AddRange(@base.Events);

            var type = new QualifiedType(new BuiltinType(PrimitiveType.IntPtr));
            string pointerAdjustment = "__PointerTo" + @base.Name;
            var adjustmentTo = new Property
            {
                Namespace = @interface,
                Name = pointerAdjustment,
                QualifiedType = type,
                GetMethod = new Method
                {
                    Name = pointerAdjustment,
                    SynthKind = FunctionSynthKind.InterfaceInstance,
                    Namespace = @interface,
                    ReturnType = type
                }
            };
            @interface.Properties.Add(adjustmentTo);
            @base.Properties.Add(adjustmentTo);

            @base.Bases.Add(new BaseClassSpecifier { Type = new TagType(@interface) });

            interfaces.Add(@interface);
            if (@base.IsTemplate)
            {
                @interface.IsDependent = true;
                @interface.TemplateParameters.AddRange(@base.TemplateParameters);
                templatedInterfaces[@base] = @interface;
                foreach (var spec in @base.Specializations)
                    @interface.Specializations.Add(
                        (ClassTemplateSpecialization) GetNewInterface(name, spec));
            }
            return @interface;
        }

        private static Property CreateInterfaceProperty(Property property, DeclarationContext @namespace)
        {
            var interfaceProperty = new Property(property) { Namespace = @namespace };
            if (property.GetMethod != null)
            {
                interfaceProperty.GetMethod = new Method(property.GetMethod)
                {
                    OriginalFunction = property.GetMethod,
                    Namespace = @namespace
                };
            }

            if (property.SetMethod != null)
            {
                // handle indexers
                interfaceProperty.SetMethod = property.GetMethod == property.SetMethod ?
                    interfaceProperty.GetMethod : new Method(property.SetMethod)
                    {
                        OriginalFunction = property.SetMethod,
                        Namespace = @namespace
                    };
            }

            return interfaceProperty;
        }

        private void ImplementInterfaceMethods(Class @class, Class @interface)
        {
            foreach (var method in @interface.Methods.Where(
                m => m.SynthKind != FunctionSynthKind.InterfaceDispose))
            {
                var existingImpl = @class.Methods.Find(
                    m => m.OriginalName == method.OriginalName &&
                        m.Parameters.Where(p => !p.Ignore).SequenceEqual(
                            method.Parameters.Where(p => !p.Ignore),
                            ParameterTypeComparer.Instance));

                if (existingImpl != null)
                {
                    if (existingImpl.OriginalFunction == null)
                        existingImpl.OriginalFunction = method;

                    continue;
                }

                var impl = new Method(method)
                {
                    Namespace = @class,
                    OriginalNamespace = @interface,
                    OriginalFunction = method.OriginalFunction
                };

                var rootBaseMethod = @class.GetBaseMethod(method);
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

                var rootBaseProperty = @class.GetBasePropertyByName(property, true);
                if (rootBaseProperty != null && rootBaseProperty.IsDeclared)
                    impl.ExplicitInterfaceImpl = @interface;

                @class.Properties.Add(impl);
            }

            foreach (var @base in @interface.Bases)
                ImplementInterfaceProperties(@class, @base.Class);
        }

        private readonly Dictionary<Class, Class> templatedInterfaces = new Dictionary<Class, Class>();
    }
}
