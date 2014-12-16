
namespace CppSharp.AST
{
    public class Variable : Declaration, ITypedDecl, IMangledDecl
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitVariableDecl(this);
        }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }

        public string Mangled { get; set; }

        public bool IsConst
        {
            get
            {
                if (QualifiedType.Qualifiers.IsConst)
                    return true;

                var arrayType = Type as ArrayType;
                if (arrayType == null)
                    return false;

                return arrayType.QualifiedType.Qualifiers.IsConst;
            }
        }
    }
}
