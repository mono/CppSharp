using System;
using System.Collections.Generic;
using CppSharp.Passes;

namespace CppSharp.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        private readonly CSharpTypePrinter typePrinter;

        public CSharpGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CSharpTypePrinter(driver.TypeDatabase, driver.Library);
            Type.TypePrinterDelegate += type => type.Visit(typePrinter).Type;
        }

        public override List<TextTemplate> Generate(TranslationUnit unit)
        {
            var outputs = new List<TextTemplate>();

            var template = new CSharpTextTemplate(Driver, unit, typePrinter);
            outputs.Add(template);

            return outputs;
        }

        public override bool SetupPasses(PassBuilder builder)
        {
            builder.CheckAbiParameters(Driver.Options);
            builder.CheckOperatorOverloads();

            return true;
        }
    }
}
