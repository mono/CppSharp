using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class ParamTypeToInterfacePass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsOperator || function.Parameters.Count > 1)
                ChangeToInterfaceType(function.OriginalReturnType);

            if (function.OperatorKind != CXXOperatorKind.Conversion && 
                function.OperatorKind != CXXOperatorKind.ExplicitConversion)
            {
                foreach (var parameter in function.Parameters.Where(p => p.Kind != ParameterKind.OperatorParameter))
                    ChangeToInterfaceType(parameter.QualifiedType);
            }
            
            return base.VisitFunctionDecl(function);
        }

        private static void ChangeToInterfaceType(QualifiedType type)
        {
            var tagType = (type.Type.GetFinalPointee() ?? type.Type) as TagType;
            if (tagType != null)
            {
                var @class = tagType.Declaration as Class;
                if (@class != null)
                {
                    var @interface = @class.Namespace.Classes.Find(c => c.OriginalClass == @class);
                    if (@interface != null)
                        tagType.Declaration = @interface;
                }
            }
        }
    }
}
