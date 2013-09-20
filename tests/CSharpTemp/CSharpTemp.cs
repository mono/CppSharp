using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class CSharpTempTests : LibraryTest
    {
        public CSharpTempTests(LanguageGeneratorKind kind)
            : base("CSharpTemp", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateInterfacesForMultipleInheritance = true;
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                ConsoleDriver.Run(new CSharpTempTests(LanguageGeneratorKind.CSharp));
            }
        }
    }
}

