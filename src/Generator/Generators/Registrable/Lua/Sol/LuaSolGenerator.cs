using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable.Lua.Sol
{

    public class LuaSolGenerator : Generator
    {
        public const string Id = "Lua::Sol";
        public static readonly GeneratorKind Kind = new(Id, "lua::sol", typeof(LuaSolGenerator), typeof(LuaSolTypePrinter), new[] { "lua::sol" });

        public LuaSolGeneratorOptions GeneratorOptions
        {
            get;
        }

        public LuaSolGenerator(BindingContext context) : base(context)
        {
            GeneratorOptions = new LuaSolGeneratorOptions(this);
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new LuaSolHeaders(this, units);
            outputs.Add(header);

            var source = new LuaSolSources(this, units);
            outputs.Add(source);

            return outputs;
        }

        public override bool SetupPasses() => true;
    }
}
