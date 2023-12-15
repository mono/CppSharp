namespace CppSharp.Generators.Registrable.Lua.Sol
{
    public class LuaSolGeneratorOptions : RegistrableGeneratorOptions<LuaSolGenerator>
    {
        public LuaSolNamingStrategy NamingStrategy;

        public LuaSolGeneratorOptions(LuaSolGenerator generator) : base(generator)
        {
            NamingStrategy = new LuaSolNamingStrategy(generator);
        }

        public override string DefaultRootContextType => "::sol::state_view&";

        public override string DefaultRootContextName => "state";

        public override string DefaultTemplateContextDefaultType => "::sol::table";

        public override string DefaultTemplateContextDefaultValue => "::sol::nil";

        public override string DefaultCmakeVariableHeader => "LUA_SOL_BINDINGS_HEADER";

        public override string DefaultCmakeVariableSource => "LUA_SOL_BINDINGS_SOURCE";
    }
}
