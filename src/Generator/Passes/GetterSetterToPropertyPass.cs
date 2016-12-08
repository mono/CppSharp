using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using Type = CppSharp.AST.Type;

namespace CppSharp.Passes
{
    public class GetterSetterToPropertyPass : TranslationUnitPass
    {
        private class PropertyGenerator
        {
            private readonly List<Method> getters = new List<Method>();
            private readonly List<Method> setters = new List<Method>();
            private readonly List<Method> setMethods = new List<Method>();
            private readonly List<Method> nonSetters = new List<Method>();

            public PropertyGenerator(Class @class)
            {
                foreach (var method in @class.Methods.Where(
                    m => !m.IsConstructor && !m.IsDestructor && !m.IsOperator && m.IsGenerated))
                    DistributeMethod(method);
            }

            public void GenerateProperties()
            {
                GenerateProperties(setters, false);
                GenerateProperties(setMethods, true);

                foreach (Method getter in
                    from getter in getters
                    where getter.IsGenerated && getter.SynthKind != FunctionSynthKind.ComplementOperator &&
                          ((Class) getter.Namespace).Methods.All(m => m == getter ||!m.IsGenerated || m.Name != getter.Name)
                    select getter)
                {
                    // Make it a read-only property
                    GenerateProperty(getter.Namespace, getter);
                }
            }

            private void GenerateProperties(IEnumerable<Method> settersToUse, bool readOnly)
            {
                foreach (var setter in settersToUse)
                {
                    var type = (Class) setter.Namespace;
                    var firstWord = GetFirstWord(setter.Name);
                    var nameBuilder = new StringBuilder(setter.Name.Substring(firstWord.Length));
                    if (char.IsLower(setter.Name[0]))
                        nameBuilder[0] = char.ToLowerInvariant(nameBuilder[0]);
                    string afterSet = nameBuilder.ToString();
                    var s = setter;
                    foreach (var getter in nonSetters.Where(m => m.Namespace == type &&
                                                                 m.ExplicitInterfaceImpl == s.ExplicitInterfaceImpl))
                    {
                        var name = GetReadWritePropertyName(getter, afterSet);
                        if (name == afterSet &&
                            GetUnderlyingType(getter.OriginalReturnType).Equals(
                                GetUnderlyingType(setter.Parameters[0].QualifiedType)))
                        {
                            Method g = getter;
                            foreach (var method in type.Methods.Where(m => m != g && m.Name == name))
                            {
                                var oldName = method.Name;
                                method.Name = string.Format("get{0}{1}",
                                    char.ToUpperInvariant(method.Name[0]), method.Name.Substring(1));
                                Diagnostics.Debug("Method {0}::{1} renamed to {2}", method.Namespace.Name, oldName, method.Name);
                            }
                            foreach (var @event in type.Events.Where(e => e.Name == name))
                            {
                                var oldName = @event.Name;
                                @event.Name = string.Format("on{0}{1}",
                                    char.ToUpperInvariant(@event.Name[0]), @event.Name.Substring(1));
                                Diagnostics.Debug("Event {0}::{1} renamed to {2}", @event.Namespace.Name, oldName, @event.Name);
                            }
                            getter.Name = name;
                            GenerateProperty(getter.Namespace, getter, readOnly ? null : setter);
                            goto next;
                        }
                    }
                    Property baseProperty = type.GetBaseProperty(new Property { Name = afterSet }, getTopmost: true);
                    if (!type.IsInterface && baseProperty != null && baseProperty.IsVirtual && setter.IsVirtual)
                    {
                        bool isReadOnly = baseProperty.SetMethod == null;
                        GenerateProperty(setter.Namespace, baseProperty.GetMethod,
                            readOnly || isReadOnly ? null : setter);
                    }
                next:
                    ;
                }
                foreach (Method nonSetter in nonSetters)
                {
                    Class type = (Class) nonSetter.Namespace;
                    string name = GetPropertyName(nonSetter.Name);
                    Property baseProperty = type.GetBaseProperty(new Property { Name = name }, getTopmost: true);
                    if (!type.IsInterface && baseProperty != null && baseProperty.IsVirtual)
                    {
                        bool isReadOnly = baseProperty.SetMethod == null;
                        if (readOnly == isReadOnly)
                        {
                            GenerateProperty(nonSetter.Namespace, nonSetter,
                                readOnly ? null : baseProperty.SetMethod);
                        }
                    }
                }
            }

