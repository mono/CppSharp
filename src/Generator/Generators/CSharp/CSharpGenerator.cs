using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Passes;

namespace CppSharp.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        private readonly CSharpTypePrinter typePrinter;
        private readonly CSharpExpressionPrinter expressionPrinter;

        public CSharpGenerator(BindingContext context) : base(context)
        {
            typePrinter = new CSharpTypePrinter(context);
            expressionPrinter = new CSharpExpressionPrinter(typePrinter);
        }

        public override List<Template> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<Template>();

            var template = new CSharpSources(Context, units, typePrinter, expressionPrinter);
            outputs.Add(template);

            return outputs;
        }

        public override bool SetupPasses()
        {
            // Both the CheckOperatorsOverloadsPass and CheckAbiParameters can
            // create and and new parameters to functions and methods. Make sure
            // CheckAbiParameters runs last because hidden structure parameters
            // should always occur first.

            Context.TranslationUnitPasses.AddPass(new CheckAbiParameters());

            return true;
        }

        protected override string TypePrinterDelegate(Type type)
        {
            return type.Visit(typePrinter).Type;
        }
    }
}
