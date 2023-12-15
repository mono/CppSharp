using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public abstract class RegistrableGenerator<TOptions, THeader, TSource> : Generator
        where THeader : CodeGenerator
        where TSource : CodeGenerator
    {
        public TOptions GeneratorOptions { get; }

        public RegistrableGenerator(BindingContext context) : base(context)
        {
            GeneratorOptions = CreateOptions(this);
        }

        protected abstract TOptions CreateOptions(RegistrableGenerator<TOptions, THeader, TSource> generator);

        protected abstract THeader CreateHeader(RegistrableGenerator<TOptions, THeader, TSource> generator, IEnumerable<TranslationUnit> units);

        protected abstract TSource CreateSource(RegistrableGenerator<TOptions, THeader, TSource> generator, IEnumerable<TranslationUnit> units);

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            return new List<CodeGenerator>
            {
                CreateHeader(this, units),
                CreateSource(this, units)
            };
        }

        public override bool SetupPasses() => true;
    }
}
