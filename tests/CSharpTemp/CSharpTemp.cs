using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CSharpTempTests : LibraryTest
    {
        public CSharpTempTests(GeneratorKind kind)
            : base("CSharpTemp", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateInterfacesForMultipleInheritance = true;
            driver.Options.GenerateProperties = true;
            driver.Options.GenerateVirtualTables = true;
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CSharpTempTests(GeneratorKind.CSharp));
        }
    }
}

