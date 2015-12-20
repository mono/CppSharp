using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class EncodingsTestsGenerator : GeneratorTest
    {
        public EncodingsTestsGenerator(GeneratorKind kind)
            : base("Encodings", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.Encoding = Encoding.Unicode;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {

        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new EncodingsTestsGenerator(GeneratorKind.CSharp));
        }
    }
}
