namespace Cxxi
{
    /// <summary>
    /// Represents a a C/C++ record field Decl.
    /// </summary>
    public class Field : Declaration, ITypedDecl
    {
        public Type Type { get; set; }
        public AccessSpecifier Access { get; set; }
        public uint Offset { get; set; }

        public Field()
        {
            Offset = 0;
        }

        public Field(string name, Type type, AccessSpecifier access)
        {
            Name = name;
            Type = type;
            Access = access;
            Offset = 0;
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFieldDecl(this);
        }
    }
}