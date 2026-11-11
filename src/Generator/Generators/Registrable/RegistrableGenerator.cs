using CppSharp.AST;
using System.Collections.Generic;
using System.IO;

namespace CppSharp.Generators.Registrable
{
    public abstract class RegistrableGenerator<TOptions, THeader, TSource> : Generator
        where TOptions : RegistrableGeneratorOptions
        where THeader : CodeGenerator
        where TSource : CodeGenerator
    {
        public TOptions GeneratorOptions { get; }

        public TranslationUnit GlobalTranslationUnit { get; private set; }

        public TranslationUnit InheritanceTranslationUnit { get; private set; }

        // TODO: Implement when Generator interface is cleaner
        // public CodeGenerator ModuleHeaderCodeGenerator { get; private set; }

        // TODO: Implement when Generator interface is cleaner
        //public CodeGenerator ModuleSourceCodeGenerator { get; private set; }

        public RegistrableGenerator(BindingContext context) : base(context)
        {
            GeneratorOptions = CreateOptions();
        }

        protected abstract TOptions CreateOptions();

        protected abstract THeader CreateHeader(IEnumerable<TranslationUnit> units);

        protected abstract TSource CreateSource(IEnumerable<TranslationUnit> units);

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            return new List<CodeGenerator>
            {
                CreateHeader(units),
                CreateSource(units)
            };
        }

        // TODO: Should be a better method for this maybe Configure.
        public override bool SetupPasses()
        {
            {
                var module = Context.Options.Modules[1];
                GlobalTranslationUnit = Context.ASTContext.FindOrCreateTranslationUnit(
                    Path.Combine(Context.Options.OutputDir, GeneratorOptions.OutputSubDir, "@package", "global.h")
                );
                GlobalTranslationUnit.Module = module;
            }
            {
                var module = Context.Options.Modules[1];
                InheritanceTranslationUnit = Context.ASTContext.FindOrCreateTranslationUnit(
                    Path.Combine(Context.Options.OutputDir, GeneratorOptions.OutputSubDir, "@package", "inheritance.h")
                );
                InheritanceTranslationUnit.Module = module;
            }
            return true;
        }
    }
}
