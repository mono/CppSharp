
namespace CppSharp.AST
{
    public class Variable : Declaration, ITypedDecl, IMangledDecl
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitVariableDecl(this);
        }

        public AccessSpecifier Access { get; set; }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }

        public string Mangled { get; set; }
    }
}
