using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Passes;

namespace CppSharp.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        private readonly CSharpTypePrinter typePrinter;
        private readonly CSharpExpressionPrinter expressionPrinter;

        public CSharpGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CSharpTypePrinter(driver);
            expressionPrinter = new CSharpExpressionPrinter();
            CppSharp.AST.Type.TypePrinterDelegate += type => type.Visit(typePrinter).Type;
        }

        public override List<Template> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<Template>();

            var template = new CSharpTextTemplate(Driver, units, typePrinter, expressionPrinter);
            outputs.Add(template);

            return outputs;
        }

        public override bool SetupPasses()
        {
            // Both the CheckOperatorsOverloadsPass and CheckAbiParameters can
            // create and and new parameters to functions and methods. Make sure
            // CheckAbiParameters runs last because hidden structure parameters
            // should always occur first.

            Driver.AddTranslationUnitPass(new CheckAbiParameters());

            return true;
        }
    }
}
