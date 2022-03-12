using System;
using CppSharp.AST;
using CppSharp.Extensions;

namespace CppSharp.Generators.Cpp
{
    public class NAPITypeCheckGen : CodeGenerator
    {
        private int ParameterIndex;

        public override string FileExtension { get; }

        public NAPITypeCheckGen(int parameterIndex) : base(null)
        {
            ParameterIndex = parameterIndex;
        }

        public override void Process()
        {
            throw new System.NotImplementedException();
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            var condition = string.Empty;
            switch (primitive)
            {
                case PrimitiveType.Bool:
                    condition = $"NAPI_IS_BOOL(types[{ParameterIndex}])";
                    break;
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.WideChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                    condition = $"NAPI_IS_INT32(types[{ParameterIndex}], args[{ParameterIndex}])";
                    break;
                case PrimitiveType.UInt:
                    condition = $"NAPI_IS_UINT32(types[{ParameterIndex}], args[{ParameterIndex}])";
                    break;
                case PrimitiveType.LongLong:
                    condition = $"NAPI_IS_INT64(types[{ParameterIndex}], args[{ParameterIndex}])";
                    break;
                case PrimitiveType.ULongLong:
                    condition = $"NAPI_IS_UINT64(types[{ParameterIndex}], args[{ParameterIndex}])";
                    break;
                case PrimitiveType.Null:
                    condition = $"NAPI_IS_NULL(types[{ParameterIndex}])";
                    break;
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                    condition = $"NAPI_IS_NUMBER(types[{ParameterIndex}])";
                    break;
                case PrimitiveType.Void:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.String:
                case PrimitiveType.Decimal:
                default:
                    throw new NotImplementedException();
            }

            Write(condition);
            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration is Enumeration)
            {
                VisitPrimitiveType(PrimitiveType.Int, quals);
                return true;
            }

            Write($"NAPI_IS_OBJECT(types[{ParameterIndex}])");
            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            Write($"NAPI_IS_ARRAY(types[{ParameterIndex}])");
            return true;
        }
    }
}