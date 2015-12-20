using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class StandardLibTestsGenerator : GeneratorTest
    {
        public StandardLibTestsGenerator(GeneratorKind kind)
            : base("StandardLib", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("IntWrapperValueType");
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new StandardLibTestsGenerator(GeneratorKind.CLI));
        }
    }
}
