using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class VTableTests : LibraryTest
    {
        public VTableTests(GeneratorKind kind)
            : base("VTables", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateVirtualTables = true;
            driver.TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any);
            driver.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
        }

        public override void Preprocess(Driver driver, ASTContext lib)
        {

        }

        static class Program
        {
            public static void Main(string[] args)
            {
                ConsoleDriver.Run(new VTableTests(GeneratorKind.CLI));
                ConsoleDriver.Run(new VTableTests(GeneratorKind.CSharp));
            }
        }
    }
}
