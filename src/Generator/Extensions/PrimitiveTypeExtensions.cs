using CppSharp.AST;
using CppSharp.Parser;
using System;
using System.Security.Cryptography;

namespace CppSharp.Extensions
{
    public static class PrimitiveTypeExtensions
    {
        public static bool IsIntegerType(this PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                    return true;
                default:
                    return false;
            }
        }

        public static (uint Width, uint Alignment) GetInfo(this PrimitiveType primitive, ParserTargetInfo targetInfo, out bool signed)
        {
            signed = false;

            switch (primitive)
            {
                case PrimitiveType.Bool:
                    return (targetInfo.BoolWidth, targetInfo.BoolAlign);

                case PrimitiveType.Short:
                    signed = true;
                    return (targetInfo.ShortWidth, targetInfo.ShortAlign);
                case PrimitiveType.Int:
                    signed = true;
                    return (targetInfo.IntWidth, targetInfo.IntAlign);
                case PrimitiveType.Long:
                    signed = true;
                    return (targetInfo.LongWidth, targetInfo.LongAlign);
                case PrimitiveType.LongLong:
                    signed = true;
                    return (targetInfo.LongLongWidth, targetInfo.LongLongAlign);

                case PrimitiveType.UShort:
                    return (targetInfo.ShortWidth, targetInfo.ShortAlign);
                case PrimitiveType.UInt:
                    return (targetInfo.IntWidth, targetInfo.IntAlign);
                case PrimitiveType.ULong:
                    return (targetInfo.LongWidth, targetInfo.LongAlign);
                case PrimitiveType.ULongLong:
                    return (targetInfo.LongLongWidth, targetInfo.LongLongAlign);

                case PrimitiveType.SChar:
                case PrimitiveType.Char:
                    signed = true;
                    return (targetInfo.CharWidth, targetInfo.CharAlign);
                case PrimitiveType.UChar:
                    return (targetInfo.CharWidth, targetInfo.CharAlign);
                case PrimitiveType.WideChar:
                    signed = targetInfo.WCharType.IsSigned();
                    return (targetInfo.WCharWidth, targetInfo.WCharAlign);
                case PrimitiveType.Char16:
                    signed = targetInfo.Char16Type.IsSigned();
                    return (targetInfo.Char16Width, targetInfo.Char16Align);
                case PrimitiveType.Char32:
                    signed = targetInfo.Char32Type.IsSigned();
                    return (targetInfo.Char32Width, targetInfo.Char32Align);

                case PrimitiveType.Float:
                    signed = true;
                    return (targetInfo.FloatWidth, targetInfo.FloatAlign);
                case PrimitiveType.Double:
                    signed = true;
                    return (targetInfo.DoubleWidth, targetInfo.DoubleAlign);
                case PrimitiveType.LongDouble:
                    signed = true;
                    return (targetInfo.LongDoubleWidth, targetInfo.LongDoubleAlign);
                case PrimitiveType.Half:
                    signed = true;
                    return (targetInfo.HalfWidth, targetInfo.HalfAlign);
                case PrimitiveType.Float128:
                    signed = true;
                    return (targetInfo.Float128Width, targetInfo.Float128Align);

                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                    signed = targetInfo.IntPtrType.IsSigned();
                    return (targetInfo.PointerWidth, targetInfo.PointerAlign);

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
