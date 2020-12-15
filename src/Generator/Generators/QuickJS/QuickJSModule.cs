using System.IO;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates QuickJS C/C++ module init files.
    /// QuickJS documentation: https://bellard.org/quickjs/
    /// </summary>
    public class QuickJSModule : NAPICodeGenerator
    {
        private readonly Module module;

        public QuickJSModule(BindingContext context, Module module)
            : base(context, module.Units.GetGenerated())
        {
            this.module = module;
        }

        public override string FileExtension { get; } = "cpp";

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            {
                WriteInclude(new CInclude()
                {
                    File = "quickjs.h",
                    Kind = CInclude.IncludeKind.Angled
                });

                foreach (var unit in TranslationUnits)
                {
                    WriteInclude(new CInclude()
                    {
                        File = GetIncludeFileName(Context, unit),
                        Kind = CInclude.IncludeKind.Quoted
                    });
                }

                NewLine();
            }
            PopBlock();

            WriteLine("extern \"C\" {");
            NewLine();

            PushBlock();
            {
                foreach (var unit in TranslationUnits)
                {
                    var name = NAPISources.GetTranslationUnitName(unit);
                    WriteLine($"extern void register_{name}(JSContext *ctx, JSModuleDef *m, bool set);");
                }
            }
            PopBlock(NewLineKind.BeforeNextBlock);
            NewLine();

            WriteLine("#define countof(x) (sizeof(x) / sizeof((x)[0]))");
            NewLine();

            var moduleName = module.LibraryName;

            // Generate init function.
            WriteLine($"static int js_{moduleName}_init(JSContext* ctx, JSModuleDef* m)");
            WriteOpenBraceAndIndent();

            foreach (var unit in TranslationUnits)
            {
                var name = NAPISources.GetTranslationUnitName(unit);
                WriteLine($"register_{name}(ctx, m, /*set=*/true);");
            }
            NewLine();

            WriteLine("return 0;");
            UnindentAndWriteCloseBrace();
            NewLine();

            // Generate module initializer.
            WriteLine("#ifdef JS_SHARED_LIBRARY");
            WriteLine("#define JS_INIT_MODULE js_init_module");
            WriteLine("#else");
            WriteLine($"#define JS_INIT_MODULE js_init_module_{moduleName}");
            WriteLine("#endif");
            NewLine();

            Write("extern \"C\" ");
            WriteLine("JSModuleDef *JS_INIT_MODULE(JSContext *ctx, const char *module_name)");
            WriteOpenBraceAndIndent();

            WriteLine("JSModuleDef* m;");
            WriteLine($"m = JS_NewCModule(ctx, module_name, js_{moduleName}_init);");
            WriteLine("if (!m)");
            WriteLineIndent("return nullptr;");
            NewLine();

            foreach (var unit in TranslationUnits)
            {
                var name = NAPISources.GetTranslationUnitName(unit);
                WriteLine($"register_{name}(ctx, m, /*set=*/false);");
            }
            NewLine();

            WriteLine("return m;");

            UnindentAndWriteCloseBrace();

            NewLine();
            WriteLine("}");
        }

        public static string GetIncludeFileName(BindingContext context,
            TranslationUnit unit)
        {
            // TODO: Replace with GetIncludePath
            string file;
            if (context.Options.GenerateName != null)
                file = context.Options.GenerateName(unit);
            else
                file = Path.GetFileNameWithoutExtension(unit.FileName)
                .Replace('\\', '/');

            return $"{file}.h";
        }
    }
}
