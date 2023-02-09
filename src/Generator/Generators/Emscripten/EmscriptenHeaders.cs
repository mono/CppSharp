using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators.Emscripten
{
    /// <summary>
    /// Generates Emscripten Embind C/C++ header files.
    /// Embind documentation: https://emscripten.org/docs/porting/connecting_cpp_and_javascript/embind.html
    /// </summary>
    public sealed class EmscriptenHeaders : EmscriptenCodeGenerator
    {
        public EmscriptenHeaders(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        //public override bool ShouldGenerateNamespaces => false;

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            WriteLine("#pragma once");
            NewLine();
            PopBlock();

            var name = GetTranslationUnitName(TranslationUnit);
            WriteLine($"extern \"C\" void embind_init_{name}();");
        }

        public override bool VisitClassDecl(Class @class)
        {
            return true;
        }

        public override bool VisitEvent(Event @event)
        {
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            return true;
        }
    }
}
