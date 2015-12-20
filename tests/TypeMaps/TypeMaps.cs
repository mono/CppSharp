using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class TypeMaps : GeneratorTest
    {
        public TypeMaps(GeneratorKind kind)
            : base("TypeMaps", kind)
        {

        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateProperties = true;
            driver.Options.GenerateConversionOperators = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("HasQList");
            ctx.FindCompleteClass("QList").Constructors.First(c => c.IsCopyConstructor).GenerationKind = GenerationKind.None;
            ctx.IgnoreClassWithName("IgnoredType");
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new TypeMaps(GeneratorKind.CLI));
            ConsoleDriver.Run(new TypeMaps(GeneratorKind.CSharp));
        }
    }
}
