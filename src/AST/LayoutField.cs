namespace CppSharp.AST
{
    public class LayoutField
    {
        public LayoutField(uint offset, Field field)
        {
            Field = field;
            Offset = offset;
        }

        public uint Offset { get; set; }

        public string Name
        {
            get { return name ?? Field.OriginalName; }
            set { name = value; }
        }

        public Field Field { get; set; }

        public override string ToString()
        {
            return string.Format("{0} | {1}", Offset, Field.OriginalName);
        }

        private string name;
    }
}