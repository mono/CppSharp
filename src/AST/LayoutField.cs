using System;

namespace CppSharp.AST
{
    public class LayoutField
    {
        public uint Offset { get; set; }
        public QualifiedType QualifiedType { get; set; }
        public string Name { get; set; }
        public IntPtr FieldPtr { get; set; }
        public bool IsVTablePtr { get { return FieldPtr == IntPtr.Zero; } }
        public Expression Expression { get; set; }

        public override string ToString()
        {
            return string.Format("{0} | {1}", Offset, Name);
        }
    }
}