            private static string GetReadWritePropertyName(INamedDecl getter, string afterSet)
            {
                string name = GetPropertyName(getter.Name);
                if (name != afterSet && name.StartsWith("is", StringComparison.Ordinal))
                {
                    name = char.ToLowerInvariant(name[2]) + name.Substring(3);
                }
                return name;
            }

            private static Type GetUnderlyingType(QualifiedType type)
            {
                TagType tagType = type.Type as TagType;
                if (tagType != null)
                    return type.Type;
                // TODO: we should normally check pointer types for const; 
                // however, there's some bug, probably in the parser, that returns IsConst = false for "const Type& arg"
                // so skip the check for the time being
                PointerType pointerType = type.Type as PointerType;
                return pointerType != null ? pointerType.Pointee : type.Type;
            }

            private static void GenerateProperty(DeclarationContext context, Method getter, Method setter = null)
            {
                var type = (Class) context;
                var name = GetPropertyName(getter.Name);
                if (type.Properties.Any(p => p.Name == name &&
                    p.ExplicitInterfaceImpl == getter.ExplicitInterfaceImpl))
                    return;

                var property = new Property
                {
                    Access = getter.Access == AccessSpecifier.Public ||
                        (setter != null && setter.Access == AccessSpecifier.Public) ?
                            AccessSpecifier.Public : AccessSpecifier.Protected,
                    Name = name,
                    Namespace = type,
                    QualifiedType = getter.OriginalReturnType,
                    OriginalNamespace = getter.OriginalNamespace
                };
                if (getter.IsOverride || (setter != null && setter.IsOverride))
                {
                    var baseVirtualProperty = type.GetBaseProperty(property, getTopmost: true);
                    if (baseVirtualProperty != null && !baseVirtualProperty.IsVirtual)
                    {
                        // the only way the above can happen is if we are generating properties in abstract implementations
                        // in which case we can have less naming conflicts since the abstract base can also contain non-virtual properties
                        if (getter.SynthKind == FunctionSynthKind.AbstractImplCall)
                            return;
                        throw new Exception(string.Format(
                            "Base of property {0} is not virtual while the getter is.",
                            getter.QualifiedOriginalName));
                    }
                    if (baseVirtualProperty == null || baseVirtualProperty.SetMethod == null)
                        setter = null;
                }
                property.GetMethod = getter;
                property.SetMethod = setter;
                property.ExplicitInterfaceImpl = getter.ExplicitInterfaceImpl;
                if (property.ExplicitInterfaceImpl == null && setter != null)
                {
                    property.ExplicitInterfaceImpl = setter.ExplicitInterfaceImpl;
                }
                if (getter.Comment != null)
                {
                    property.Comment = CombineComments(getter, setter);
                }
                type.Properties.Add(property);
                getter.GenerationKind = GenerationKind.Internal;
                if (setter != null)
                    setter.GenerationKind = GenerationKind.Internal;
            }

            private static RawComment CombineComments(Declaration getter, Declaration setter)
            {
                var comment = new RawComment
                {
                    Kind = getter.Comment.Kind,
                    BriefText = getter.Comment.BriefText,
                    Text = getter.Comment.Text
                };
                if (getter.Comment.FullComment != null)
                {
                    comment.FullComment = new FullComment();
                    comment.FullComment.Blocks.AddRange(getter.Comment.FullComment.Blocks);
                    if (getter != setter && setter != null && setter.Comment != null)
                    {
                        comment.BriefText += Environment.NewLine + setter.Comment.BriefText;
                        comment.Text += Environment.NewLine + setter.Comment.Text;
                        comment.FullComment.Blocks.AddRange(setter.Comment.FullComment.Blocks);
                    }
                }
                return comment;
            }

