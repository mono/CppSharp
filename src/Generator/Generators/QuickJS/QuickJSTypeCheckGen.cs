using System;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;

namespace CppSharp.Generators.Cpp
{
    public class QuickJSTypeCheckGen : CodeGenerator
    {
        private int ParameterIndex;

        public override string FileExtension { get; }

        public QuickJSTypeCheckGen(int parameterIndex) : base(null)
        {
            ParameterIndex = parameterIndex;
        }

        public override void Process()
        {
            throw new System.NotImplementedException();
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            // TODO: Use TargetInfo to check the actual width of types for the target.

            var condition = string.Empty;
            var arg = $"argv[{ParameterIndex}]";
            switch (primitive)
            {
                case PrimitiveType.Bool:
                    condition = $"JS_IsBool({arg})";
                    break;
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                    condition = $"JS_IsInt8({arg})";
                    break;
                case PrimitiveType.UChar:
                    condition = $"JS_IsUInt8({arg})";
                    break;
                case PrimitiveType.WideChar:
                    condition = $"JS_IsUInt16({arg})";
                    break;
                case PrimitiveType.Short:
                    condition = $"JS_IsInt16({arg})";
                    break;
                case PrimitiveType.UShort:
                    condition = $"JS_IsUInt16({arg})";
                    break;
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    condition = $"JS_IsInt32({arg})";
                    break;
                case PrimitiveType.ULong:
                case PrimitiveType.UInt:
                    condition = $"JS_IsUInt32({arg})";
                    break;
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                    condition = $"JS_IsBigInt(ctx, {arg})";
                    break;
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    condition = $"JS_IsFloat({arg})";
                    break;
                case PrimitiveType.LongDouble:
                case PrimitiveType.Float128:
                    condition = $"JS_IsBigFloat({arg})";
                    break;
                case PrimitiveType.String:
                    condition = $"JS_IsString({arg}) || JS_IsNull({arg})";
                    break;
                case PrimitiveType.Decimal:
                    condition = $"JS_IsBigDecimal({arg})";
                    break;
                case PrimitiveType.Null:
                    condition = $"JS_IsNull({arg})";
                    break;
                case PrimitiveType.Void:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                default:
                    throw new NotImplementedException();
            }

            Write(condition);
            return true;
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (pointer.IsConstCharString())
                return VisitPrimitiveType(PrimitiveType.String, quals);

            return base.VisitPointerType(pointer, quals);
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            Write($"JS_IsFunction(ctx, argv[{ParameterIndex}])");
            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration is Enumeration)
            {
                VisitPrimitiveType(PrimitiveType.Int, quals);
                return true;
            }

            var arg = $"argv[{ParameterIndex}]";
            Write($"JS_IsObject({arg}) || JS_IsNull({arg})");
            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            Write($"JS_IsArray(ctx, argv[{ParameterIndex}])");
            return true;
        }
    }
}