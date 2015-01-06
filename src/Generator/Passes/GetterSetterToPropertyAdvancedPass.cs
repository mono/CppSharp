﻿using System;
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
    public class GetterSetterToPropertyAdvancedPass : TranslationUnitPass
    {
        private class PropertyGenerator
        {
            private readonly IDiagnosticConsumer log;
            private readonly List<Method> getters = new List<Method>();
            private readonly List<Method> setters = new List<Method>();
            private readonly List<Method> setMethods = new List<Method>();
            private readonly List<Method> nonSetters = new List<Method>();

            public PropertyGenerator(Class @class, IDiagnosticConsumer log)
            {
                this.log = log;
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
                    where getter.IsGenerated &&
                          ((Class) getter.Namespace).Methods.All(m => m == getter || m.Name != getter.Name)
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
                    Class type = (Class) setter.Namespace;
                    StringBuilder nameBuilder = new StringBuilder(setter.Name.Substring(3));
                    if (char.IsLower(setter.Name[0]))
                        nameBuilder[0] = char.ToLowerInvariant(nameBuilder[0]);
                    string afterSet = nameBuilder.ToString();
                    foreach (var getter in nonSetters.Where(m => m.Namespace == type))
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
                                log.Debug("Method {0}::{1} renamed to {2}", method.Namespace.Name, oldName, method.Name);
                            }
                            foreach (var @event in type.Events.Where(e => e.Name == name))
                            {
                                var oldName = @event.Name;
                                @event.Name = string.Format("on{0}{1}",
                                    char.ToUpperInvariant(@event.Name[0]), @event.Name.Substring(1));
                                log.Debug("Event {0}::{1} renamed to {2}", @event.Namespace.Name, oldName, @event.Name);
                            }
                            getter.Name = name;
                            GenerateProperty(getter.Namespace, getter, readOnly ? null : setter);
                            goto next;
                        }
                    }
                    Property baseVirtualProperty = type.GetRootBaseProperty(new Property { Name = afterSet });
                    if (!type.IsInterface && baseVirtualProperty != null)
                    {
                        bool isReadOnly = baseVirtualProperty.SetMethod == null;
                        GenerateProperty(setter.Namespace, baseVirtualProperty.GetMethod,
                            readOnly || isReadOnly ? null : setter);
                    }
                next:
                    ;
                }
                foreach (Method nonSetter in nonSetters)
                {
                    Class type = (Class) nonSetter.Namespace;
                    string name = GetPropertyName(nonSetter.Name);
                    Property baseVirtualProperty = type.GetRootBaseProperty(new Property { Name = name });
                    if (!type.IsInterface && baseVirtualProperty != null)
                    {
                        bool isReadOnly = baseVirtualProperty.SetMethod == null;
                        if (readOnly == isReadOnly)
                        {
                            GenerateProperty(nonSetter.Namespace, nonSetter,
                                readOnly ? null : baseVirtualProperty.SetMethod);
                        }
                    }
                }
            }

            private static string GetReadWritePropertyName(INamedDecl getter, string afterSet)
            {
                string name = GetPropertyName(getter.Name);
                if (name != afterSet && name.StartsWith("is"))
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
                Class type = (Class) context;
                if (type.Properties.All(p => getter.Name != p.Name ||
                        p.ExplicitInterfaceImpl != getter.ExplicitInterfaceImpl))
                {
                    Property property = new Property();
                    property.Name = GetPropertyName(getter.Name);
                    property.Namespace = type;
                    property.QualifiedType = getter.OriginalReturnType;
                    if (getter.IsOverride || (setter != null && setter.IsOverride))
                    {
                        Property baseVirtualProperty = type.GetRootBaseProperty(property);
                        if (baseVirtualProperty.SetMethod == null)
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
                        var comment = new RawComment();
                        comment.Kind = getter.Comment.Kind;
                        comment.BriefText = getter.Comment.BriefText;
                        comment.Text = getter.Comment.Text;
                        if (getter.Comment.FullComment != null)
                        {
                            comment.FullComment = new FullComment();
                            comment.FullComment.Blocks.AddRange(getter.Comment.FullComment.Blocks);
                            if (setter != null && setter.Comment != null)
                            {
                                comment.BriefText += Environment.NewLine + setter.Comment.BriefText;
                                comment.Text += Environment.NewLine + setter.Comment.Text;
                                comment.FullComment.Blocks.AddRange(setter.Comment.FullComment.Blocks);
                            }
                        }
                        property.Comment = comment;
                    }
                    type.Properties.Add(property);
                    getter.GenerationKind = GenerationKind.Internal;
                    if (setter != null)
                        setter.GenerationKind = GenerationKind.Internal;
                }
            }

            private static string GetPropertyName(string name)
            {
                if (GetFirstWord(name) == "get" && name != "get")
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

            private void DistributeMethod(Method method)
            {
                if (GetFirstWord(method.Name) == "set" && method.Name.Length > 3 &&
                    method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void))
                {
                    if (method.Parameters.Count == 1)
                        setters.Add(method);
                    else
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
                var result = GetFirstWord(method.Name);
                return (result.Length < method.Name.Length &&
                        (result == "get" || result == "is" || result == "has")) ||
                       (result != "to" && result != "new" && !verbs.Contains(result));
            }

            private static string GetFirstWord(string name)
            {
                List<char> firstVerb = new List<char>
                                    {
                                        char.ToLowerInvariant(name[0])
                                    };
                firstVerb.AddRange(name.Skip(1).TakeWhile(
                    c => char.IsLower(c) || !char.IsLetterOrDigit(c)));
                return new string(firstVerb.ToArray());
            }
        }

        private static readonly HashSet<string> verbs = new HashSet<string>();

        static GetterSetterToPropertyAdvancedPass()
        {
            LoadVerbs();
        }

        private static void LoadVerbs()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resourceStream = GetResourceStream(assembly))
            {
                using (var streamReader = new StreamReader(resourceStream))
                    while (!streamReader.EndOfStream)
                        verbs.Add(streamReader.ReadLine());
            }
        }

        private static Stream GetResourceStream(Assembly assembly)
        {
            var stream = assembly.GetManifestResourceStream("CppSharp.Generator.Passes.verbs.txt");
            // HACK: a bug in premake for OS X causes resources to be embedded with an incorrect location
            return stream ?? assembly.GetManifestResourceStream("verbs.txt");
        }


        public GetterSetterToPropertyAdvancedPass()
        {
            Options.VisitClassProperties = false;
            Options.VisitFunctionParameters = false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (VisitDeclarationContext(@class))
            {
                new PropertyGenerator(@class, Log).GenerateProperties();
            }
            return false;
        }
    }
}
