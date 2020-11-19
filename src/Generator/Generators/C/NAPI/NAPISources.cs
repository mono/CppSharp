using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates Node N-API C/C++ source files.
    /// N-API documentation: https://nodejs.org/api/n-api.html
    /// </summary>
    public class NAPISources : CppSources
    {
        public NAPISources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override void Process()
        {
            base.Process();
        }
    }
}