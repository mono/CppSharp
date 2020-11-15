using System;
using System.Collections.Generic;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates QuickJS C/C++ source files.
    /// QuickJS documentation: https://bellard.org/quickjs/
    /// </summary>
    public class QuickJSSources : CppSources
    {
        public QuickJSSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Managed);
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);

            WriteInclude(new CInclude()
            {
                File = "quickjs.h",
                Kind = CInclude.IncludeKind.Angled
            });

            foreach (var unit in TranslationUnits)
            {
                WriteInclude(new CInclude()
                {
                    File = unit.FileName,
                    Kind = CInclude.IncludeKind.Quoted
                });
            }

            NewLine();
            PopBlock();

            VisitNamespace(TranslationUnit);
        }

        public override ParamMarshal GenerateFunctionParamMarshal(Parameter param, int paramIndex,
            Function function = null)
        {
            var paramMarshal = new ParamMarshal { Name = param.Name, Param = param };

            var argName = Generator.GeneratedIdentifier(param.Name);

            Parameter effectiveParam = param;
            var isRef = param.IsOut || param.IsInOut;
            var paramType = param.Type;

            var ctx = new MarshalContext(Context, CurrentIndentation)
            {
                Parameter = effectiveParam,
                ParameterIndex = paramIndex,
                ArgName = argName,
                Function = function
            };

            var marshal = new QuickJSMarshalManagedToNativePrinter(ctx);
            effectiveParam.Visit(marshal);

            if (string.IsNullOrEmpty(marshal.Context.Before))
                throw new Exception($"Cannot marshal argument of function '{function.QualifiedOriginalName}'");

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            NewLine();

            paramMarshal.Name = argName;
            return paramMarshal;
        }

        public override void GenerateFunctionCallReturnMarshal(Function function)
        {
            var ctx = new MarshalContext(Context, CurrentIndentation)
            {
                ArgName = Helpers.ReturnIdentifier,
                ReturnVarName = Helpers.ReturnIdentifier,
                ReturnType = function.ReturnType
            };

            var marshal = new QuickJSMarshalNativeToManagedPrinter(ctx);
            function.ReturnType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            NewLine();
            WriteLine($"return {marshal.Context.Return};");
        }

        public override bool VisitFunctionDecl(Function function)
        {
            Write("extern \"C\" ");
            WriteLine($"JSValue js_{function.Name}(JSContext* ctx, JSValueConst this_val,");
            WriteLineIndent("int argc, JSValueConst* argv)");
            WriteOpenBraceAndIndent();

            GenerateFunctionCall(function);

            UnindentAndWriteCloseBrace();

            return true;
        }
    }
}
