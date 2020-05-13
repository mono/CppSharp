using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.Cpp;

namespace CppSharp.Generators.C
{
    /// <summary>
    /// N-API generator responsible for driving the generation of binding files.
    /// N-API documentation: https://nodejs.org/api/n-api.html
    /// </summary>
    public class NAPIGenerator : CppGenerator
    {
        public NAPIGenerator(BindingContext context) : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new NAPIHeaders(Context, units);
            outputs.Add(header);

            var source = new CppSources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override GeneratorOutput GenerateModule(Module module)
        {
            return base.GenerateModule(module);
        }
    }
}
