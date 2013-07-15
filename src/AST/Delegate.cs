namespace CppSharp
{
    public class Delegate : Declaration
    {
        public FunctionType Type;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitDeclaration(this);
        }
    }
}
