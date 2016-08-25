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

        public CLIGenerator(BindingContext context) : base(context)
        {
            typePrinter = new CLITypePrinter(context);
        }

        public override List<Template> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<Template>();

            var header = new CLIHeaders(Context, units);
            outputs.Add(header);

            var source = new CLISources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override bool SetupPasses()
        {
            // Note: The ToString override will only work if this pass runs
            // after the MoveOperatorToCallPass.
            if (Context.Options.GenerateObjectOverrides)
                Context.TranslationUnitPasses.AddPass(new ObjectOverridesPass(this));
            return true;
        }

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;
            return @class.IsRefType && (!@class.HasBase || !@class.HasRefBase());
        }

        protected override string TypePrinterDelegate(Type type)
        {
            return type.Visit(typePrinter);
        }
    }
}