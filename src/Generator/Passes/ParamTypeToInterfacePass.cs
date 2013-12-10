using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class ParamTypeToInterfacePass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsOperator)
            {
                if (function.HasIndirectReturnTypeParameter)
                {
                    var parameter = function.Parameters.Find(p => p.Kind == ParameterKind.IndirectReturnType);
                    parameter.QualifiedType = GetInterfaceType(parameter.QualifiedType);
                }
                else
                {
                    function.ReturnType = GetInterfaceType(function.ReturnType);
                }
                foreach (Parameter parameter in function.Parameters.Where(
                    p => p.Kind != ParameterKind.IndirectReturnType))
                {
                    parameter.QualifiedType = GetInterfaceType(parameter.QualifiedType);
                }
            }
            return base.VisitFunctionDecl(function);
        }

        private static QualifiedType GetInterfaceType(QualifiedType type)
        {
            var tagType = type.Type as TagType;
            if (tagType == null)
            {
                var pointerType = type.Type as PointerType;
                if (pointerType != null)
                    tagType = pointerType.Pointee as TagType;
            }
            if (tagType != null)
            {
                var @class = tagType.Declaration as Class;
                if (@class != null)
                {
                    var @interface = @class.Namespace.Classes.Find(c => c.OriginalClass == @class);
                    if (@interface != null)
                        return new QualifiedType(new TagType(@interface));
                }
            }
            return type;
        }
    }
}
