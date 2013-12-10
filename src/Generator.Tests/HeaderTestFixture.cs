using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Utils;

namespace CppSharp.Generator.Tests
{
    public class HeaderTestFixture
    {
        protected Driver Driver;
        protected DriverOptions Options;
        protected ASTContext AstContext;

        protected void ParseLibrary(string file)
        {
            Options = new DriverOptions();

            var testsPath = LibraryTest.GetTestsDirectory("Native");
            Options.IncludeDirs.Add(testsPath);
            Options.Headers.Add(file);

            Driver = new Driver(Options, new TextDiagnosticPrinter());
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            AstContext = Driver.ASTContext;
        }
    }
}
