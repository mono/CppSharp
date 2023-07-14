using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public class TemplateTemplateParameter : Template
    {
        /// <summary>
        /// Whether this template template parameter is a template parameter pack.
        /// <para>template&lt;template&lt;class T&gt; ...MetaFunctions&gt; struct Apply;</para>
        /// </summary>
        public bool IsParameterPack { get; set; }

        /// <summary>
        /// Whether this parameter pack is a pack expansion.
        /// <para>A template template parameter pack is a pack expansion if its template parameter list contains an unexpanded parameter pack.</para>
        /// </summary>
        public bool IsPackExpansion { get; set; }

        /// <summary>
        /// Whether this parameter is a template template parameter pack that has a known list of different template parameter lists at different positions.
        /// A parameter pack is an expanded parameter pack when the original parameter pack's template parameter list was itself a pack expansion, and that expansion has already been expanded. For exampe, given:
        /// <para>
        /// template&lt;typename...Types&gt; struct Outer { template&lt;template&lt;Types&gt; class...Templates> struct Inner; };
        /// </para>
        /// The parameter pack Templates is a pack expansion, which expands the pack Types.When Types is supplied with template arguments by instantiating Outer, the instantiation of Templates is an expanded parameter pack.
        /// </summary>
        public bool IsExpandedParameterPack { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTemplateTemplateParameterDecl(this);
        }
    }

    /// <summary>
    /// Represents a type template parameter.
    /// </summary>
    public class TypeTemplateParameter : Declaration
    {
        /// <summary>
        /// Get the nesting depth of the template parameter.
        /// </summary>
        public uint Depth { get; set; }

        /// <summary>
        /// Get the index of the template parameter within its parameter list.
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Whether this parameter is a non-type template parameter pack.
        /// <para>
        /// If the parameter is a parameter pack, the type may be a PackExpansionType.In the following example, the Dims parameter is a parameter pack (whose type is 'unsigned').
        /// <para>template&lt;typename T, unsigned...Dims&gt; struct multi_array;</para>
        /// </para>
        /// </summary>
        public bool IsParameterPack { get; set; }

        // Generic type constraint
        public string Constraint;

        public QualifiedType DefaultArgument { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTemplateParameterDecl(this);
        }
    }

    /// <summary>
    /// Represents a non-type template parameter.
    /// </summary>
    public class NonTypeTemplateParameter : Declaration
    {
        /// <summary>
        /// Get the nesting depth of the template parameter.
        /// </summary>
        public uint Depth { get; set; }

        /// <summary>
        /// Get the index of the template parameter within its parameter list.
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Whether this parameter is a non-type template parameter pack.
        /// <para>
        /// If the parameter is a parameter pack, the type may be a PackExpansionType.In the following example, the Dims parameter is a parameter pack (whose type is 'unsigned').
        /// <para>template&lt;typename T, unsigned...Dims&gt; struct multi_array;</para>
        /// </para>
        /// </summary>
        public bool IsParameterPack { get; set; }

        public ExpressionObsolete DefaultArgument { get; set; }

        /// <summary>
        /// Get the position of the template parameter within its parameter list.
        /// </summary>
        public uint Position { get; set; }

        /// <summary>
        /// Whether this parameter pack is a pack expansion.
        /// <para>
        /// A non-type template parameter pack is a pack expansion if its type contains an unexpanded parameter pack.In this case, we will have built a PackExpansionType wrapping the type.
        /// </para>
        /// </summary>
        public bool IsPackExpansion { get; set; }

        /// <summary>
        /// Whether this parameter is a non-type template parameter pack that has a known list of different types at different positions.
        /// <para>A parameter pack is an expanded parameter pack when the original parameter pack's type was itself a pack expansion, and that expansion has already been expanded. For example, given:</para>
        /// <para>
        /// template&lt;typename...Types&gt;
        /// struct X {
        ///   template&lt;Types...Values&gt;
        ///   struct Y { /* ... */ };
        /// };
        /// </para>
        /// The parameter pack Values has a PackExpansionType as its type, which expands Types.When Types is supplied with template arguments by instantiating X,
        /// the instantiation of Values becomes an expanded parameter pack.For example, instantiating X&lt;int, unsigned int&gt;
        /// results in Values being an expanded parameter pack with expansion types int and unsigned int.
        /// </summary>
        public bool IsExpandedParameterPack { get; set; }

        public QualifiedType Type { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitNonTypeTemplateParameterDecl(this);
        }
    }

    /// <summary>
    /// The base class of all kinds of template declarations
    /// (e.g., class, function, etc.).
    /// </summary>
    public abstract class Template : Declaration
    {
        // Name of the declaration.
        public override string Name
        {
            get => TemplatedDecl != null ? TemplatedDecl.Name : base.Name;
            set
            {
                base.Name = value;
                if (TemplatedDecl != null)
                    TemplatedDecl.Name = value;
            }
        }

        protected Template()
        {
            Parameters = new List<Declaration>();
        }

        protected Template(Declaration decl)
        {
            TemplatedDecl = decl;
            Parameters = new List<Declaration>();
        }

        public Declaration TemplatedDecl;

        public readonly List<Declaration> Parameters;

        public override string ToString()
        {
            return TemplatedDecl.ToString();
        }
    }

    /// <summary>
    /// Declaration of a type alias template.
    /// </summary>
    public class TypeAliasTemplate : Template
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTypeAliasTemplateDecl(this);
        }
    }

    /// <summary>
    /// Declaration of a class template.
    /// </summary>
    public class ClassTemplate : Template
    {
        public readonly List<ClassTemplateSpecialization> Specializations;

        public Class TemplatedClass => TemplatedDecl as Class;

        public ClassTemplate()
        {
            Specializations = new List<ClassTemplateSpecialization>();
        }

        public ClassTemplate(Declaration decl)
            : base(decl)
        {
            Specializations = new List<ClassTemplateSpecialization>();
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassTemplateDecl(this);
        }

        public override string Name
        {
            get => TemplatedDecl != null ? TemplatedClass.Name : base.Name;
            set
            {
                if (TemplatedDecl != null)
                    TemplatedClass.Name = value;
                else
                    base.Name = value;
            }
        }

        public override string OriginalName
        {
            get => TemplatedDecl != null ? TemplatedClass.OriginalName : base.OriginalName;
            set
            {
                if (TemplatedDecl != null)
                    TemplatedClass.OriginalName = value;
                else
                    base.OriginalName = value;
            }
        }

        public ClassTemplateSpecialization FindSpecializationByUSR(string usr)
        {
            return Specializations.FirstOrDefault(spec => spec.USR == usr);
        }

        public ClassTemplatePartialSpecialization FindPartialSpecializationByUSR(string usr)
        {
            return FindSpecializationByUSR(usr) as ClassTemplatePartialSpecialization;
        }
    }

    /// <summary>
    /// Describes the kind of template specialization that a particular
    /// template specialization declaration represents.
    /// </summary>
    public enum TemplateSpecializationKind
    {
        /// This template specialization was formed from a template-id but has
        /// not yet been declared, defined, or instantiated.
        Undeclared,

        /// This template specialization was implicitly instantiated from a
        /// template.
        ImplicitInstantiation,

        /// This template specialization was declared or defined by an explicit
        /// specialization or partial specialization.
        ExplicitSpecialization,

        /// This template specialization was instantiated from a template due
        /// to an explicit instantiation declaration request.
        ExplicitInstantiationDeclaration,

        /// This template specialization was instantiated from a template due
        /// to an explicit instantiation definition request.
        ExplicitInstantiationDefinition
    }

    /// <summary>
    /// Represents a class template specialization, which refers to a class
    /// template with a given set of template arguments.
    /// </summary>
    public class ClassTemplateSpecialization : Class
    {
        public ClassTemplate TemplatedDecl;

        public readonly List<TemplateArgument> Arguments;

        public TemplateSpecializationKind SpecializationKind;

        public ClassTemplateSpecialization()
        {
            Arguments = new List<TemplateArgument>();
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassTemplateSpecializationDecl(this);
        }

        public override string ToString()
        {
            var args = string.Join(", ", Arguments.Select(a => a.ToString()));
            return $"{OriginalName}<{args}> [{SpecializationKind}]";
        }
    }

    /// <summary>
    /// Represents a class template partial specialization, which refers to
    /// a class template with a given partial set of template arguments.
    /// </summary>
    public class ClassTemplatePartialSpecialization : ClassTemplateSpecialization
    {
    }

    /// <summary>
    /// Declaration of a template function.
    /// </summary>
    public class FunctionTemplate : Template
    {
        public readonly List<FunctionTemplateSpecialization> Specializations;

        public FunctionTemplate()
        {
            Specializations = new List<FunctionTemplateSpecialization>();
        }

        public FunctionTemplate(Declaration decl)
            : base(decl)
        {
            Specializations = new List<FunctionTemplateSpecialization>();
        }

        public Function TemplatedFunction => TemplatedDecl as Function;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionTemplateDecl(this);
        }
    }

    /// <summary>
    /// Represents a function template specialization, which refers to a function
    /// template with a given set of template arguments.
    /// </summary>
    public class FunctionTemplateSpecialization
    {
        public FunctionTemplate Template;

        public readonly List<TemplateArgument> Arguments;

        public Function SpecializedFunction;

        public TemplateSpecializationKind SpecializationKind;

        public FunctionTemplateSpecialization()
        {
            Arguments = new List<TemplateArgument>();
        }

        public FunctionTemplateSpecialization(FunctionTemplateSpecialization fts)
        {
            Template = fts.Template;
            Arguments = new List<TemplateArgument>();
            Arguments.AddRange(fts.Arguments);
            SpecializedFunction = fts.SpecializedFunction;
            SpecializationKind = fts.SpecializationKind;
        }

        public T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionTemplateSpecializationDecl(this);
        }
    }

    /// <summary>
    /// Represents a declaration of a variable template.
    /// </summary>
    public class VarTemplate : Template
    {
        public readonly List<VarTemplateSpecialization> Specializations;

        public Variable TemplatedVariable => TemplatedDecl as Variable;

        public VarTemplate()
        {
            Specializations = new List<VarTemplateSpecialization>();
        }

        public VarTemplate(Variable var) : base(var)
        {
            Specializations = new List<VarTemplateSpecialization>();
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitVarTemplateDecl(this);
        }
    }

    /// <summary>
    /// Represents a var template specialization, which refers to a var
    /// template with a given set of template arguments.
    /// </summary>
    public class VarTemplateSpecialization : Variable
    {
        public VarTemplate TemplatedDecl;

        public readonly List<TemplateArgument> Arguments;

        public TemplateSpecializationKind SpecializationKind;

        public VarTemplateSpecialization()
        {
            Arguments = new List<TemplateArgument>();
        }
    }

    /// <summary>
    /// Represents a variable template partial specialization, which refers to
    /// a variable template with a given partial set of template arguments.
    /// </summary>
    public class VarTemplatePartialSpecialization : VarTemplateSpecialization
    {
    }

    /// <summary>
    /// Represents a dependent using declaration which was marked with typename.
    /// </summary>
    public class UnresolvedUsingTypename : Declaration
    {
        //public TypeAliasTemplate DescribedAliasTemplate { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitUnresolvedUsingDecl(this);
        }
    }
}
