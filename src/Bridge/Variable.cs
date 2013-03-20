
namespace Cxxi
{
    public class Variable : Declaration, ITypedDecl
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitVariableDecl(this);
        }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }
    }
}
