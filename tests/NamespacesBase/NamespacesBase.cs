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
        Dictionary<string, DeclInfo> output;
        public NamespacesBaseTests(GeneratorKind kind, Dictionary<string, DeclInfo> output)
            : base("NamespacesBase", kind)
        {
            this.output = output;
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.ExportNames = output;
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
            var output = new Dictionary<string, DeclInfo>();

            ConsoleDriver.Run(new NamespacesBaseTests(GeneratorKind.CSharp, output));

            BinaryFormatter formatter = new BinaryFormatter();
            Stream w_stream = new FileStream("baseOutput.bin",
                         FileMode.Create,
                         FileAccess.Write, FileShare.None);
            formatter.Serialize(w_stream, output);
            w_stream.Close();
        }
    }
}