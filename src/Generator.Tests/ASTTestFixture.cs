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
        protected ParserOptions2 ParserOptions;
        protected ASTContext AstContext;

        protected void ParseLibrary(params string[] files)
        {
            Options = new DriverOptions();
            ParserOptions = new ParserOptions2();

            var testsPath = GeneratorTest.GetTestsDirectory("Native");
            ParserOptions.addIncludeDirs(testsPath);

            Options.Headers.AddRange(files);

            Driver = new Driver(Options, new TextDiagnosticPrinter())
            {
                ParserOptions = this.ParserOptions
            };

            foreach (var module in Driver.Options.Modules)
                module.LibraryName = "Test";
            Driver.Setup();
            Driver.BuildParseOptions();
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            AstContext = Driver.Context.ASTContext;
        }
    }
}
