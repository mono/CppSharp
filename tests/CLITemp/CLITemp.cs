using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CLITemp : LibraryTest
    {
        public CLITemp(GeneratorKind kind)
            : base("CLITemp", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext lib)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITemp(GeneratorKind.CLI));
        }
    }
}
