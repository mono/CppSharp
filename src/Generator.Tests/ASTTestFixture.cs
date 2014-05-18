﻿using System;
using CppSharp.AST;
using CppSharp.Utils;

namespace CppSharp.Generator.Tests
{
    public class ASTTestFixture
    {
        protected Driver Driver;
        protected DriverOptions Options;
        protected ASTContext AstContext;

        protected void ParseLibrary(params string[] files)
        {
            Options = new DriverOptions();

            var testsPath = GeneratorTest.GetTestsDirectory("Native");

#if OLD_PARSER
            Options.IncludeDirs.Add(testsPath);
#else
            Options.addIncludeDirs(testsPath);
#endif

            Options.Headers.AddRange(files);

            Driver = new Driver(Options, new TextDiagnosticPrinter());
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            AstContext = Driver.ASTContext;
        }
    }
}
