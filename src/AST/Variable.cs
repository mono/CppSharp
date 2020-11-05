
namespace CppSharp.AST
{
    public class Variable : Declaration, ITypedDecl, IMangledDecl
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitVariableDecl(this);
        }

        public bool IsConstExpr { get; set; }
        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }
        public ExpressionObsolete Initializer { get; set; }

        public string Mangled { get; set; }
    }
}
