using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable.Lua.Sol
{
    public class LuaSolGenerator : RegistrableGenerator<LuaSolGeneratorOptions, LuaSolHeaders, LuaSolSources>
    {
        public const string Id = "Lua::Sol";
        public static readonly GeneratorKind Kind = new(Id, "lua::sol", typeof(LuaSolGenerator), typeof(LuaSolTypePrinter), new[] { "lua::sol" });

        public LuaSolGenerator(BindingContext context) : base(context)
        {
        }

        protected override LuaSolGeneratorOptions CreateOptions(RegistrableGenerator<LuaSolGeneratorOptions, LuaSolHeaders, LuaSolSources> generator)
        {
            return new LuaSolGeneratorOptions(this);
        }

        protected override LuaSolHeaders CreateHeader(RegistrableGenerator<LuaSolGeneratorOptions, LuaSolHeaders, LuaSolSources> generator, IEnumerable<TranslationUnit> units)
        {
            return new LuaSolHeaders(this, units);
        }

        protected override LuaSolSources CreateSource(RegistrableGenerator<LuaSolGeneratorOptions, LuaSolHeaders, LuaSolSources> generator, IEnumerable<TranslationUnit> units)
        {
            return new LuaSolSources(this, units);
        }

        public override bool SetupPasses() => true;
    }
}
