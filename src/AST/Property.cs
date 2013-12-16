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
                return (GetMethod != null) || (Field != null);
            }
        }

        public bool HasSetter
        {
            get
            {
                return (SetMethod != null) ||
                       (Field != null && !Field.QualifiedType.Qualifiers.IsConst);
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

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitProperty(this);
        }

        public bool IsBackedByValueClassField()
        {
            if (Field == null)
                return false;

            Type type;
            Field.Type.IsPointerTo(out type);
            type = type ?? Field.Type;

            Class decl;
            return type.IsTagDecl(out decl) && decl.IsValueType;
        }
    }
}