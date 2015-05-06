using System;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for ambiguous functions/method declarations.
    /// Example:
    /// 
    /// struct S
    /// {
    ///     void Foo(int a, int b = 0);
    ///     void Foo(int a);
    /// 
    ///     void Bar();
    ///     void Bar() const;
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
                    CheckDefaultParametersForAmbiguity(function, overload))
                {
                    function.IsAmbiguous = true;
                    overload.IsAmbiguous = true;
                }
            }

            if (function.IsAmbiguous)
                Driver.Diagnostics.Debug("Found ambiguous overload: {0}",
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
            if (function is Method && overload is Method)
            {
                var method1 = function as Method;
                var method2 = overload as Method;

                var sameParams = method1.Parameters.SequenceEqual(method2.Parameters, new ParameterTypeComparer());

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
    }
}
