using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Parser;

namespace CppSharp.Extensions
{
    public static class TypeExtensions
    {
        public static int GetWidth(this Type type, ParserTargetInfo targetInfo)
        {
            if (type is TemplateSpecializationType specializationType)
                type = specializationType.Desugared.Type;

            if (type.IsPrimitiveType(out var primitiveType))
                return (int)primitiveType.GetInfo(targetInfo, out _).Width;

            if (type.IsAddress())
                return (int)targetInfo.PointerWidth;

            if (type.TryGetEnum(out Enumeration enumeration))
                return GetWidth(enumeration.BuiltinType, targetInfo);

            if (type is ArrayType array)
                return (int)array.GetSizeInBits();

            type.TryGetClass(out Class @class);
            return @class.Layout.Size * 8;
        }

        public static int GetAlignment(this Type type, ParserTargetInfo targetInfo)
        {
            if (type is TemplateSpecializationType specializationType)
                type = specializationType.Desugared.Type;

            if (type.IsPrimitiveType(out var primitiveType))
                return (int)primitiveType.GetInfo(targetInfo, out _).Alignment;

            if (type.IsAddress())
                return (int)targetInfo.PointerAlign;

            if (type.TryGetEnum(out Enumeration enumeration))
                return GetAlignment(enumeration.BuiltinType, targetInfo);

            if (type is ArrayType array)
                return GetAlignment(array.Type.Desugar(), targetInfo);

            type.TryGetClass(out Class @class);
            if (@class.MaxFieldAlignment != 0)
                return @class.MaxFieldAlignment * 8;

            return @class.Layout.Alignment * 8;
        }
    }
}
