using System.Linq;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using Interop = System.Runtime.InteropServices;

namespace CppSharp.Generators
{
    public static class ExtensionMethods
    {
        public static Interop.CallingConvention ToInteropCallConv(this CallingConvention convention)
        {
            switch (convention)
            {
                case CallingConvention.Default:
                    return Interop.CallingConvention.Winapi;
                case CallingConvention.C:
                    return Interop.CallingConvention.Cdecl;
                case CallingConvention.StdCall:
                    return Interop.CallingConvention.StdCall;
                case CallingConvention.ThisCall:
                    return Interop.CallingConvention.ThisCall;
                case CallingConvention.FastCall:
                    return Interop.CallingConvention.FastCall;
            }

            return Interop.CallingConvention.Winapi;
        }

        public static bool IsPrimitiveTypeConvertibleToRef(this Type type)
        {
            var allowedToHaveDefaultPtrVals = new List<PrimitiveType>
            {
                PrimitiveType.Bool,
                PrimitiveType.Double,
                PrimitiveType.Float,
                PrimitiveType.Int,
                PrimitiveType.Long,
                PrimitiveType.LongLong,
                PrimitiveType.Short,
                PrimitiveType.UInt,
                PrimitiveType.ULong,
                PrimitiveType.ULongLong,
                PrimitiveType.UShort
            };
            return type.IsPointerToPrimitiveType() &&
                allowedToHaveDefaultPtrVals.Any(type.IsPointerToPrimitiveType);
        }
    }
}
