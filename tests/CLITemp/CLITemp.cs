using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CLITemp : GeneratorTest
    {
        public CLITemp(GeneratorKind kind)
            : base("CLITemp", kind)
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
            driver.TranslationUnitPasses.AddPass(new CheckValueTypeClassesPass());
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITemp(GeneratorKind.CLI));
        }
    }
}
