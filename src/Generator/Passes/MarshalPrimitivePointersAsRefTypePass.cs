using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Types;

namespace CppSharp.Passes
{
    public class MarshalPrimitivePointersAsRefTypePass : TranslationUnitPass
    {
        private static List<PrimitiveType> AllowedToHaveDefaultPtrVals = new List<PrimitiveType> {PrimitiveType.Bool, PrimitiveType.Double, PrimitiveType.Float,
                                                                         PrimitiveType.Int, PrimitiveType.Long, PrimitiveType.LongLong, PrimitiveType.Short,
                                                                         PrimitiveType.UInt, PrimitiveType.ULong, PrimitiveType.ULongLong, PrimitiveType.UShort};

        public MarshalPrimitivePointersAsRefTypePass()
        {
        }

        public override bool VisitFunctionDecl(Function function)
        {
            bool result = base.VisitFunctionDecl(function);
            foreach(Parameter param in function.Parameters.Where(p => p.Type.IsPointerToPrimitiveType()
                                          && AllowedToHaveDefaultPtrVals.Any(defPtrType => p.Type.IsPointerToPrimitiveType(defPtrType))))
            {
                param.Usage = ParameterUsage.InOut;
            }
            return result;
        }

    }
}
