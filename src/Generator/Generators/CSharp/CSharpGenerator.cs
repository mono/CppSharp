using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Passes;
using CppSharp.Generators;

namespace CppSharp.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        private readonly CSharpTypePrinter typePrinter;

        public CSharpGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CSharpTypePrinter(driver.TypeDatabase, driver.Library);
            CppSharp.AST.Type.TypePrinterDelegate += type => type.Visit(typePrinter).Type;
        }

        public override List<Template> Generate(TranslationUnit unit)
        {
            var outputs = new List<Template>();

            var template = new CSharpTextTemplate(Driver, unit, typePrinter);
            outputs.Add(template);

            return outputs;
        }

        public override bool SetupPasses()
        {
            Driver.AddTranslationUnitPass(new CheckAbiParameters(Driver.Options));
            Driver.AddTranslationUnitPass(new CheckOperatorsOverloadsPass());

            return true;
        }
    }
}
