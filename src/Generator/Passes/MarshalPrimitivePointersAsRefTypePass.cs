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
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) ||
                function.OperatorKind == CXXOperatorKind.Conversion ||
                function.OperatorKind == CXXOperatorKind.ExplicitConversion)
                return false;

            foreach (var param in function.Parameters.Where(
                p => !p.IsOut && !p.QualifiedType.IsConstRefToPrimitive() &&
                    p.Type.Desugar().IsPrimitiveTypeConvertibleToRef()))
                param.Usage = ParameterUsage.InOut;

            return true;
        }
    }
}