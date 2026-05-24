using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public abstract class RegistrableSources<TGenerator> : RegistrableCodeGenerator<TGenerator>
        where TGenerator : Generator
    {
        public RegistrableSources(TGenerator generator, IEnumerable<TranslationUnit> units) : base(generator, units)
        {
        }
    }
}
