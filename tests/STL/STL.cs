using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class STL : GeneratorTest
    {
        public STL(GeneratorKind kind)
            : base("STL", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("IntWrapperValueType");
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new STL(GeneratorKind.CLI));
        }
    }
}
