using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
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

        private bool CheckDefaultParametersForAmbiguity(Function function, Function overload)
        {
            List<Parameter> functionParams = RemoveOperatorParams(function);
            List<Parameter> overloadParams = RemoveOperatorParams(overload);

            var commonParameters = Math.Min(functionParams.Count, overloadParams.Count);

            var i = 0;
            for (; i < commonParameters; ++i)
            {
                AST.Type funcType = GetFinalType(functionParams[i]);
                AST.Type overloadType = GetFinalType(overloadParams[i]);

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

            if (functionParams.Count > overloadParams.Count)
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

        private AST.Type GetFinalType(Parameter parameter)
        {
            TypeMap typeMap;
            if (Context.TypeMaps.FindTypeMap(parameter.Type, out typeMap))
            {
                var typePrinterContext = new TypePrinterContext
                {
                    Kind = TypePrinterContextKind.Managed,
                    Parameter = parameter,
                    Type = typeMap.Type
                };

                switch (Options.GeneratorKind)
                {
                    case Generators.GeneratorKind.CLI:
                        return typeMap.CLISignatureType(typePrinterContext).Desugar();
                    case Generators.GeneratorKind.CSharp:
                        return typeMap.CSharpSignatureType(typePrinterContext).Desugar();
                }
            }

            return parameter.Type.Desugar();
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

        private static bool CheckSingleParameterPointerConstnessForAmbiguity(
            Function function, Function overload)
        {
            var functionParams = function.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).ToList();

            // It's difficult to handle this case for more than one parameter.
            // For example, if we have:
            //
            //     void f(float&, const int&);
            //     void f(const float&, int&);
            //
            // What should we do? Generate both? Generate the first one encountered?
            // Generate the one with the least amount of "complex" parameters?
            // So let's just start with the simplest case for the time being.
            
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
