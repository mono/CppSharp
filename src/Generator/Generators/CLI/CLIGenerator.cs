using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// C++/CLI generator responsible for driving the generation of
    /// source and header files.
    /// </summary>
    public class CLIGenerator : Generator
    {
        public CLIGenerator(BindingContext context) : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new CLIHeaders(Context, units);
            outputs.Add(header);

            var source = new CLISources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override bool SetupPasses() => true;

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;

            return @class.IsRefType && (!@class.NeedsBase || !@class.HasRefBase());
        }
    }
}