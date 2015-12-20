using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace CppSharp.Tests
{

    public class NamespacesBaseTests : GeneratorTest
    {
        public NamespacesBaseTests(GeneratorKind kind)
            : base("NamespacesBase", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GeneratePropertiesAdvanced = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate | RenameTargets.Variable,
                RenameCasePattern.UpperCamelCase).VisitLibrary(driver.ASTContext);
        }

    }
    public class NamespacesBase {

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new NamespacesBaseTests(GeneratorKind.CSharp));
        }
    }
}
