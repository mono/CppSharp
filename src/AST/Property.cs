using System;
using System.Linq;
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


        public bool MemberVerifies<M>(M m, Predicate<M> pred) where M:Declaration, IMember<M>
        {
            return m != null && pred(m);
        }

        public bool FieldVerifies(Predicate<Field> pred) 
        {
            return MemberVerifies(Field, pred);
        }

        public bool GetterVerifies(Predicate<Method> pred) 
        {
            return MemberVerifies(GetMethod, pred);
        }

        public bool SetterVerifies(Predicate<Method> pred) 
        {
            return MemberVerifies(SetMethod, pred);
        }

        public bool AccessorVerifies(Predicate<Method> pred) 
        {
            return GetterVerifies(pred) || SetterVerifies(pred);
        }

        public bool IsStatic
        {
            get { return AccessorVerifies(m => m.IsStatic); }
        }

        public bool IsPure
        {
            get { return AccessorVerifies(m => m.IsPure); }
        }

        public bool IsVirtual
        {
            get { return AccessorVerifies(m => m.IsVirtual); }
        }

        public bool IsOverride
        {
            get { return AccessorVerifies(m => m.IsOverride); }
        }

        public Method GetMethod { get; set; }

        public Method SetMethod { get; set; }

        public bool HasGetter
        {
            get
            {
                return GetterVerifies(g => g.GenerationKind != GenerationKind.None) 
                    || FieldVerifies(f => f.GenerationKind != GenerationKind.None);
            }
        }

        public bool HasSetter
        {
            get
            {
                return SetterVerifies(s => s.GenerationKind != GenerationKind.None) 
                    || FieldVerifies(f => f.GenerationKind != GenerationKind.None
                                      && !f.QualifiedType.Qualifiers.IsConst); 
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
            get { return GetterVerifies(g => g.OperatorKind == CXXOperatorKind.Subscript); }
        }

        public bool IsSynthetized
        {
            get { return GetterVerifies(g => g.IsSynthetized); }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitProperty(this);
        }
    }
}