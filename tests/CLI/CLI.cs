using System;
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

        public static int Main(string[] args)
        {
            try
            {
                ConsoleDriver.Run(new CLITestsGenerator(GeneratorKind.CLI));
                return 0;
            }
            catch (ArgumentException)
            {
                return 1;
            }
        }
    }
}
