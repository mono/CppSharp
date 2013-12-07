using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class STL : LibraryTest
    {
        public STL(GeneratorKind kind)
            : base("STL", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext lib)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new STL(GeneratorKind.CLI));
        }
    }
}
