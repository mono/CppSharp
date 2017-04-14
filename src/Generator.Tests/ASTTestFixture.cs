using System;
using CppSharp.AST;
using CppSharp.Utils;
using CppSharp.Parser;

namespace CppSharp.Generator.Tests
{
    public class ASTTestFixture
    {
        protected Driver Driver;
        protected DriverOptions Options;
        protected ParserOptions ParserOptions;
        protected ASTContext AstContext;

        protected void ParseLibrary(params string[] files)
        {
            Options = new DriverOptions();
            ParserOptions = new ParserOptions();

            var testsPath = GeneratorTest.GetTestsDirectory("Native");
            ParserOptions.AddIncludeDirs(testsPath);

            var module = Options.AddModule("Test");
            module.Headers.AddRange(files);

            Driver = new Driver(Options)
            {
                ParserOptions = this.ParserOptions
            };

            Driver.Setup();
            Driver.BuildParseOptions();
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            AstContext = Driver.Context.ASTContext;
        }
    }
}
