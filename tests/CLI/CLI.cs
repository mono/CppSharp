using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Types;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    [TypeMap("IgnoredClassTemplateForEmployee")]
    public class IgnoredClassTemplateForEmployeeMap : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CustomType("CLI::Employee^");
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write($"gcnew CLI::Employee({ctx.ReturnVarName}.m_employee)");
        }
    }

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

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITestsGenerator(GeneratorKind.CLI));
        }
    }
}
