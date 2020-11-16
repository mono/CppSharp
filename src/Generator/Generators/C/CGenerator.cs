using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.Cpp;

namespace CppSharp.Generators.C
{
    /// <summary>
    /// C generator responsible for driving the generation of source and
    /// header files.
    /// </summary>
    public class CGenerator : Generator
    {
        private readonly CppTypePrinter typePrinter;

        public CGenerator(BindingContext context) : base(context)
        {
            typePrinter = new CppTypePrinter(Context);
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new CppHeaders(Context, units);
            outputs.Add(header);

            var source = new CppSources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override bool SetupPasses() => true;

        protected override string TypePrinterDelegate(Type type)
        {
            return type.Visit(typePrinter).ToString();
        }
    }
}
