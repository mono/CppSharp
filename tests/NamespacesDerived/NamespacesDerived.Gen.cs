using System.IO;
using System.Linq;
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
            driver.Options.GenerateClassTemplates = true;
            driver.Options.CompileCode = true;
            driver.Options.Modules[1].IncludeDirs.Add(GetTestsDirectory("NamespacesDerived"));
            const string @base = "NamespacesBase";
            var module = driver.Options.AddModule(@base);
            module.IncludeDirs.Add(Path.GetFullPath(GetTestsDirectory(@base)));
            module.Headers.Add($"{@base}.h");
            module.OutputNamespace = @base;
            module.LibraryDirs.AddRange(driver.Options.Modules[1].LibraryDirs);
            module.Libraries.Add($"{@base}.Native");
            driver.Options.Modules[1].Dependencies.Add(module);
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.IgnoreClassWithName("Ignored");
            // operator= for this type isn't necessary for testing
            // while also requiring a large amount of C++ to get valid symbols; better ignore
            foreach (Method @operator in ctx.FindCompleteClass("StdFields").Operators)
            {
                @operator.ExplicitlyIgnore();
            }
        }
    }

    public static class NamespacesDerived
    {
        public static void Main()
        {
            ConsoleDriver.Run(new NamespacesDerivedTests(GeneratorKind.CSharp));
        }
    }
}

