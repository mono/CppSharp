using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CLITemp : GeneratorTest
    {
        public CLITemp(GeneratorKind kind)
            : base("CLITemp", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITemp(GeneratorKind.CLI));
        }
    }
}
