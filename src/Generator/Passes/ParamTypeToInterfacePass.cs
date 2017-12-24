using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class ParamTypeToInterfacePass : TranslationUnitPass
    {
        public ParamTypeToInterfacePass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            if (!function.IsOperator || function.Parameters.Count > 1)
            {
                var originalReturnType = function.OriginalReturnType;
                ChangeToInterfaceType(ref originalReturnType);
                function.OriginalReturnType = originalReturnType;
            }

            if (function.OperatorKind != CXXOperatorKind.Conversion &&
                function.OperatorKind != CXXOperatorKind.ExplicitConversion)
                foreach (var parameter in function.Parameters.Where(
                    p => p.Kind != ParameterKind.OperatorParameter))
                {
                    var qualifiedType = parameter.QualifiedType;
                    ChangeToInterfaceType(ref qualifiedType);
                    parameter.QualifiedType = qualifiedType;
                }

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            if (!base.VisitProperty(property))
                return false;

            var type = property.QualifiedType;
            ChangeToInterfaceType(ref type);
            property.QualifiedType = type;
            return true;
        }

        private static void ChangeToInterfaceType(ref QualifiedType type)
        {
            var finalType = (type.Type.GetFinalPointee() ?? type.Type).Desugar();
            Class @class;
            if (!finalType.TryGetClass(out @class))
                return;

            Class @interface = @class.GetInterface();
            if (@interface == null)
                return;

            type.Type = (Type) type.Type.Clone();
            finalType = (type.Type.GetFinalPointee() ?? type.Type).Desugar();
            finalType.TryGetClass(out @class, @interface);
        }
    }
}
