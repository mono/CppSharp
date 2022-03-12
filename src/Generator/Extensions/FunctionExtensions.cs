using CppSharp.AST;

namespace CppSharp.Extensions
{
    public static class FunctionExtensions
    {
        public static bool IsNativeMethod(this Function function)
        {
            var method = function as Method;
            if (method == null)
                return false;

            return method.Conversion == MethodConversionKind.None;
        }
    }
}