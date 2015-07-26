using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class MarshalPrimitivePointersAsRefTypePass : TranslationUnitPass
    {
        public MarshalPrimitivePointersAsRefTypePass()
        {
        }

        public override bool VisitFunctionDecl(Function function)
        {
            foreach(var param in function.Parameters.Where(p => !p.IsOut && p.Type.IsPointerToPrimitiveType()
                                          && ExtensionMethods.AllowedToHaveDefaultPtrVals.Any(defPtrType => p.Type.IsPointerToPrimitiveType(defPtrType))))
            {
                param.Usage = ParameterUsage.InOut;
            }
            return base.VisitFunctionDecl(function);
        }

    }
}
