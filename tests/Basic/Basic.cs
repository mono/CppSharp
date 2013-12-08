using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class Basic : LibraryTest
    {
        public Basic(GeneratorKind kind)
            : base("Basic", kind)
        {

        }

        public override void SetupPasses(Driver driver)
        {
            if (driver.Options.IsCSharpGenerator)
            {
                driver.Options.GenerateAbstractImpls = true;                
            }
        }

        public override void Preprocess(Driver driver, ASTContext lib)
        {
            lib.SetClassAsValueType("Bar");
            lib.SetClassAsValueType("Bar2");
            lib.SetMethodParameterUsage("Hello", "TestPrimitiveOut", 1, ParameterUsage.Out);
            lib.SetMethodParameterUsage("Hello", "TestPrimitiveOutRef", 1, ParameterUsage.Out);
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new Basic(GeneratorKind.CLI));
            ConsoleDriver.Run(new Basic(GeneratorKind.CSharp));
        }
    }
}