            private static string GetPropertyName(string name)
            {
                var firstWord = GetFirstWord(name);
                if (Match(firstWord, new[] { "get" }) && name != firstWord)
                {
                    if (char.IsLower(name[0]))
                    {
                        if (name.Length == 4)
                        {
                            return char.ToLowerInvariant(
                                name[3]).ToString(CultureInfo.InvariantCulture);
                        }
                        return char.ToLowerInvariant(
                            name[3]).ToString(CultureInfo.InvariantCulture) +
                                        name.Substring(4);
                    }
                    return name.Substring(3);
                }
                return name;
            }

            private static string GetPropertyNameFromSetter(Method setter)
            {
                var name = setter.Name.Substring("set".Length);
                if (string.IsNullOrEmpty(name))
                    return name;
                if (char.IsLower(setter.Name[0]) && !char.IsLower(name[0]))
                    return char.ToLowerInvariant(name[0]) + name.Substring(1);
                return name;
            }

            private void DistributeMethod(Method method)
            {
                var firstWord = GetFirstWord(method.Name);
                if (Match(firstWord, new[] { "set" }) && method.Name.Length > firstWord.Length &&
                    method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void))
                {
                    if (method.Parameters.Count == 1)
                        setters.Add(method);
                    else if (method.Parameters.Count > 1)
                        setMethods.Add(method);
                }
                else
                {
                    if (IsGetter(method))
                        getters.Add(method);
                    if (method.Parameters.All(p => p.Kind == ParameterKind.IndirectReturnType))
                        nonSetters.Add(method);
                }
            }

            private static bool IsGetter(Method method)
            {
                if (method.IsDestructor ||
                    (method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void)) ||
                    method.Parameters.Any(p => p.Kind != ParameterKind.IndirectReturnType))
                    return false;
                var firstWord = GetFirstWord(method.Name);
                return (firstWord.Length < method.Name.Length &&
                    Match(firstWord, new[] { "get", "is", "has" })) ||
                    (!Match(firstWord, new[] { "to", "new" }) && !verbs.Contains(firstWord));
            }

            private static bool Match(string prefix, IEnumerable<string> prefixes)
            {
                return prefixes.Any(p => prefix == p || prefix == p + '_');
            }

            private static string GetFirstWord(string name)
            {
                var firstWord = new List<char> { char.ToLowerInvariant(name[0]) };
                for (int i = 1; i < name.Length; i++)
                {
                    var c = name[i];
                    if (char.IsLower(c))
                    {
                        firstWord.Add(c);
                        continue;
                    }
                    if (c == '_')
                    {
                        firstWord.Add(c);
                        break;
                    }
                    if (char.IsUpper(c))
                        break;
                }
                return new string(firstWord.ToArray());
            }
        }

        private static readonly HashSet<string> verbs = new HashSet<string>();

        static GetterSetterToPropertyPass()
        {
            LoadVerbs();
        }

        private static void LoadVerbs()
        {
            var assembly = Assembly.GetAssembly(typeof(GetterSetterToPropertyPass));
            using (var resourceStream = GetResourceStream(assembly))
            {
                using (var streamReader = new StreamReader(resourceStream))
                    while (!streamReader.EndOfStream)
                        verbs.Add(streamReader.ReadLine());
            }
        }

        private static Stream GetResourceStream(Assembly assembly)
        {
            var resources = assembly.GetManifestResourceNames();

            if (resources.Count() == 0)
                throw new Exception("Cannot find embedded verbs data resource.");

            // We are relying on this fact that there is only one resource embedded.
            // Before we loaded the resource by name but found out that naming was
            // different between different platforms and/or build systems.
            return assembly.GetManifestResourceStream(resources[0]);
        }

        public GetterSetterToPropertyPass()
        {
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (base.VisitClassDecl(@class))
                new PropertyGenerator(@class).GenerateProperties();
            return false;
        }
    }
}
