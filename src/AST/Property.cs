using System.Collections.Generic;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a C++ property.
    /// </summary>
    public class Property : Declaration, ITypedDecl
    {
        public Type Type
        {
            get { return QualifiedType.Type; }
        }

        public QualifiedType QualifiedType { get; set; }

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
    }
}