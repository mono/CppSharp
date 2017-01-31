using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators
{
    public abstract class Template : BlockGenerator
    {
        public BindingContext Context { get; private set; }

        public IDiagnostics Log { get { return Context.Diagnostics; } }
        public DriverOptions Options { get { return Context.Options; } }

        public List<TranslationUnit> TranslationUnits { get; private set; }

        public TranslationUnit TranslationUnit { get { return TranslationUnits[0]; } }

        public abstract string FileExtension { get; }

        protected Template(BindingContext context, IEnumerable<TranslationUnit> units)
        {
            Context = context;
            TranslationUnits = new List<TranslationUnit>(units);
        }

        public abstract void Process();

        public new string Generate()
        {
            if (Options.IsCSharpGenerator && Options.CompileCode)
                return base.GenerateUnformatted();

            return base.Generate();
        }
    }
}
