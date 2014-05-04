using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass will create conversion operators out of single argument
    /// constructors.
    /// </summary>
    public class ConstructorToConversionOperatorPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!method.IsConstructor)
                return false;
            if (method.IsCopyConstructor)
                return false;
            if (method.Parameters.Count != 1)
                return false;
            var parameter = method.Parameters[0];
            var parameterType = parameter.Type as PointerType;
            if (parameterType == null)
                return false;
            if (!parameterType.IsReference)
                return false;
            var qualifiedPointee = parameterType.QualifiedPointee;
            Class castFromClass;
            if (!qualifiedPointee.Type.IsTagDecl(out castFromClass))
                return false;
            var castToClass = method.OriginalNamespace as Class;
            if (castToClass == null)
                return false;
            if (castFromClass == castToClass)
                return false;

            var operatorKind = method.IsExplicit
                ? CXXOperatorKind.ExplicitConversion
                : CXXOperatorKind.Conversion;
            var castToType = new TagType(castToClass);
            var qualifiedCastToType = new QualifiedType(castToType);
            var conversionOperator = new Method()
            {
                Name = Operators.GetOperatorIdentifier(operatorKind),
                Namespace = castFromClass,
                Kind = CXXMethodKind.Conversion,
                IsSynthetized = true,
                ConversionType = qualifiedCastToType,
                ReturnType = qualifiedCastToType
            };
            conversionOperator.OperatorKind = operatorKind;

            castFromClass.Methods.Add(conversionOperator);
            return true;
        }
    }
}
