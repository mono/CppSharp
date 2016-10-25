namespace CppSharp.AST
{
    /// <summary>
    /// Base class for declarations which introduce a typedef-name.
    /// </summary>
    public abstract class TypedefNameDecl : Declaration, ITypedDecl
    {
        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }
        public bool IsSynthetized { get; set; }
    }

    /// <summary>
    /// Represents a type definition in C++.
    /// </summary>
    public class TypedefDecl : TypedefNameDecl
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTypedefDecl(this);
        }
    }

    /// <summary>
    /// Represents a type alias in C++.
    /// </summary>
    public class TypeAlias : TypedefNameDecl
    {
        public TypeAliasTemplate DescribedAliasTemplate { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitTypeAliasDecl(this);
        }
    }
}