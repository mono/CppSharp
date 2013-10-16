using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class Basic : LibraryTest
    {
        public Basic(LanguageGeneratorKind kind)
            : base("Basic", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext lib)
        {
            lib.SetClassAsValueType("Bar");
            lib.SetClassAsValueType("Bar2");
            lib.SetMethodParameterUsage("Hello", "TestPrimitiveOut", 1, ParameterUsage.Out);
            lib.SetMethodParameterUsage("Hello", "TestPrimitiveOutRef", 1, ParameterUsage.Out);
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                ConsoleDriver.Run(new Basic(LanguageGeneratorKind.CLI));
                ConsoleDriver.Run(new Basic(LanguageGeneratorKind.CSharp));
            }
        }
    }
}
