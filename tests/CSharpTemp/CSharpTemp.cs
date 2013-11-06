using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CSharpTempTests : LibraryTest
    {
        public CSharpTempTests(GeneratorKind kind)
            : base("CSharpTemp", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateInterfacesForMultipleInheritance = true;
            driver.Options.GenerateProperties = true;
            driver.Options.GenerateVirtualTables = true;
        }

        public override void Postprocess(Driver driver, ASTContext lib)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate,
                RenameCasePattern.UpperCamelCase).VisitLibrary(driver.ASTContext);
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CSharpTempTests(GeneratorKind.CSharp));
        }
    }
}

