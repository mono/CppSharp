using System.Collections.Generic;
using System.IO;
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

        public override bool Generate(TranslationUnit unit,
            List<GeneratorOutput> outputs)
        {
            var template = new CSharpTextTemplate(Driver, unit, typePrinter);
            outputs.Add(GenerateTemplateOutput(template));

            return true;
        }
    }
}
