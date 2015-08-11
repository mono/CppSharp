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

        public Expression Expression { get; set; }

        public bool IsBitField { get; set; }

        public uint BitWidth { get; set; }

        public string InternalName
        {
            get { return internalName ?? (internalName = OriginalName); }
            set { internalName = value; }
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

        public Field(Field field): base(field)
        {
            QualifiedType = field.QualifiedType;
            Offset = field.Offset;
            Class = field.Class;
            Expression = field.Expression;
            IsBitField = field.IsBitField;
            BitWidth = field.BitWidth;
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFieldDecl(this);
        }

        private string internalName;
    }
}