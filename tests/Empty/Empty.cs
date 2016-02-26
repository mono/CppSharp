using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class EmptyTestsGenerator : GeneratorTest
    {
        public EmptyTestsGenerator(GeneratorKind kind)
            : base("Empty", kind)
        {
        }

        public override void Setup(Driver driver)
        {
            base.Setup(driver);
            driver.Options.OutputNamespace = "EmptyTest";
        }

        public override void SetupPasses(Driver driver)
        {
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new EmptyTestsGenerator(GeneratorKind.CSharp));
            ConsoleDriver.Run(new EmptyTestsGenerator(GeneratorKind.CLI));
        }
    }
}
