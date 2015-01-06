using System;
using CppSharp.AST;
using CppSharp.Generators;
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
            driver.Options.DependentNameSpaces.Add("NamespacesBase");
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            foreach (TranslationUnit unit in ctx.TranslationUnits)
            {
                if (unit.FileName != "NamespacesDerived.h")
                {
                    unit.GenerationKind = GenerationKind.Link;
                }
            }
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
        }

    }

    public class NamespacesDerived {

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new NamespacesDerivedTests(GeneratorKind.CSharp));
        }

    }
}

