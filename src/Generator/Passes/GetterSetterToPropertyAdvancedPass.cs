using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CppSharp.AST;
using Type = CppSharp.AST.Type;

namespace CppSharp.Passes
{
    class GetterSetterToPropertyAdvancedPass : TranslationUnitPass
    {
        // collect all types of methods first to be able to match pairs and detect virtuals and overrides;
        // (a property needs to) be virtual or an override if either of its constituent methods are such)
        private readonly List<Method> setters = new List<Method>();
        private readonly List<Method> setMethods = new List<Method>();
        private readonly List<Method> nonSetters = new List<Method>();
        private readonly HashSet<Method> getters = new HashSet<Method>();
        private static readonly HashSet<string> verbs = new HashSet<string>();

        static GetterSetterToPropertyAdvancedPass()
        {
            LoadVerbs();
        }

        private static void LoadVerbs()
        {
            using (var resourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("CppSharp.Generator.Passes.verbs.txt"))
            {
                using (StreamReader streamReader = new StreamReader(resourceStream))
                    while (!streamReader.EndOfStream)
                        verbs.Add(streamReader.ReadLine());
            }
        }

        public GetterSetterToPropertyAdvancedPass()
        {
            Options.VisitClassFields = false;
            Options.VisitClassProperties = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitNamespaceTypedefs = false;
            Options.VisitNamespaceEvents = false;
            Options.VisitNamespaceVariables = false;
            Options.VisitFunctionParameters = false;
            Options.VisitTemplateArguments = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            bool result = base.VisitTranslationUnit(unit);
            GenerateProperties();
            return result;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!method.IsConstructor && !method.IsDestructor && !method.IsOperator &&
                !method.Ignore)
                DistributeMethod(method);
            return base.VisitMethodDecl(method);
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
            foreach (var group in settersToUse.GroupBy(m => m.Namespace))
            {
                foreach (var setter in group)
                {
                    Class type = (Class) setter.Namespace;
                    StringBuilder nameBuilder = new StringBuilder(setter.Name.Substring(3));
                    if (char.IsLower(setter.Name[0]))
                        nameBuilder[0] = char.ToLowerInvariant(nameBuilder[0]);
                    string afterSet = nameBuilder.ToString();
                    foreach (var getter in nonSetters.Where(m => m.Namespace == type))
                    {
                        string name = GetPropertyName(getter.Name);
                        if (string.Compare(name, afterSet, StringComparison.OrdinalIgnoreCase) == 0 &&
                            GetUnderlyingType(getter.OriginalReturnType).Equals(
                                GetUnderlyingType(setter.Parameters[0].QualifiedType)) &&
                            !type.Methods.Any(
                                m =>
                                    m != getter &&
                                    string.Compare(name, m.Name, StringComparison.OrdinalIgnoreCase) == 0))
                        {
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
            if (type.Properties.All(
                p => string.Compare(getter.Name, p.Name, StringComparison.OrdinalIgnoreCase) != 0 ||
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
                    foreach (Method method in type.Methods.Where(m => m.Name == property.Name &&
                        m.Parameters.Any(p => p.Kind != ParameterKind.IndirectReturnType)))
                        method.Name = "get" + method.Name;
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
                    comment.FullComment = new FullComment();
                    comment.FullComment.Blocks.AddRange(getter.Comment.FullComment.Blocks);
                    if (setter != null && setter.Comment != null)
                    {
                        comment.BriefText += Environment.NewLine + setter.Comment.BriefText;
                        comment.Text += Environment.NewLine + setter.Comment.Text;
                        comment.FullComment.Blocks.AddRange(setter.Comment.FullComment.Blocks);
                    }
                    property.Comment = comment;
                }
                type.Properties.Add(property);
                getter.IsGenerated = false;
                if (setter != null)
                    setter.IsGenerated = false;
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

        private bool IsGetter(Method method)
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
}
