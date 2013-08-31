using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Utils
{
    public class ParameterTypeComparer : IEqualityComparer<Parameter>
    {
        public bool Equals(Parameter x, Parameter y)
        {
            return x.Type == y.Type;
        }

        public int GetHashCode(Parameter obj)
        {
            return obj.Type.GetHashCode();
        }
    }
}
