using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{

    public class NamespacesDerivedTests : GeneratorTest
    {
        public NamespacesDerivedTests(GeneratorKind kind)
            : base("NamespacesDerived", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateDefaultValuesForArguments = true;
            driver.Options.GeneratePropertiesAdvanced = true;
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate | RenameTargets.Variable,
                RenameCasePattern.UpperCamelCase).VisitLibrary(driver.ASTContext);
        }
    }

    public class NamespacesDerived {

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new NamespacesBaseTests(GeneratorKind.CSharp));
            ConsoleDriver.Run(new NamespacesDerivedTests(GeneratorKind.CSharp));
        }

    }
}

