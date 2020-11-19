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
        public NAPIModule(BindingContext context, Module module)
            : base(context, module.Units.GetGenerated())
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override string FileExtension { get; } = "cpp";

        public override void Process()
        {
            var include = new CInclude()
            {
                File = "node/node_api.h",
                Kind = CInclude.IncludeKind.Angled
            };

            WriteInclude(include);
            NewLine();

            WriteLine("NAPI_MODULE_INIT()");
            WriteOpenBraceAndIndent();

            WriteLine("napi_value result;");
            WriteLine("NAPI_CALL(env, napi_create_object(env, &result));");

            WriteLine("return result;");

            UnindentAndWriteCloseBrace();
        }
    }
}