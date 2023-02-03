using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.Emscripten
{
    /// <summary>
    /// Generates Emscripten module init files.
    /// Embind documentation: https://emscripten.org/docs/porting/connecting_cpp_and_javascript/embind.html
    /// </summary>
    public class EmscriptenModule : EmscriptenCodeGenerator
    {
        private readonly Module module;

        public EmscriptenModule(BindingContext context, Module module)
            : base(context, module.Units.GetGenerated())
        {
            this.module = module;
        }

        public override string FileExtension { get; } = "cpp";

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);
            NewLine();

            PushBlock(BlockKind.Includes);
            {
                WriteInclude(new CInclude()
                {
                    File = "emscripten/bind.h",
                    Kind = CInclude.IncludeKind.Angled
                });
                
                foreach (var unit in TranslationUnits)
                    WriteInclude(GetIncludeFileName(Context, unit), CInclude.IncludeKind.Quoted);
            }
            PopBlock(NewLineKind.Always);

            WriteLine("extern \"C\" {");
            NewLine();

            PushBlock(BlockKind.ForwardReferences);
            {
                foreach (var unit in TranslationUnits.Where(unit => unit.IsGenerated))
                {
                    var name = GetTranslationUnitName(unit);
                    WriteLine($"void embind_init_{name}();");
                }
            }
            PopBlock(NewLineKind.Always);

            var moduleName = module.LibraryName;
            WriteLine($"void embind_init_{moduleName}()");
            WriteOpenBraceAndIndent();

            foreach (var unit in TranslationUnits)
            {
                var name = GetTranslationUnitName(unit);
                WriteLine($"embind_init_{name}();");
            }

            UnindentAndWriteCloseBrace();
            
            NewLine();
            WriteLine("}");
            NewLine();

            WriteLine($"static struct EmBindInit_{moduleName} : emscripten::internal::InitFunc {{");
            WriteLineIndent($"EmBindInit_{moduleName}() : InitFunc(embind_init_{moduleName}) {{}}");
            WriteLine($"}} EmBindInit_{moduleName}_instance;");
        }

        public static string GetIncludeFileName(BindingContext context, TranslationUnit unit)
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
