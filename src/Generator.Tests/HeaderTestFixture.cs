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

#if OLD_PARSER
            Options.IncludeDirs.Add(testsPath);
#else
            Options.addIncludeDirs(testsPath);
#endif

            Options.Headers.Add(file);

            Driver = new Driver(Options, new TextDiagnosticPrinter());
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            AstContext = Driver.ASTContext;
        }
    }
}
