using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

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
            if (AlreadyVisited(method) || !method.IsGenerated || !method.IsConstructor
                || method.IsCopyConstructor
                // conversion operators can only be public
                || method.Access != AccessSpecifier.Public)
                return false;

            var @params = method.Parameters.Where(p => p.Kind == ParameterKind.Regular).ToList();
            if (@params.Count == 0)
                return false;

            if (Options.GenerateDefaultValuesForArguments)
            {
                var nonDefaultParams = @params.Count(p => p.DefaultArgument == null ||
                    (p.DefaultArgument.Class == StatementClass.Call &&
                     p.DefaultArgument.Declaration.Ignore));
                if (nonDefaultParams > 1)
                    return false;   
            }
            else
            {
                if (@params.Count != 1)
                    return false;
            }

            if (method.Parameters[0].Type.IsDependentPointer())
                return false;

            var parameter = method.Parameters[0];
            Class @class;
            var paramType = (parameter.Type.GetFinalPointee() ?? parameter.Type).Desugar();
            if (paramType.TryGetClass(out @class) && @class == method.Namespace)
                return false;

            // TODO: disable implicit operators for C++/CLI because they seem not to be support parameters
            if (!Options.IsCSharpGenerator)
            {
                var pointerType = parameter.Type as PointerType;
                if (pointerType != null && !pointerType.IsReference)
                    return false;
            }
            var qualifiedPointee = parameter.Type.GetFinalPointee() ?? parameter.Type;
            Class castFromClass;
            var castToClass = method.OriginalNamespace as Class;
            if (qualifiedPointee.TryGetClass(out castFromClass))
            {
                if (castToClass == null || castToClass.IsAbstract)
                    return false;
                if (ConvertsBetweenDerivedTypes(castToClass, castFromClass))
                    return false;
            }
            if (castToClass != null && castToClass.IsAbstract)
                return false;
            var operatorKind = method.IsExplicit
                ? CXXOperatorKind.ExplicitConversion
                : CXXOperatorKind.Conversion;
            var qualifiedCastToType = new QualifiedType(new TagType(method.Namespace));
            var conversionOperator = new Method
            {
                Name = Operators.GetOperatorIdentifier(operatorKind),
                Namespace = method.Namespace,
                Kind = CXXMethodKind.Conversion,
                SynthKind = FunctionSynthKind.ComplementOperator,
                ConversionType = qualifiedCastToType,
                ReturnType = qualifiedCastToType,
                OperatorKind = operatorKind,
                IsExplicit = method.IsExplicit,
                FunctionType = method.FunctionType
            };
            conversionOperator.Parameters.Add(new Parameter(parameter)
                {
                    DefaultArgument = null,
                    OriginalDefaultArgument = null
                });
            ((Class) method.Namespace).Methods.Add(conversionOperator);
            return true;
        }

        private static bool ConvertsBetweenDerivedTypes(Class castToClass, Class castFromClass)
        {
            var @base = castToClass;
            while (@base != null)
            {
                if (@base == castFromClass)
                    return true;
                @base = @base.BaseClass;
            }
            @base = castFromClass;
            while (@base != null)
            {
                if (@base == castToClass)
                    return true;
                @base = @base.BaseClass;
            }
            return false;
        }
    }
}
