namespace CppSharp.Generators.Registrable.Lua.Sol
{
    public class LuaSolGeneratorOptions : RegistrableGeneratorOptions
    {
        public LuaSolNamingStrategy NamingStrategy;

        public LuaSolGeneratorOptions(LuaSolGenerator generator) : base()
        {
            NamingStrategy = new LuaSolNamingStrategy(generator);
        }
    }
}
