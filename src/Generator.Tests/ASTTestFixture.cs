using System;
using CppSharp.AST;
using CppSharp.Utils;
using CppSharp.Parser;
using CppSharp.Passes;
using CppSharp.Generators;
using NUnit.Framework;
using CppSharp.AST.Extensions;

namespace CppSharp.Generator.Tests
{
    public class ASTTestFixture
    {
        protected Driver Driver;
        protected ASTContext AstContext;
        protected BindingContext Context;

        public ASTTestFixture(params string[] files)
        {
            this.files = files;
        }

        [OneTimeSetUp]
        public void Init()
        {
            ParseLibrary(files);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            if (files.Length > 0)
            {
                Driver.Dispose();
            }
        }

        protected void ParseLibrary(params string[] files)
        {
            if (files.Length == 0)
            {
                return;
            }

            var options = new DriverOptions { GeneratorKind = GeneratorKind.CSharp };

            var module = options.AddModule("Test");
            module.IncludeDirs.Add(GeneratorTest.GetTestsDirectory("Native"));
            module.Headers.AddRange(files);

            Driver = new Driver(options)
            {
                ParserOptions = new ParserOptions { SkipPrivateDeclarations = true }
            };

            Driver.Setup();
            if (!Driver.ParseCode())
                throw new Exception("Error parsing the code");

            Driver.SetupTypeMaps();
            AstContext = Driver.Context.ASTContext;
            new CleanUnitPass { Context = Driver.Context }.VisitASTContext(AstContext);
            new ResolveIncompleteDeclsPass { Context = Driver.Context }.VisitASTContext(AstContext);

            Context = new BindingContext(options, Driver.ParserOptions);
            Context.TypeMaps = new Types.TypeMapDatabase(Context);

            CppSharp.AST.Type.TypePrinterDelegate = type =>
            {
                PrimitiveType primitiveType;
                return type.IsPrimitiveType(out primitiveType) ? primitiveType.ToString() : string.Empty;
            };
        }

        private readonly string[] files;
    }
}
