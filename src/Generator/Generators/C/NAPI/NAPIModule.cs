using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates Node N-API C/C++ module init files.
    /// N-API documentation: https://nodejs.org/api/n-api.html
    /// </summary>
    public class NAPIModule : CCodeGenerator
    {
        public NAPIModule(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override string FileExtension { get; } = "cpp";

        public override void Process()
        {
            var include = new CInclude()
            {
                File = "node_api.h",
                Kind = CInclude.IncludeKind.Angled
            };

            WriteInclude(include);

            WriteLine("NAPI_MODULE_INIT()");
            WriteOpenBraceAndIndent();

            UnindentAndWriteCloseBrace();
        }
    }
}
