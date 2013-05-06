using System.Collections.Generic;

namespace CppSharp.AST
{
    public static class Utils
    {
        public static IList<Function> GetFunctionOverloads(Function function,
            Class @class = null)
        {
            var overloads = new List<Function>();

            if (@class == null)
            {
                var @namespace = function.Namespace;
                return @namespace.GetFunctionOverloads(function);
            }

            return @class.GetFunctionOverloads(function);
        }
    }
}
