
using System;
using CppSharp.AST;

namespace CppSharp.Generators.C
{
    public class CppDefaultValuePrinter : CppTypePrinter
    {
        public CppDefaultValuePrinter(BindingContext context) : base(context)
        {
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Bool:
                    return PrintFlavorKind == CppTypePrintFlavorKind.Cpp ? "false" : "0";
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                case PrimitiveType.Float128:
                case PrimitiveType.Decimal:
                    return "0";
                case PrimitiveType.Null:
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.String:
                    return PrintFlavorKind == CppTypePrintFlavorKind.Cpp ? "nullptr" : "0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
