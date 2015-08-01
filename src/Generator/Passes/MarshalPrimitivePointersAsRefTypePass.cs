using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class MarshalPrimitivePointersAsRefTypePass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            foreach (var param in function.Parameters.Where(
                p => !p.IsOut && p.IsPrimitiveParameterConvertibleToRef()))
                param.Usage = ParameterUsage.InOut;
            return base.VisitFunctionDecl(function);
        }
    }
}