using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class MarshalPrimitivePointersAsRefTypePass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) ||
                function.OperatorKind == CXXOperatorKind.Conversion ||
                function.OperatorKind == CXXOperatorKind.ExplicitConversion)
                return false;

            foreach (var param in function.Parameters.Where(
                p => !p.IsOut && p.Type.IsPrimitiveTypeConvertibleToRef()))
                param.Usage = ParameterUsage.InOut;

            return true;
        }
    }
}