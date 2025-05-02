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

        protected override LuaSolGeneratorOptions CreateOptions()
        {
            return new LuaSolGeneratorOptions(this);
        }

        protected override LuaSolHeaders CreateHeader(IEnumerable<TranslationUnit> units)
        {
            return new LuaSolHeaders(this, units);
        }

        protected override LuaSolSources CreateSource(IEnumerable<TranslationUnit> units)
        {
            return new LuaSolSources(this, units);
        }
    }
}
