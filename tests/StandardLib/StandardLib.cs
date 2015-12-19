using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    public class StandardLibTestsGenerator : GeneratorTest
    {
        public StandardLibTestsGenerator(GeneratorKind kind)
            : base("StandardLib", kind)
        {
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("IntWrapperValueType");
        }

        public static int Main(string[] args)
        {
            try
            {
                ConsoleDriver.Run(new StandardLibTestsGenerator(GeneratorKind.CLI));
                return 0;
            }
            catch (ArgumentException)
            {
                return 1;
            }
        }
    }
}
