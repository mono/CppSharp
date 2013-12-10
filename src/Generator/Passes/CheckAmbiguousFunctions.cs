using System;
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
            if (AlreadyVisited(function))
                return false;

            if (function.IsAmbiguous)
                return false;

            var overloads = function.Namespace.GetFunctionOverloads(function);

            foreach (var overload in overloads)
            {
                if (function.OperatorKind == CXXOperatorKind.Conversion)
                    continue;

                if (overload == function) continue;

                if (overload.Ignore) continue;

                if (!CheckDefaultParameters(function, overload))
                    continue;

                if (!CheckConstness(function, overload))
                    continue;

                function.IsAmbiguous = true;
                overload.IsAmbiguous = true;
            }

            if (function.IsAmbiguous)
                Driver.Diagnostics.Debug("Found ambiguous overload: {0}",
                    function.QualifiedOriginalName);

            return true;
        }

        static bool CheckDefaultParameters(Function function, Function overload)
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
                overload.ExplicityIgnored = true;
            else
                function.ExplicityIgnored = true;

            return true;
        }

        static bool CheckConstness(Function function, Function overload)
        {
            if (function is Method && overload is Method)
            {
                var method1 = function as Method;
                var method2 = overload as Method;

                if (method1.IsConst && !method2.IsConst)
                {
                    method1.ExplicityIgnored = true;
                    return false;
                }

                if (method2.IsConst && !method1.IsConst)
                {
                    method2.ExplicityIgnored = true;
                    return false;
                }
            }

            return true;
        }
    }
}
