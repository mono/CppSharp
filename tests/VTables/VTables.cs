using System;
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

        public override void SetupPasses(Driver driver)
        {
            driver.TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any);
            driver.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {

        }

        public static int Main(string[] args)
        {
            try
            {
                ConsoleDriver.Run(new VTableTests(GeneratorKind.CLI));
                ConsoleDriver.Run(new VTableTests(GeneratorKind.CSharp));
                return 0;
            }
            catch (ArgumentException)
            {
                return 1;
            }
        }
    }
}
