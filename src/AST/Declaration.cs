using System;
using System.Collections.Generic;
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
        QualifiedType QualifiedType { get; }
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
        Internal,
        /// <summary>
        /// Declaration was already generated in a linked assembly.
        /// </summary>
        Link, 
    }

    /// <summary>
    /// Represents a C++ declaration.
    /// </summary>
    public abstract class Declaration : INamedDecl
    {
        public SourceLocation Location;

        public int LineNumberStart { get; set; }
        public int LineNumberEnd { get; set; }

        private DeclarationContext @namespace;
        public DeclarationContext OriginalNamespace;

        // Namespace the declaration is contained in.
        public DeclarationContext Namespace
        {
            get { return @namespace; }
            set
            {
                @namespace = value;
                if (OriginalNamespace == null)
                    OriginalNamespace = @namespace;
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

        public virtual string OriginalName { get; set; }

        // Name of the declaration.
        private string name;

        public virtual string Name
        {
            get { return name; }
            set
            {
                name = value;
                if (OriginalName == null)
                    OriginalName = name;
            }
        }

        /// <summary>
        /// The effective name of a declaration is the logical name
        /// for the declaration. We need this to make the distiction between
        /// the real and the "effective" name of the declaration to properly
        /// support things like inline namespaces when handling type maps.
        /// </summary>
        public virtual string LogicalName
        {
            get { return Name; }
        }

        /// <summary>
        /// The effective original name of a declaration is the logical
        /// original name for the declaration.
        /// </summary>
        public virtual string LogicalOriginalName
        {
            get { return OriginalName; }
        }

        public string GetQualifiedName(Func<Declaration, string> getName,
            Func<Declaration, DeclarationContext> getNamespace)
        {
            if (Namespace == null)
                return getName(this);

            if (Namespace.IsRoot)
                return getName(this);

            var namespaces = GatherNamespaces(getNamespace(this));
            namespaces.Reverse();

            var names = namespaces.Select(getName).ToList();
            names.Add(getName(this));
            names = names.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            return string.Join("::", names);
        }

        private List<Declaration> GatherNamespaces(DeclarationContext @namespace)
        {
            var namespaces = new List<Declaration>();

            var currentNamespace = @namespace;
            while (currentNamespace != null)
            {
                namespaces.Add(currentNamespace);
                currentNamespace = currentNamespace.Namespace;
            }

            return namespaces;
        }

        public string QualifiedName
        {
            get
            {
                return GetQualifiedName(decl => decl.Name, decl => decl.Namespace);
            }
        }

        public string QualifiedOriginalName
        {
            get
            {
                return GetQualifiedName(
                    decl => decl.OriginalName, decl => decl.OriginalNamespace);
            }
        }

        public string QualifiedLogicalName
        {
            get
            { 
                return GetQualifiedName(
                    decl => decl.LogicalName, decl => decl.Namespace);
            }
        }

        public string QualifiedLogicalOriginalName
        {
            get
            {
                return GetQualifiedName(
                    decl => decl.LogicalOriginalName, decl => decl.OriginalNamespace);
            }
        }

        // Comment associated with declaration.
        public RawComment Comment;

        private GenerationKind? generationKind;

        public GenerationKind GenerationKind
        {
            get
            {
                if (generationKind.HasValue)
                    return generationKind.Value;

                if (Namespace != null)
                    // fields in nested classes have to always be generated
                    return !Namespace.IsGenerated && this is Field ? GenerationKind.Internal : Namespace.GenerationKind;

                return GenerationKind.Generate;
            }
            set { generationKind = value; }
        }

        /// <summary>
        /// Whether the declaration should be generated.
        /// </summary>
        public virtual bool IsGenerated
        {
            get
            {
                return GenerationKind == GenerationKind.Generate;
            }
        }

        /// <summary>
        /// Whether the declaration was explicitly set to be generated via
        /// the GenerationKind propery as opposed to the default generated state.
        /// </summary>
        public virtual bool IsExplicitlyGenerated
        {
            get
            {
                return generationKind.HasValue && generationKind.Value == GenerationKind.Generate;
            }
        }

        /// <summary>
        /// Whether the declaration internal bindings should be generated.
        /// </summary>
        public bool IsInternal
        {
            get
            {
                return GenerationKind == GenerationKind.Internal;
            }
        }

        /// <summary>
        /// Whether a binded version of this declaration is available.
        /// </summary>
        public bool IsDeclared
        {
            get
            {
                var k = GenerationKind;
                return k == GenerationKind.Generate
                    || k == GenerationKind.Internal
                    || k == GenerationKind.Link;
            }
        }

        public void ExplicitlyIgnore()
        {
            GenerationKind = GenerationKind.None;
        }

        [Obsolete("Replace set by ExplicitlyIgnore(). Replace get by GenerationKind == GenerationKind.None.")]
        public bool ExplicityIgnored 
        { 
            get { return GenerationKind == GenerationKind.None; }
            set { if (value) ExplicitlyIgnore(); }
        }

        public virtual bool Ignore
        {
            get { return GenerationKind == GenerationKind.None; }
            set { if (value) ExplicitlyIgnore(); }
        }

        public AccessSpecifier Access { get; set; }

        // Contains debug text about the declaration.
        public string DebugText;

        // True if the declaration is incomplete (no definition).
        public bool IsIncomplete;

        // True if the declaration is dependent.
        public bool IsDependent;

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
        }

        public override string ToString()
        {
            return OriginalName;
        }

        public abstract T Visit<T>(IDeclVisitor<T> visitor);
    }

    /// <summary>
    /// Represents a type definition in C++.
    /// </summary>
    public class TypedefDecl : Declaration, ITypedDecl
    {
        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }
        public bool IsSynthetized { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTypedefDecl(this);
        }
    }

    public interface IDeclVisitor<out T>
    {
        T VisitDeclaration(Declaration decl);
        T VisitClassDecl(Class @class);
        T VisitFieldDecl(Field field);
        T VisitFunctionDecl(Function function);
        T VisitMethodDecl(Method method);
        T VisitParameterDecl(Parameter parameter);
        T VisitTypedefDecl(TypedefDecl typedef);
        T VisitEnumDecl(Enumeration @enum);
        T VisitVariableDecl(Variable variable);
        T VisitClassTemplateDecl(ClassTemplate template);
        T VisitFunctionTemplateDecl(FunctionTemplate template);
        T VisitMacroDefinition(MacroDefinition macro);
        T VisitNamespace(Namespace @namespace);
        T VisitEvent(Event @event);
        T VisitProperty(Property @property);
        T VisitFriend(Friend friend);
    }
}
