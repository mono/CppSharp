using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;
using CppSharp.Passes;

namespace CppSharp.Generators.TS
{
    /// <summary>
    /// C++ generator responsible for driving the generation of source and
    /// header files.
    /// </summary>
    public class TSGenerator : CGenerator
    {
        public TSGenerator(BindingContext context) : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new TSSources(Context, units);
            outputs.Add(header);

            return outputs;
        }

        public override bool SetupPasses()
        {
            return true;
        }
    }
}
