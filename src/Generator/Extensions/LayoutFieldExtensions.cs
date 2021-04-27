using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Parser;

namespace CppSharp.Extensions
{
    public static class LayoutFieldExtensions
    {
        internal static int CalculateOffset(this LayoutField field, LayoutField previousField, ParserTargetInfo targetInfo)
        {
            var type = field.QualifiedType.Type.Desugar();
            var prevFieldSize = previousField.QualifiedType.Type.Desugar().GetWidth(targetInfo) / 8;
            var unalignedOffset = previousField.Offset + prevFieldSize;
            var alignment = type.GetAlignment(targetInfo) / 8;

            if (type is ArrayType arrayType && arrayType.Type.Desugar().IsClass())
            {
                // We have an array of structs. When we generate this field, we'll transform it into an fixed
                // array of bytes to which the elements in the embedded struct array can be bound (via their
                // accessors). At that point, the .Net subsystem will have no information in the generated
                // class on which to base its own alignment calculation. Consequently, we must generate
                // padding. Set alignment to indicate one-byte alignment which is consistent with the "fixed
                // byte fieldName[]" we'll eventually generate so that the offset we return here mimics what
                // will be the case once the struct[] -> byte[] transformation occurs.
                alignment = 1;
            }

            var alignedOffset = (unalignedOffset + (alignment - 1)) & -alignment;
            return (int)alignedOffset;
        }
    }
}