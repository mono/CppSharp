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
        static GetterSetterToPropertyPass()
        {
            LoadVerbs();
        }

        private static void LoadVerbs()
        {
            var assembly = Assembly.GetAssembly(typeof(GetterSetterToPropertyPass));
            using var resourceStream = GetResourceStream(assembly);
            using var streamReader = new StreamReader(resourceStream);
            while (!streamReader.EndOfStream)
                Verbs.Add(streamReader.ReadLine());
        }

        private static Stream GetResourceStream(Assembly assembly)
        {
            var resources = assembly.GetManifestResourceNames();
            if (!resources.Any())
                throw new Exception("Cannot find embedded verbs data resource.");

            // We are relying on this fact that there is only one resource embedded.
            // Before we loaded the resource by name but found out that naming was
            // different between different platforms and/or build systems.
            return assembly.GetManifestResourceStream(resources[0]);
        }

        public GetterSetterToPropertyPass()
            => VisitOptions.ResetFlags(VisitFlags.ClassTemplateSpecializations);

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            ProcessProperties(@class, GenerateProperties(@class));
            return false;
        }

        protected virtual List<Property> GetProperties() => new();

        protected IEnumerable<Property> GenerateProperties(Class @class)
        {
            var properties = GetProperties();
            foreach (var method in @class.Methods.Where(
                m => !m.IsConstructor && !m.IsDestructor && !m.IsOperator && m.IsGenerated &&
                    (properties.All(p => p.GetMethod != m && p.SetMethod != m) ||
                        m.OriginalFunction != null) &&
                    m.SynthKind != FunctionSynthKind.DefaultValueOverload &&
                    m.SynthKind != FunctionSynthKind.ComplementOperator &&
                    m.SynthKind != FunctionSynthKind.FieldAcessor &&
                    !m.ExcludeFromPasses.Contains(typeof(GetterSetterToPropertyPass))))
            {
                if (IsGetter(method))
                {
                    string name = GetPropertyName(method.Name);
                    CreateOrUpdateProperty(properties, method, name, method.OriginalReturnType);
                    continue;
                }

                if (IsSetter(method))
                {
                    string name = GetPropertyNameFromSetter(method.Name);
                    QualifiedType type = method.Parameters.First(
                        p => p.Kind == ParameterKind.Regular).QualifiedType;
                    CreateOrUpdateProperty(properties, method, name, type, true);
                }
            }

            return CleanUp(@class, properties);
        }

        private IEnumerable<Property> CleanUp(Class @class, List<Property> properties)
        {
            if (!Options.UsePropertyDetectionHeuristics)
                return properties;

            for (int i = properties.Count - 1; i >= 0; i--)
            {
                Property property = properties[i];
                if (property.HasSetter || property.IsExplicitlyGenerated)
                    continue;

                string firstWord = GetFirstWord(property.GetMethod.Name);
                if (firstWord.Length < property.GetMethod.Name.Length &&
                    Match(firstWord, new[] { "get", "is", "has" }))
                    continue;

                if (Match(firstWord, new[] { "to", "new", "on" }) ||
                    Verbs.Contains(firstWord))
                {
                    property.GetMethod.GenerationKind = GenerationKind.Generate;
                    @class.Properties.Remove(property);
                    properties.RemoveAt(i);
                }
            }

            return properties;
        }

        private static void CreateOrUpdateProperty(List<Property> properties, Method method,
            string name, QualifiedType type, bool isSetter = false)
        {
            Type underlyingType = GetUnderlyingType(type);
            Property property = properties.Find(
                p => p.Field == null &&
                    ((!isSetter && p.SetMethod?.IsStatic == method.IsStatic) ||
                     (isSetter && p.GetMethod?.IsStatic == method.IsStatic)) &&
                    ((p.HasGetter && GetUnderlyingType(
                         p.GetMethod.OriginalReturnType).Equals(underlyingType)) ||
                     (p.HasSetter && GetUnderlyingType(
                         p.SetMethod.Parameters[0].QualifiedType).Equals(underlyingType))) &&
                    Match(p, name));

            if (property == null)
                properties.Add(property = new Property { Name = name, QualifiedType = type });

            method.AssociatedDeclaration = property;

            if (isSetter)
                property.SetMethod = method;
            else
            {
                property.GetMethod = method;
                property.QualifiedType = method.OriginalReturnType;
            }

            property.Access = (AccessSpecifier)Math.Max(
                (int)(property.GetMethod ?? property.SetMethod).Access,
                (int)method.Access);

            if (method.ExplicitInterfaceImpl != null)
                property.ExplicitInterfaceImpl = method.ExplicitInterfaceImpl;
        }

        private static bool Match(Property property, string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (property.Name == name)
                return true;

            if (property.Name == RemovePrefix(name))
                return true;

            if (RemovePrefix(property.Name) == name)
            {
                property.Name = property.OriginalName = name;
                return true;
            }

            return false;
        }

        private static string RemovePrefix(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            string name = GetPropertyName(identifier);
            return name.StartsWith("is", StringComparison.Ordinal) && name != "is" ?
                char.ToLowerInvariant(name[2]) + name.Substring(3) : name;
        }

        private static void ProcessProperties(Class @class, IEnumerable<Property> properties)
        {
            foreach (Property property in properties)
            {
                ProcessOverridden(@class, property);

                if (!property.HasGetter)
                    continue;
                if (!property.HasSetter &&
                    @class.GetOverloads(property.GetMethod).Any(
                        m => m != property.GetMethod && !m.Ignore))
                    continue;

                Property conflict = properties.LastOrDefault(
                    p => p.Name == property.Name && p != property &&
                        p.ExplicitInterfaceImpl == property.ExplicitInterfaceImpl);
                if (conflict?.GetMethod != null)
                    conflict.GetMethod = null;

                property.GetMethod.GenerationKind = GenerationKind.Internal;
                if (property.SetMethod != null &&
                    property.SetMethod.OriginalReturnType.Type.Desugar().IsPrimitiveType(PrimitiveType.Void))
                    property.SetMethod.GenerationKind = GenerationKind.Internal;
                property.Namespace = @class;
                @class.Properties.Add(property);
                RenameConflictingMethods(@class, property);
                CombineComments(property);
            }
        }

        private static void ProcessOverridden(Class @class, Property property)
        {
            if (!property.IsOverride)
                return;

            Property baseProperty = @class.GetBaseProperty(property);
            if (baseProperty == null)
            {
                if (property.HasSetter)
                    property.SetMethod = null;
                else
                    property.GetMethod = null;
            }
            else if (!property.HasGetter && baseProperty.HasSetter)
                property.GetMethod = baseProperty.GetMethod;
            else if (!property.HasSetter || !baseProperty.HasSetter)
                property.SetMethod = baseProperty.SetMethod;
        }

        private static void RenameConflictingMethods(Class @class, Property property)
        {
            foreach (var method in @class.Methods.Where(
                m => m.IsGenerated && m.Name == property.Name))
            {
                var oldName = method.Name;
                method.Name = $@"get{char.ToUpperInvariant(method.Name[0])}{
                    method.Name.Substring(1)}";
                Diagnostics.Debug("Method {0}::{1} renamed to {2}",
                    method.Namespace.Name, oldName, method.Name);
            }
            foreach (var @event in @class.Events.Where(
                e => e.Name == property.Name))
            {
                var oldName = @event.Name;
                @event.Name = $@"on{char.ToUpperInvariant(@event.Name[0])}{
                    @event.Name.Substring(1)}";
                Diagnostics.Debug("Event {0}::{1} renamed to {2}",
                    @event.Namespace.Name, oldName, @event.Name);
            }
        }

        private static Type GetUnderlyingType(QualifiedType type)
        {
            if (type.Type is TagType)
                return type.Type;

            // TODO: we should normally check pointer types for const; 
            // however, there's some bug, probably in the parser, that returns IsConst = false for "const Type& arg"
            // so skip the check for the time being
            return type.Type is PointerType pointerType ? pointerType.Pointee : type.Type;
        }

        private static void CombineComments(Property property)
        {
            Method getter = property.GetMethod;
            if (getter.Comment == null)
                return;

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
                Method setter = property.SetMethod;
                if (getter != setter && setter?.Comment != null)
                {
                    comment.BriefText += Environment.NewLine + setter.Comment.BriefText;
                    comment.Text += Environment.NewLine + setter.Comment.Text;
                    comment.FullComment.Blocks.AddRange(setter.Comment.FullComment.Blocks);
                }
            }
            property.Comment = comment;
        }

        private static string GetPropertyName(string name)
        {
            var firstWord = GetFirstWord(name);
            if (!Match(firstWord, new[] {"get"}) ||
                (string.Compare(name, firstWord, StringComparison.InvariantCultureIgnoreCase) == 0) ||
                char.IsNumber(name[3])) return name;

            if (name.Length == 4)
            {
                return char.ToLowerInvariant(
                    name[3]).ToString(CultureInfo.InvariantCulture);
            }

            return string.Concat(char.ToLowerInvariant(
                       name[3]).ToString(CultureInfo.InvariantCulture), name.AsSpan(4));
        }

        private static string GetPropertyNameFromSetter(string name)
        {
            var nameBuilder = new StringBuilder(name);
            string firstWord = GetFirstWord(name);
            if (firstWord == "set" || firstWord == "set_")
                nameBuilder.Remove(0, firstWord.Length);
            if (nameBuilder.Length == 0)
                return nameBuilder.ToString();

            nameBuilder.TrimUnderscores();
            nameBuilder[0] = char.ToLowerInvariant(nameBuilder[0]);
            return nameBuilder.ToString();
        }

        private bool IsGetter(Method method) =>
            !method.IsDestructor &&
            !method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void) && 
            method.Parameters.All(p => p.Kind == ParameterKind.IndirectReturnType);

        private static bool IsSetter(Method method)
        {
            Type returnType = method.OriginalReturnType.Type.Desugar();
            return (returnType.IsPrimitiveType(PrimitiveType.Void) ||
                returnType.IsPrimitiveType(PrimitiveType.Bool)) &&
                method.Parameters.Count(p => p.Kind == ParameterKind.Regular) == 1;
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

        private static readonly HashSet<string> Verbs = new();
    }
}
