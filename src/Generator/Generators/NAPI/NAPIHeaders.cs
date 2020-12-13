using System.Collections.Generic;
using System.IO;
using CppSharp.AST;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates Node N-API C/C++ header files.
    /// N-API documentation: https://nodejs.org/api/n-api.html
    /// </summary>
    public class NAPIHeaders : CppHeaders
    {
        public NAPIHeaders(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override void Process()
        {
            return;
        }
    }
}
