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
            {
                var originalReturnType = function.OriginalReturnType;
                ChangeToInterfaceType(ref originalReturnType);
                function.OriginalReturnType = originalReturnType;
            }

            if (function.OperatorKind != CXXOperatorKind.Conversion &&
                function.OperatorKind != CXXOperatorKind.ExplicitConversion)
                foreach (var parameter in function.Parameters.Where(p => p.Kind != ParameterKind.OperatorParameter))
                {
                    var qualifiedType = parameter.QualifiedType;
                    ChangeToInterfaceType(ref qualifiedType);
                    parameter.QualifiedType = qualifiedType;
                }

            return base.VisitFunctionDecl(function);
        }

        private static void ChangeToInterfaceType(ref QualifiedType type)
        {
            var tagType = (type.Type.GetFinalPointee() ?? type.Type) as TagType;
            if (tagType != null)
            {
                var @class = tagType.Declaration as Class;
                if (@class != null)
                {
                    var @interface = @class.Namespace.Classes.Find(c => c.OriginalClass == @class);
                    if (@interface != null)
                    {
                        type.Type = (Type) type.Type.Clone();
                        ((TagType) (type.Type.GetFinalPointee() ?? type.Type)).Declaration = @interface;
                    }
                }
            }
        }
    }
}
