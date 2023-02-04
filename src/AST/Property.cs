using System.Collections.Generic;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a C++ property.
    /// </summary>
    public class Property : Declaration, ITypedDecl
    {
        public Property()
        {
        }

        public Property(Property property)
            : base(property)
        {
            QualifiedType = property.QualifiedType;
            GetMethod = property.GetMethod;
            SetMethod = property.SetMethod;
            Field = property.Field;
            parameters.AddRange(property.Parameters);
        }

        public Type Type => QualifiedType.Type;

        public QualifiedType QualifiedType { get; set; }

        public bool IsStatic =>
            GetMethod is {IsStatic: true} ||
            SetMethod is {IsStatic: true};

        public bool IsPure =>
            GetMethod is {IsPure: true} ||
            SetMethod is {IsPure: true};

        public bool IsVirtual =>
            GetMethod is {IsVirtual: true} ||
            SetMethod is {IsVirtual: true};

        public bool IsOverride =>
            GetMethod is {IsOverride: true} ||
            SetMethod is {IsOverride: true};

        public Method GetMethod { get; set; }

        public Method SetMethod { get; set; }

        public bool HasGetter =>
            (GetMethod != null &&
             GetMethod.GenerationKind != GenerationKind.None) ||
            (Field != null &&
             Field.GenerationKind != GenerationKind.None);

        public bool HasSetter =>
            (SetMethod != null &&
             SetMethod.GenerationKind != GenerationKind.None) ||
            (Field != null &&
             (!Field.QualifiedType.IsConst() ||
              Field.Type.IsConstCharString()) &&
             Field.GenerationKind != GenerationKind.None);

        // The field that should be get and set by this property
        public Field Field { get; set; }

        public Class ExplicitInterfaceImpl { get; set; }

        private readonly List<Parameter> parameters = new();

        /// <summary>
        /// Only applicable to index ([]) properties.
        /// </summary>
        public List<Parameter> Parameters => parameters;

        public bool IsIndexer =>
            GetMethod is {OperatorKind: CXXOperatorKind.Subscript};

        public bool IsSynthetized =>
            (GetMethod != null && GetMethod.IsSynthetized) ||
            (SetMethod != null && SetMethod.IsSynthetized);

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitProperty(this);
        }
    }
}