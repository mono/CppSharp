using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// C/C++ generator responsible for driving the generation of source
    /// and header files.
    /// </summary>
    public class CppGenerator : Generator
    {
        private readonly CppTypePrinter typePrinter;

        public CppGenerator(BindingContext context) : base(context)
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

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;

            return @class.IsRefType && (!@class.HasBase || !@class.HasRefBase());
        }

        protected override string TypePrinterDelegate(Type type)
        {
            return type.Visit(typePrinter).ToString();
        }
    }
}
