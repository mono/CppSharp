using System.IO;
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

        public override void Setup(Driver driver)
        {
            base.Setup(driver);
            driver.Options.GenerateDefaultValuesForArguments = true;

            driver.Options.Modules[1].IncludeDirs.Add(GetTestsDirectory("NamespacesDerived"));
            var @base = "NamespacesBase";
            var module = driver.Options.AddModule(@base);
            module.IncludeDirs.Add(Path.GetFullPath(GetTestsDirectory(@base)));
            module.Headers.Add($"{@base}.h");
            module.OutputNamespace = @base;
            module.SharedLibraryName = $"{@base}.Native";
            // Workaround for CLR which does not check for .dll if the name already has a dot
            if (System.Type.GetType("Mono.Runtime") == null)
                module.SharedLibraryName += ".dll";
            driver.Options.Modules[1].Dependencies.Add(module);
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            driver.Generator.OnUnitGenerated += o =>
            {
                Block firstBlock = o.Outputs[0].RootBlock.Blocks[1];
                firstBlock.WriteLine("using System.Runtime.CompilerServices;");
                firstBlock.NewLine();
                firstBlock.WriteLine("[assembly:InternalsVisibleTo(\"NamespacesDerived.CSharp\")]");
            };
        }
    }

    public class NamespacesDerived
    {
        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new NamespacesDerivedTests(GeneratorKind.CSharp));
        }
    }
}

