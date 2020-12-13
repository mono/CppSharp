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
            WriteInclude("node/node_api.h", CInclude.IncludeKind.Angled);
            WriteInclude("NAPIHelpers.h", CInclude.IncludeKind.Quoted);
            NewLine();

            PushBlock();
            foreach (var unit in TranslationUnits)
            {
                var name = NAPISources.GetTranslationUnitName(unit);
                WriteLine($"extern void register_{name}(napi_env env, napi_value exports);");
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("// napi_value NAPI_MODULE_INITIALIZER(napi_env env, napi_value exports)");
            WriteLine("NAPI_MODULE_INIT()");
            WriteOpenBraceAndIndent();

            foreach (var unit in TranslationUnits)
            {
                var name = NAPISources.GetTranslationUnitName(unit);
                WriteLine($"register_{name}(env, exports);");
            }
            NewLine();

            WriteLine("return nullptr;");

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }
    }
}