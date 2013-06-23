using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class Hello : LibraryTest
    {
        public Hello(LanguageGeneratorKind kind)
            : base("Hello", kind)
        {
        }

        public override void Preprocess(Driver driver, Library lib)
        {
            lib.SetClassAsValueType("Bar");
            lib.SetClassAsValueType("Bar2");
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                Driver.Run(new Hello(LanguageGeneratorKind.CPlusPlusCLI));
                Driver.Run(new Hello(LanguageGeneratorKind.CSharp));
            }
        }
    }
}
