using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Passes;
using CppSharp.Parser;

namespace CppSharp.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        public CSharpGenerator(BindingContext context) : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var gen = new CSharpSources(Context, units) { TypePrinter = (CSharpTypePrinter)typePrinter };
            outputs.Add(gen);

            return outputs;
        }

        public override bool SetupPasses()
        {
            if (Context.Options.GenerateDefaultValuesForArguments)
            {
                Context.TranslationUnitPasses.AddPass(new FixDefaultParamValuesOfOverridesPass());
                Context.TranslationUnitPasses.AddPass(new HandleDefaultParamValuesPass());
            }

            // Both the CheckOperatorsOverloadsPass and CheckAbiParameters can
            // create and and new parameters to functions and methods. Make sure
            // CheckAbiParameters runs last because hidden structure parameters
            // should always occur first.

            if (Context.ParserOptions.LanguageVersion > LanguageVersion.C99_GNU)
                Context.TranslationUnitPasses.AddPass(new CheckAbiParameters());

            return true;
        }
    }
}
