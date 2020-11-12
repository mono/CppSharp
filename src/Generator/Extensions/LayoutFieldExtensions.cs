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
            var alignedOffset = (unalignedOffset + (alignment - 1)) & -alignment;
            return (int)alignedOffset;
        }
    }
}