using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
        }

    }
    public class NamespacesBase {

        public static void Main(string[] args)
        {

            ConsoleDriver.Run(new NamespacesBaseTests(GeneratorKind.CSharp));

        }
    }
}