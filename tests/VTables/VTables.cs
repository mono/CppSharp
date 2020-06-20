using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class VTableTests : GeneratorTest
    {
        public VTableTests(GeneratorKind kind)
            : base("VTables", kind)
        {
        }

        public override void Setup(Driver driver)
        {
            base.Setup(driver);

            driver.ParserOptions.EnableRTTI = true;
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Context.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {

        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new VTableTests(GeneratorKind.CLI));
            ConsoleDriver.Run(new VTableTests(GeneratorKind.CSharp));
        }
    }
}
