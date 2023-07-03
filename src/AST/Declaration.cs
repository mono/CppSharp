using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CppSharp.AST
{
    public interface IRedeclarableDecl
    {
        Declaration PreviousDecl { get; }
    }

    public interface ITypedDecl
    {
        Type Type { get; }
        QualifiedType QualifiedType { get; set; }
    }

    public interface INamedDecl
    {
        string Name { get; set; }
    }

    public interface IMangledDecl
    {
        string Mangled { get; set; }
    }

    /// <summary>
    /// Kind of the generated declaration
    /// </summary>
    public enum GenerationKind
    {
        /// <summary>
        /// Declaration is not generated.
        /// </summary>
        None,
        /// <summary>
        /// Declaration is generated.
        /// </summary>
        Generate,
        /// <summary>
        /// Declaration is generated to be used internally.
        /// </summary>
        Internal
    }

    public class DeclarationBase
    {
        public bool IsInvalid { get; set; }

        protected GenerationKind? generationKind;

        public virtual GenerationKind GenerationKind
        {
            get => generationKind ?? GenerationKind.Generate;
            set => generationKind = value;
        }

        /// <summary>
        /// Whether the declaration should be generated.
        /// </summary>
        public bool IsGenerated => GenerationKind == GenerationKind.Generate;

        /// <summary>
        /// Whether the declaration has an explicit set generation kind.
        /// </summary>
        public bool HasExplicitGenerationKind => generationKind.HasValue;

        /// <summary>
        /// Whether the declaration was explicitly set to be generated via
        /// the GenerationKind property as opposed to the default generated state.
        /// </summary>
        public bool IsExplicitlyGenerated => generationKind == GenerationKind.Generate;

        /// <summary>
        /// Whether the declaration internal bindings should be generated.
        /// </summary>
        public bool IsInternal => GenerationKind == GenerationKind.Internal;

        /// <summary>
        /// Whether a binded version of this declaration is available.
        /// </summary>
        public bool IsDeclared => GenerationKind == GenerationKind.Generate
            || GenerationKind == GenerationKind.Internal;

        public void ExplicitlyIgnore()
        {
            GenerationKind = GenerationKind.None;
        }

        [Obsolete("Replace set by ExplicitlyIgnore(). Replace get by GenerationKind == GenerationKind.None.")]
        public bool ExplicityIgnored
        {
            get => GenerationKind == GenerationKind.None;
            set { if (value) ExplicitlyIgnore(); }
        }

        public virtual bool Ignore
        {
            get => GenerationKind == GenerationKind.None;
            set { if (value) ExplicitlyIgnore(); }
        }
    }

    /// <summary>
    /// Represents a C++ declaration.
    /// </summary>
    [DebuggerDisplay("{ToString()} [{GetType().Name}]")]
    public abstract class Declaration : DeclarationBase, INamedDecl
    {
        public SourceLocation Location;

        public int LineNumberStart { get; set; }
        public int LineNumberEnd { get; set; }
        public bool IsImplicit { get; set; }
        public int AlignAs { get; set; }
        public int MaxFieldAlignment { get; set; }

        private DeclarationContext @namespace;
        public DeclarationContext OriginalNamespace;

        // Namespace the declaration is contained in.
        public DeclarationContext Namespace
        {
            get => @namespace;
            set
            {
                @namespace = value;
                OriginalNamespace ??= @namespace;
            }
        }

        public TranslationUnit TranslationUnit
        {
            get
            {
                if (this is TranslationUnit)
                    return this as TranslationUnit;
                return Namespace.TranslationUnit;
            }
        }

        public override GenerationKind GenerationKind
        {
            get
            {
                if (generationKind.HasValue)
                    return generationKind.Value;

                if (Namespace != null)
                    // fields in nested classes have to always be generated
                    return !Namespace.IsGenerated && this is Field ?
                        GenerationKind.Internal : Namespace.GenerationKind;

                return GenerationKind.Generate;
            }
        }

        public virtual string OriginalName { get; set; }

        // Name of the declaration.
        private string name;

        public virtual string Name
        {
            get => name;
            set
            {
                name = value;
                OriginalName ??= name;
            }
        }

        /// <summary>
        /// The effective name of a declaration is the logical name
        /// for the declaration. We need this to make the distiction between
        /// the real and the "effective" name of the declaration to properly
        /// support things like inline namespaces when handling type maps.
        /// </summary>
        public virtual string LogicalName => Name;

        /// <summary>
        /// The effective original name of a declaration is the logical
        /// original name for the declaration.
        /// </summary>
        public virtual string LogicalOriginalName => OriginalName;

        public static string QualifiedNameSeparator = "::";

        public string GetQualifiedName(Func<Declaration, string> getName,
            Func<Declaration, DeclarationContext> getNamespace, bool getOriginal)
        {
            DeclarationContext @namespace = getNamespace(this);
            if (@namespace == null)
                return getName(this);

            if (@namespace.IsRoot)
                return getName(this);

            var namespaces = GatherNamespaces(@namespace, getOriginal);

            var names = namespaces.Select(getName).ToList();
            names.Add(getName(this));
            names = names.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            return string.Join(QualifiedNameSeparator, names);
        }

        public static IEnumerable<Declaration> GatherNamespaces(DeclarationContext @namespace, bool getOriginal)
        {
            var namespaces = new Stack<Declaration>();

            var currentNamespace = @namespace;
            while (currentNamespace != null)
            {
                var isInlineNamespace = currentNamespace is Namespace { IsInline: true };
                if (!isInlineNamespace)
                    namespaces.Push(currentNamespace);
                currentNamespace = getOriginal ? currentNamespace.OriginalNamespace : currentNamespace.Namespace;
            }

            return namespaces;
        }

        public string QualifiedName
        {
            get
            {
                return GetQualifiedName(decl => GetDeclName(decl, decl.Name), decl => decl.Namespace, false);
            }
        }

        public string QualifiedOriginalName
        {
            get
            {
                return GetQualifiedName(
                    decl => GetDeclName(decl, decl.OriginalName), decl => decl.OriginalNamespace, true);
            }
        }

        public string QualifiedLogicalName
        {
            get
            {
                return GetQualifiedName(
                    decl => GetDeclName(decl, decl.LogicalName), decl => decl.Namespace, false);
            }
        }

        public string QualifiedLogicalOriginalName
        {
            get
            {
                return GetQualifiedName(
                    decl => GetDeclName(decl, decl.LogicalOriginalName), decl => decl.OriginalNamespace, true);
            }
        }

        private static string GetDeclName(Declaration decl, string name)
        {
            if (decl is ClassTemplateSpecialization specialization)
                return string.Format("{0}<{1}>", name,
                    string.Join(", ", specialization.Arguments.Select(
                        a => a.Type.Type == null ? string.Empty : a.Type.ToString())));
            return name;
        }

        public Declaration AssociatedDeclaration { get; set; }

        // Comment associated with declaration.
        public RawComment Comment;

        public AccessSpecifier Access { get; set; }

        // Contains debug text about the declaration.
        public string DebugText;

        // True if the declaration is incomplete (no definition).
        public bool IsIncomplete;

        // True if the declaration is dependent.
        public bool IsDependent;

        // True if the declaration is deprecated.
        public bool IsDeprecated;

        // Keeps a reference to the complete version of this declaration.
        public Declaration CompleteDeclaration;

        // Tracks the original declaration definition order.
        public uint DefinitionOrder;

        // Passes that should not be run on this declaration.
        public ISet<System.Type> ExcludeFromPasses;

        // List of preprocessed entities attached to this declaration.
        public IList<PreprocessedEntity> PreprocessedEntities;

        // Pointer to the original declaration from Clang.
        public IntPtr OriginalPtr;

        // The Unified Symbol Resolution (USR) for the declaration.
        // A Unified Symbol Resolution (USR) is a string that identifies a 
        // particular entity (function, class, variable, etc.) within a program.
        // USRs can be compared across translation units to determine, e.g., 
        // when references in one translation refer to an entity defined in 
        // another translation unit. 
        public string USR;

        public List<Attribute> Attributes { get; private set; }

        public List<Declaration> Redeclarations { get; } = new List<Declaration>();

        // Custom declaration map for custom code generation.
        public object DeclMap { get; set; }

        protected Declaration()
        {
            Access = AccessSpecifier.Public;
            ExcludeFromPasses = new HashSet<System.Type>();
            PreprocessedEntities = new List<PreprocessedEntity>();
            Attributes = new List<Attribute>();
        }

        protected Declaration(string name)
            : this()
        {
            this.name = name;
        }

        protected Declaration(Declaration declaration)
            : this()
        {
            Namespace = declaration.Namespace;
            OriginalNamespace = declaration.OriginalNamespace;
            OriginalName = declaration.OriginalName;
            name = declaration.Name;
            Comment = declaration.Comment;
            generationKind = declaration.generationKind;
            Access = declaration.Access;
            DebugText = declaration.DebugText;
            IsIncomplete = declaration.IsIncomplete;
            IsDependent = declaration.IsDependent;
            CompleteDeclaration = declaration.CompleteDeclaration;
            DefinitionOrder = declaration.DefinitionOrder;
            ExcludeFromPasses = new HashSet<System.Type>(
                declaration.ExcludeFromPasses);
            PreprocessedEntities = new List<PreprocessedEntity>(
                declaration.PreprocessedEntities);
            OriginalPtr = declaration.OriginalPtr;
            LineNumberStart = declaration.LineNumberStart;
            LineNumberEnd = declaration.LineNumberEnd;
            IsImplicit = declaration.IsImplicit;
            AssociatedDeclaration = declaration.AssociatedDeclaration;
            DeclMap = declaration.DeclMap;
        }

        public override string ToString()
        {
            return OriginalName;
        }

        public abstract T Visit<T>(IDeclVisitor<T> visitor);
    }

    public interface IDeclVisitor<out T>
    {
        T VisitDeclaration(Declaration decl);
        T VisitTranslationUnit(TranslationUnit unit);
        T VisitClassDecl(Class @class);
        T VisitFieldDecl(Field field);
        T VisitFunctionDecl(Function function);
        T VisitMethodDecl(Method method);
        T VisitParameterDecl(Parameter parameter);
        T VisitTypedefNameDecl(TypedefNameDecl typedef);
        T VisitTypedefDecl(TypedefDecl typedef);
        T VisitTypeAliasDecl(TypeAlias typeAlias);
        T VisitEnumDecl(Enumeration @enum);
        T VisitEnumItemDecl(Enumeration.Item item);
        T VisitVariableDecl(Variable variable);
        T VisitMacroDefinition(MacroDefinition macro);
        T VisitNamespace(Namespace @namespace);
        T VisitEvent(Event @event);
        T VisitProperty(Property @property);
        T VisitFriend(Friend friend);
        T VisitClassTemplateDecl(ClassTemplate template);
        T VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization);
        T VisitFunctionTemplateDecl(FunctionTemplate template);
        T VisitFunctionTemplateSpecializationDecl(FunctionTemplateSpecialization specialization);
        T VisitVarTemplateDecl(VarTemplate template);
        T VisitVarTemplateSpecializationDecl(VarTemplateSpecialization template);
        T VisitTemplateTemplateParameterDecl(TemplateTemplateParameter templateTemplateParameter);
        T VisitTemplateParameterDecl(TypeTemplateParameter templateParameter);
        T VisitNonTypeTemplateParameterDecl(NonTypeTemplateParameter nonTypeTemplateParameter);
        T VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate);
        T VisitUnresolvedUsingDecl(UnresolvedUsingTypename unresolvedUsingTypename);
    }
}
