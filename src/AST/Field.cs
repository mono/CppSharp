namespace CppSharp.AST
{
    /// <summary>
    /// Represents a a C/C++ record field Decl.
    /// </summary>
    public class Field : Declaration, ITypedDecl
    {
        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }

        public uint Offset { get; set; }
        public Class Class { get; set; }

        public uint OffsetInBytes
        {
            get { return Offset / (sizeof (byte) * 8); }
        }

        public Field()
        {
            Offset = 0;
        }

        public Field(string name, QualifiedType type, AccessSpecifier access)
        {
            Name = name;
            QualifiedType = type;
            Access = access;
            Offset = 0;
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFieldDecl(this);
        }
    }
}