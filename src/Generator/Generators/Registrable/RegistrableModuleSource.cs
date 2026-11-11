using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public class RegistrableModuleSource<TGenerator> : RegistrableCodeGenerator<TGenerator>
        where TGenerator : Generator
    {
        public RegistrableModuleSource(TGenerator generator, IEnumerable<TranslationUnit> units) : base(generator, units)
        {
        }

        public override string FileExtension { get; } = "cpp";

        public override void Process()
        {
        }
    }
}
