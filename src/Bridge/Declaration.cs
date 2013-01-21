using System;
using System.Collections.Generic;

namespace Cxxi
{
    public interface IRedeclarableDecl
    {
        Declaration PreviousDecl { get; }
    }

    /// <summary>
    /// Represents a C++ declaration.
    /// </summary>
    public abstract class Declaration
    {
        // Namespace the declaration is contained in.
        public Namespace Namespace;

        private string name;

        // Name of the declaration.
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                if (string.IsNullOrEmpty(OriginalName))
                    OriginalName = name;
            }
        }

        // Name of the declaration.
        public string OriginalName;

        public string QualifiedOriginalName
        {
            get
            {
                return Namespace.IsRoot ? OriginalName
                    : string.Format("{0}::{1}", Namespace.Name, OriginalName);
            }
        }

        // Doxygen-style brief comment.
        public string BriefComment;

        // Whether the declaration should be ignored.
        public virtual bool Ignore
        {
            get
            {
                return ExplicityIgnored || Namespace.Ignore;
            }
        }

        // Whether the declaration was explicitly ignored.
        public bool ExplicityIgnored;

        // Contains debug text about the declaration.
        public string DebugText;

        // True if the declaration is incomplete (no definition).
        public bool IsIncomplete;

        // Keeps a reference to the complete version of this declaration.
        public Declaration CompleteDeclaration;

        // Tracks the original declaration definition order.
        public uint DefinitionOrder;

        protected Declaration()
        {
        }

        protected Declaration(string name)
        {
            Name = name;
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
    public class TypedefDecl : Declaration
    {
        /// Type defined.
        public Type Type;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTypedefDecl(this);
        }
    }

    /// <summary>
    /// Represents a C preprocessor macro definition.
    /// </summary>
    public class MacroDefinition : Declaration
    {
        // Contains the macro definition text.
        public string Expression;

        public MacroDefinition()
        {
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitMacroDefinition(this);
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
        T VisitClassTemplateDecl(ClassTemplate template);
        T VisitFunctionTemplateDecl(FunctionTemplate template);
        T VisitMacroDefinition(MacroDefinition macro);
        T VisitNamespace(Namespace @namespace);
    }
}