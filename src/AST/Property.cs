using System.Collections.Generic;

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

        public Type Type
        {
            get { return QualifiedType.Type; }
        }

        public QualifiedType QualifiedType { get; set; }

        public bool IsStatic
        {
            get
            {
                return (GetMethod != null && GetMethod.IsStatic) ||
                       (SetMethod != null && SetMethod.IsStatic);
            }
        }

        public bool IsPure
        {
            get
            {
                return (GetMethod != null && GetMethod.IsPure) ||
                       (SetMethod != null && SetMethod.IsPure);
            }
        }

        public bool IsVirtual
        {
            get
            {
                return (GetMethod != null && GetMethod.IsVirtual) ||
                       (SetMethod != null && SetMethod.IsVirtual);
            }
        }

        public bool IsOverride
        {
            get
            {
                return (GetMethod != null && GetMethod.IsOverride) ||
                       (SetMethod != null && SetMethod.IsOverride);
            }
        }

        public Method GetMethod { get; set; }

        public Method SetMethod { get; set; }

        public bool HasGetter
        {
            get
            {
                return (GetMethod != null &&
                        GetMethod.GenerationKind != GenerationKind.None) ||
                       (Field != null &&
                        Field.GenerationKind != GenerationKind.None);
            }
        }

        public bool HasSetter
        {
            get
            {
                return (SetMethod != null && 
                        SetMethod.GenerationKind != GenerationKind.None) ||
                       (Field != null && 
                        !Field.QualifiedType.Qualifiers.IsConst && 
                        Field.GenerationKind != GenerationKind.None);
            }
        }

        // The field that should be get and set by this property
        public Field Field { get; set; }

        public Class ExplicitInterfaceImpl { get; set; }

        private readonly List<Parameter> parameters = new List<Parameter>();
        
        /// <summary>
        /// Only applicable to index ([]) properties.
        /// </summary>
        public List<Parameter> Parameters
        {
            get { return parameters; }
        }

        public bool IsIndexer
        {
            get
            {
                return GetMethod != null &&
                       GetMethod.OperatorKind == CXXOperatorKind.Subscript;
            }
        }

        public bool IsSynthetized
        {
            get { return GetMethod != null && GetMethod.IsSynthetized; }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitProperty(this);
        }

    }
}