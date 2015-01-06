using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// C++/CLI generator responsible for driving the generation of
    /// source and header files.
    /// </summary>
    public class CLIGenerator : Generator
    {
        private readonly CLITypePrinter typePrinter;

        public CLIGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CLITypePrinter(driver);
            Type.TypePrinterDelegate += type => type.Visit(typePrinter);
        }

        public override List<Template> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<Template>();

            var header = new CLIHeadersTemplate(Driver, units);
            outputs.Add(header);

            var source = new CLISourcesTemplate(Driver, units);
            outputs.Add(source);

            return outputs;
        }

        public override bool SetupPasses()
        {
            // Note: The ToString override will only work if this pass runs
            // after the MoveOperatorToCallPass.
            if (Driver.Options.GenerateObjectOverrides)
                Driver.TranslationUnitPasses.AddPass(new ObjectOverridesPass());
            return true;
        }

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;
            return @class.IsRefType && (!@class.HasBase || !@class.HasRefBase());
        }
    }
}