using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CommonTestsGenerator : GeneratorTest
    {
        public CommonTestsGenerator(GeneratorKind kind)
            : base("Common", kind)
        {

        }

        public override void Setup(Driver driver)
        {
            base.Setup(driver);

            driver.Options.OutputNamespace = "CommonTest";
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateProperties = true;
            driver.Options.GenerateConversionOperators = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            driver.AddTranslationUnitPass(new GetterSetterToPropertyPass());
            driver.AddTranslationUnitPass(new CheckMacroPass());
            ctx.SetClassAsValueType("Bar");
            ctx.SetClassAsValueType("Bar2");
            ctx.IgnoreClassWithName("IgnoredType");

            ctx.FindCompleteClass("Foo").Enums.First(
                e => string.IsNullOrEmpty(e.Name)).Name = "RenamedEmptyEnum";
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CommonTestsGenerator(GeneratorKind.CLI));
            ConsoleDriver.Run(new CommonTestsGenerator(GeneratorKind.CSharp));
        }
    }
}
