?using System;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Tests
{
    public class TestAttributesPass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (AlreadyVisited(function) || function.Name != "obsolete")
                return false;

            var attribute = new Attribute
            {
                Type = typeof(ObsoleteAttribute),
                Value = string.Format("\"{0} is obsolete.\"", function.Name)
            };

            function.Attributes.Add(attribute);

            return base.VisitFunctionDecl(function);
        }
    }

    public class NamespacesBaseTests : GeneratorTest
    {
        public NamespacesBaseTests(GeneratorKind kind)
            : base("NamespacesBase", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateAbstractImpls = true;
            driver.Options.GenerateInterfacesForMultipleInheritance = true;
            driver.Options.GeneratePropertiesAdvanced = true;
            driver.Options.GenerateVirtualTables = true;
            driver.Options.GenerateCopyConstructors = true;
            // To ensure that calls to constructors in conversion operators
            // are not ambiguous with multiple inheritance pass enabled.
            driver.Options.GenerateConversionOperators = true;
            driver.TranslationUnitPasses.AddPass(new TestAttributesPass());
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
            driver.Options.GenerateSingleCSharpFile = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("TestCopyConstructorVal");
            ctx.SetClassAsValueType("QGenericArgument");

            ctx.IgnoreClassWithName("IgnoredTypeInheritingNonIgnoredWithNoEmptyCtor");
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate,
                RenameCasePattern.UpperCamelCase).VisitLibrary(driver.ASTContext);
        }

    }
    public class Namespaces {

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new NamespacesBaseTests(GeneratorKind.CSharp));
        }

    }
}

