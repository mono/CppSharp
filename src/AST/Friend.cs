namespace CppSharp.AST
{
    public class Friend : Declaration
    {
        public Declaration Declaration { get; set; }

        public Friend()
        {
        }

        public Friend(Declaration declaration)
        {
            Declaration = declaration;
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFriend(this);
        }
    }
}
