using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates QuickJS C/C++ header files.
    /// QuickJS documentation: https://bellard.org/quickjs/
    /// </summary>
    public class QuickJSHeaders : CppHeaders
    {
        public QuickJSHeaders(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override bool ShouldGenerateNamespaces => false;

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            WriteLine("#pragma once");
            NewLine();

            var include = new CInclude()
            {
                File = "quickjs.h",
                Kind = CInclude.IncludeKind.Angled
            };

            WriteInclude(include);
            NewLine();
            PopBlock();

            VisitNamespace(TranslationUnit);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            Write("extern \"C\" ");
            WriteLine($"JSValue js_{function.Name}(JSContext* ctx, JSValueConst this_val,");
            WriteLineIndent("int argc, JSValueConst* argv);");

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
