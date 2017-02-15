using CppSharp.AST;
using CppSharp.Types;
using System;

namespace CppSharp.Generators
{
    public class TypePrinterResult
    {
        public string Type;
        public TypeMap TypeMap;
        public string NameSuffix;

        public static implicit operator TypePrinterResult(string type)
        {
            return new TypePrinterResult { Type = type };
        }

        public override string ToString() => Type;
    }
}