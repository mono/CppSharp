using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

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

            if (function.IsAmbiguous)
                return false;

            var overloads = function.Namespace.GetOverloads(function);

            foreach (var overload in overloads)
            {
                if (function.OperatorKind == CXXOperatorKind.Conversion)
                    continue;
                if (function.OperatorKind == CXXOperatorKind.ExplicitConversion)
                    continue;

                if (overload == function) continue;

                if (!overload.IsGenerated) continue;

                if (CheckConstnessForAmbiguity(function, overload) ||
                    CheckDefaultParametersForAmbiguity(function, overload) ||
                    CheckSingleParameterPointerConstnessForAmbiguity(function, overload))
                {
                    function.IsAmbiguous = true;
                    overload.IsAmbiguous = true;
                }
            }

            if (function.IsAmbiguous)
                Diagnostics.Debug("Found ambiguous overload: {0}",
                    function.QualifiedOriginalName);

            return true;
        }

        private static bool CheckDefaultParametersForAmbiguity(Function function, Function overload)
        {
            var commonParameters = Math.Min(function.Parameters.Count, overload.Parameters.Count);

            var i = 0;
            for (; i < commonParameters; ++i)
            {
                var funcParam = function.Parameters[i];
                var overloadParam = overload.Parameters[i];

                if (!funcParam.QualifiedType.Equals(overloadParam.QualifiedType))
                    return false;
            }

            for (; i < function.Parameters.Count; ++i)
            {
                var funcParam = function.Parameters[i];
                if (!funcParam.HasDefaultValue)
                    return false;
            }

            for (; i < overload.Parameters.Count; ++i)
            {
                var overloadParam = overload.Parameters[i];
                if (!overloadParam.HasDefaultValue)
                    return false;
            }

            if (function.Parameters.Count > overload.Parameters.Count)
                overload.ExplicitlyIgnore();
            else
                function.ExplicitlyIgnore();

            return true;
        }

        private static bool CheckConstnessForAmbiguity(Function function, Function overload)
        {
            var method1 = function as Method;
            var method2 = overload as Method;
            if (method1 != null && method2 != null)
            {
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
            }

            return false;
        }

        private static bool CheckSingleParameterPointerConstnessForAmbiguity(
            Function function, Function overload)
        {
            var functionParams = function.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).ToList();
            // It's difficult to handle this case for more than one parameter
            // For example, if we have:
            //     void f(float&, const int&);
            //     void f(const float&, int&);
            // what should we do? Generate both? Generate the first one encountered?
            // Generate the one with the least amount of "complex" parameters?
            // So let's just start with the simplest case for the time being
            if (functionParams.Count != 1)
                return false;
            var overloadParams = overload.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).ToList();
            if (overloadParams.Count != 1)
                return false;

            var parameterFunction = functionParams[0];
            var parameterOverload = overloadParams[0];

            var pointerParamFunction = parameterFunction.Type.Desugar() as PointerType;
            var pointerParamOverload = parameterOverload.Type.Desugar() as PointerType;

            if (pointerParamFunction == null || pointerParamOverload == null)
                return false;

            if (!pointerParamFunction.GetPointee().Equals(pointerParamOverload.GetPointee()))
                return false;

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

            return false;
        }
    }
}
