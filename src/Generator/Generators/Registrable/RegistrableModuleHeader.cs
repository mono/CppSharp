using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public class RegistrableModuleHeader<TGenerator> : RegistrableCodeGenerator<TGenerator>
        where TGenerator : Generator
    {
        public RegistrableModuleHeader(TGenerator generator, IEnumerable<TranslationUnit> units) : base(generator, units)
        {
        }

        public override string FileExtension { get; } = "h";

        public override void Process()
        {
        }
    }
}
