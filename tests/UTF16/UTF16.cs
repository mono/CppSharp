using System.Text;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class UTF16Tests : GeneratorTest
    {
        public UTF16Tests(GeneratorKind kind)
            : base("UTF16", kind)
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
            ConsoleDriver.Run(new UTF16Tests(GeneratorKind.CSharp));
        }
    }
}
