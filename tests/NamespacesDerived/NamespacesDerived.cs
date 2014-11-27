using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

namespace CppSharp.Tests
{

    public class NamespacesDerivedTests : GeneratorTest
    {
        Dictionary<string, DeclInfo> input;

        public NamespacesDerivedTests(GeneratorKind kind, Dictionary<string, DeclInfo> input)
            : base("NamespacesDerived", kind)
        {
            this.input = input;
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.DependentNameSpaces.Add("NamespacesBase");
            driver.Options.ImportNames = input;
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

            BinaryFormatter formatter = new BinaryFormatter();

            Stream r_stream = new FileStream("baseOutput.bin",
                         FileMode.Open,
                         FileAccess.Read, FileShare.Read);
            var baseOutput = (Dictionary<string, DeclInfo>)formatter.Deserialize(r_stream);

            ConsoleDriver.Run(new NamespacesDerivedTests(GeneratorKind.CSharp, baseOutput));

            r_stream.Close();

        }

    }
}

