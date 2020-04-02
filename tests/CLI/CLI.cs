using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CLITestsGenerator : GeneratorTest
    {
        public CLITestsGenerator(GeneratorKind kind)
            : base("CLI", kind)
        {
        }

        public override void Setup(Driver driver)
        {
            driver.Options.GenerateFinalizers = true;
            driver.Options.GenerateObjectOverrides = true;
            base.Setup(driver);
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITestsGenerator(GeneratorKind.CLI));
        }
    }
}
