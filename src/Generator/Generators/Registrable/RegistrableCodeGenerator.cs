using CppSharp.AST;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public abstract class RegistrableCodeGenerator<TGenerator> : CodeGenerator
        where TGenerator : Generator
    {
        public TGenerator Generator { get; set; }

        public RegistrableCodeGenerator(TGenerator generator, IEnumerable<TranslationUnit> units) : base(generator.Context, units)
        {
            Generator = generator;
        }
    }
}
