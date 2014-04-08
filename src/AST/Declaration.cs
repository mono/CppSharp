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

    [Flags]
    public enum IgnoreFlags
    {
        None = 0,
        Generation = 1 << 0,
        Processing = 1 << 1,
        Explicit   = 1 << 2
    }

    /// <summary>
    /// Represents a C++ declaration.
    /// </summary>
    public abstract class Declaration : INamedDecl
    {
        
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
                if (string.IsNullOrEmpty(OriginalName))
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

        private string GetQualifiedName(Func<Declaration, string> getName,
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

        // Keeps flags to know the type of ignore.
        public IgnoreFlags IgnoreFlags { get; set; }

        // Whether the declaration should be generated.
        public virtual bool IsGenerated
        {
            get
            {
                var isGenerated = !IgnoreFlags.HasFlag(IgnoreFlags.Generation);

                if (Namespace == null)
                    return isGenerated;

                return isGenerated && Namespace.IsGenerated;
            }

            set
            {
                if (value)
                    IgnoreFlags &= ~IgnoreFlags.Generation;
                else
                    IgnoreFlags |= IgnoreFlags.Generation;
            }
        }

        // Whether the declaration was explicitly ignored.
        public bool ExplicityIgnored
        {
            get
            {
                var isExplicitlyIgnored = IgnoreFlags.HasFlag(IgnoreFlags.Explicit);

                if (Namespace == null)
                    return isExplicitlyIgnored;

                return isExplicitlyIgnored || Namespace.ExplicityIgnored;
            }

            set
            {
                if (value)
                    IgnoreFlags |= IgnoreFlags.Explicit;
                else
                    IgnoreFlags &= ~IgnoreFlags.Explicit;
            }
        }

        // Whether the declaration should be ignored.
        public virtual bool Ignore
        {
            get
            {
                var isIgnored = IgnoreFlags != IgnoreFlags.None;

                if (Namespace != null)
                    isIgnored |= Namespace.Ignore;

                return isIgnored;
            }
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

        public List<Attribute> Attributes { get; private set; }

        protected Declaration()
        {
            Access = AccessSpecifier.Public;
            IgnoreFlags = IgnoreFlags.None;
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
            OriginalName = declaration.OriginalName;
            name = declaration.Name;
            Comment = declaration.Comment;
            IgnoreFlags = declaration.IgnoreFlags;
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
    }
}
