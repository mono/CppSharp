using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public abstract class RegistrableHeaders<TGenerator> : RegistrableCodeGenerator<TGenerator>
        where TGenerator : Generator
    {
        public RegistrableHeaders(TGenerator generator, IEnumerable<TranslationUnit> units) : base(generator, units)
        {
        }
    }
}
