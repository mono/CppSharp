using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for ambiguous functions/method declarations.
    ///
    /// Example:
    /// struct S
    /// {
    ///     void Foo(int a, int b = 0);
    ///     void Foo(int a);
    ///     void Bar();
    ///     void Bar() const;
    /// 
    ///     void Qux(int&amp; i);
    ///     void Qux(int&amp;&amp; i);
    ///     void Qux(const int&amp; i);
    /// };
    ///
    /// When we call Foo(0) the compiler will not know which call we want and
    /// will error out so we need to detect this and either ignore the methods
    /// or flag them such that the generator can explicitly disambiguate when
    /// generating the call to the native code.
    /// </summary>
    public class CheckAmbiguousFunctions : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(AST.Function function)
        {
            if (!VisitDeclaration(function))
                return false;

            if (function.IsAmbiguous || !function.IsGenerated)
                return false;

            var overloads = function.Namespace.GetOverloads(function);

            foreach (var overload in overloads)
            {
                if (overload == function) continue;

                if (!overload.IsGenerated) continue;

                var ambiguous =
                    function.OperatorKind == CXXOperatorKind.Conversion ||
                    function.OperatorKind == CXXOperatorKind.ExplicitConversion
                    ? CheckConversionAmbiguity(function, overload)
                    : CheckConstnessForAmbiguity(function, overload) ||
                      CheckDefaultParametersForAmbiguity(function, overload) ||
                      CheckParametersPointerConstnessForAmbiguity(function, overload);

                if (ambiguous)
                {
                    function.IsAmbiguous = true;
                    overload.IsAmbiguous = true;
                }
            }

            if (function.IsAmbiguous)
                Diagnostics.Debug($"Found ambiguous overload: {function.QualifiedOriginalName}");

            return true;
        }

        private bool CheckDefaultParametersForAmbiguity(Function function, Function overload)
        {
            List<Parameter> functionParams = RemoveOperatorParams(function);
            List<Parameter> overloadParams = RemoveOperatorParams(overload);

            var commonParameters = Math.Min(functionParams.Count, overloadParams.Count);

            int functionMappedParams = 0;
            int overloadMappedParams = 0;

            var i = 0;
            for (; i < commonParameters; ++i)
            {
                AST.Type funcOriginalType = functionParams[i].Type.Desugar();
                AST.Type funcType = funcOriginalType.GetMappedType(
                    TypeMaps, Options.GeneratorKind);
                AST.Type overloadOriginalType = overloadParams[i].Type.Desugar();
                AST.Type overloadType = overloadOriginalType.GetMappedType(
                    TypeMaps, Options.GeneratorKind);

                if (!funcType.Equals(funcOriginalType))
                    functionMappedParams++;

                if (!overloadType.Equals(overloadOriginalType))
                    overloadMappedParams++;

                AST.Type funcPointee = funcType.GetFinalPointee() ?? funcType;
                AST.Type overloadPointee = overloadType.GetFinalPointee() ?? overloadType;

                if (((funcPointee.IsPrimitiveType() || overloadPointee.IsPrimitiveType()) &&
                      !funcType.Equals(overloadType)) ||
                    !funcPointee.Equals(overloadPointee))
                    return false;
            }

            for (; i < functionParams.Count; ++i)
            {
                var funcParam = functionParams[i];
                if (!funcParam.HasDefaultValue)
                    return false;
            }

            for (; i < overloadParams.Count; ++i)
            {
                var overloadParam = overloadParams[i];
                if (!overloadParam.HasDefaultValue)
                    return false;
            }

            if (functionParams.Count > overloadParams.Count ||
                functionMappedParams < overloadMappedParams)
                overload.ExplicitlyIgnore();
            else
                function.ExplicitlyIgnore();

            return true;
        }

        private List<Parameter> RemoveOperatorParams(Function function)
        {
            var functionParams = new List<Parameter>(function.Parameters);

            if (!function.IsOperator ||
                (Context.Options.GeneratorKind != GeneratorKind.CLI &&
                 Context.Options.GeneratorKind != GeneratorKind.CSharp))
                return functionParams;

            // C++ operators in a class have no class param unlike C#
            // but we need to be able to compare them to free operators.
            Parameter param = functionParams.Find(p => p.Kind == ParameterKind.Regular);
            if (param != null)
            {
                AST.Type type = param.Type.Desugar();
                type = (type.GetFinalPointee() ?? type).Desugar();
                Class @class;
                if (type.TryGetClass(out @class) &&
                    function.Namespace == @class)
                    functionParams.Remove(param);
            }

            return functionParams;
        }

        private static bool CheckConstnessForAmbiguity(Function function, Function overload)
        {
            var method1 = function as Method;
            var method2 = overload as Method;
            if (method1 == null || method2 == null)
                return false;

            var sameParams = method1.Parameters.SequenceEqual(method2.Parameters,
                ParameterTypeComparer.Instance);

            if (method1.IsConst && !method2.IsConst && sameParams)
            {
                method1.ExplicitlyIgnore();
                return true;
            }

            if (method2.IsConst && !method1.IsConst && sameParams)
            {
                method2.ExplicitlyIgnore();
                return true;
            }

            return false;
        }

        private bool CheckConversionAmbiguity(Function function, Function overload)
        {
            var method1 = function as Method;
            var method2 = overload as Method;
            if (method1 == null || method2 == null)
                return false;

            var type1 = method1.ReturnType.Type.Desugar();
            var type2 = method2.ReturnType.Type.Desugar();

            if (type1 is PointerType pointerType1 &&
                type2 is PointerType pointerType2)
            {
                type1 = pointerType1.GetPointee();
                type2 = pointerType2.GetPointee();
            }

            var mappedType1 = type1.GetMappedType(TypeMaps, Options.GeneratorKind);
            var mappedType2 = type2.GetMappedType(TypeMaps, Options.GeneratorKind);

            if (mappedType1.Equals(mappedType2))
            {
                method2.ExplicitlyIgnore();
                return true;
            }

            return false;
        }

        private static bool CheckParametersPointerConstnessForAmbiguity(
            Function function, Function overload)
        {
            var functionParams = function.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).ToList();

            var overloadParams = overload.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).ToList();

            if (functionParams.Count != overloadParams.Count)
                return false;

            for (int i = 0; i < functionParams.Count; i++)
            {
                var parameterFunction = functionParams[i];
                var parameterOverload = overloadParams[i];

                var pointerParamFunction = parameterFunction.Type.Desugar() as PointerType;
                var pointerParamOverload = parameterOverload.Type.Desugar() as PointerType;

                if (pointerParamFunction == null || pointerParamOverload == null)
                    continue;

                if (!pointerParamFunction.GetPointee().Equals(pointerParamOverload.GetPointee()))
                    continue;

                if (parameterFunction.IsConst && !parameterOverload.IsConst)
                {
                    function.ExplicitlyIgnore();
                    return true;
                }

                if (parameterOverload.IsConst && !parameterFunction.IsConst)
                {
                    overload.ExplicitlyIgnore();
                    return true;
                }

                if (pointerParamFunction.Modifier == PointerType.TypeModifier.RVReference &&
                    pointerParamOverload.Modifier != PointerType.TypeModifier.RVReference)
                {
                    function.ExplicitlyIgnore();
                    return true;
                }

                if (pointerParamFunction.Modifier != PointerType.TypeModifier.RVReference &&
                    pointerParamOverload.Modifier == PointerType.TypeModifier.RVReference)
                {
                    overload.ExplicitlyIgnore();
                    return true;
                }
            }

            return false;
        }
    }
}
