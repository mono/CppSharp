using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using static CppSharp.Generators.Cpp.NAPISources;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates QuickJS C/C++ source files.
    /// QuickJS documentation: https://bellard.org/quickjs/
    /// </summary>
    public class QuickJSSources : NAPISources
    {
        public QuickJSSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            {
                WriteInclude("quickjs.h", CInclude.IncludeKind.Angled);
                WriteInclude("assert.h", CInclude.IncludeKind.Angled);

                foreach (var unit in TranslationUnits)
                {
                    WriteInclude(unit.IncludePath, CInclude.IncludeKind.Angled);
                }

                NewLine();
            }
            PopBlock();

            WriteLine("extern \"C\" {");
            NewLine();

            var registerGen = new QuickJSRegister(Context, TranslationUnits);
            registerGen.Process();
            WriteLine(registerGen.Generate());

            PushBlock();
            {
                var name = GetTranslationUnitName(TranslationUnit);
                WriteLine($"void register_{name}(JSContext *ctx, JSModuleDef *m, bool set)");
                WriteOpenBraceAndIndent();

                TranslationUnit.Visit(this);

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            WriteLine("}");
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

            PushBlock();
            WriteLine($"register_class_{GetCIdentifier(Context, @class)}(ctx, m, set);");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.IsOperator)
                return true;

            PushBlock();
            WriteLine($"register_function_{GetCIdentifier(Context, function)}(ctx, m, set);");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return false;

            PushBlock();
            WriteLine($"register_enum_{GetCIdentifier(Context, @enum)}(ctx, m, set);");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }
    }

    public class QuickJSRegister : NAPIRegister
    {
        public QuickJSRegister(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

/*
            PushBlock(BlockKind.InternalsClass, @class);
            {
                var callbacks = new QuickJSInvokes(Context);
                @class.Visit(callbacks);
                Write(callbacks.Generate());
            }
            PopBlock(NewLineKind.BeforeNextBlock);
*/

            PushBlock(BlockKind.Class, @class);
            {
                Write($"static void register_class_{GetCIdentifier(Context, @class)}");
                WriteLine("(JSContext *ctx, JSModuleDef *m, bool set)");
                WriteOpenBraceAndIndent();

/*
                var sources = new NAPIRegisterImpl(Context);
                sources.Indent(CurrentIndentation);
                @class.Visit(sources);
                Write(sources.Generate());
*/

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            return false;
        }

        public override void GenerateFunctionGroup(List<Function> group)
        {
            var function = group.First();
            if (function.IsOperator)
                return;

            PushBlock(BlockKind.Function);
            {
                var callbacks = new QuickJSInvokes(Context);
                callbacks.GenerateFunctionGroup(group);
                Write(callbacks.Generate());
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(BlockKind.Function);
            {
                Write($"static void register_function_{GetCIdentifier(Context, function)}");
                WriteLine("(JSContext *ctx, JSModuleDef *m, bool set)");
                WriteOpenBraceAndIndent();

                WriteLine("if (!set)");
                WriteOpenBraceAndIndent();
                WriteLine($"int status = JS_AddModuleExport(ctx, m, \"{function.Name}\");");
                WriteLine("assert(status != -1);");
                WriteLine("return;");
                UnindentAndWriteCloseBrace();
                NewLine();

                var callbackId = $"callback_function_{GetCIdentifier(Context, function)}";
                var maxParams = @group.Max(f => f.Parameters.Count);

                WriteLine($"JSValue val = JS_NewCFunction(ctx, {callbackId}, \"{function.Name}\"," +
                          $" {maxParams});");
                WriteLine($"int status = JS_SetModuleExport(ctx, m, \"{function.Name}\", val);");
                WriteLine("assert(status != -1);");

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return false;

            PushBlock(BlockKind.Enum);
            {
                Write($"static void register_enum_{GetCIdentifier(Context, @enum)}");
                WriteLine("(JSContext *ctx, JSModuleDef *m)");
                WriteOpenBraceAndIndent();

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }

    public class QuickJSInvokes : NAPIInvokes
    {
        public QuickJSInvokes(BindingContext context)
            : base(context, null)
        {
        }

        public QuickJSInvokes(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
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

        public override void GenerateFunctionGroup(List<Function> @group)
        {
            GenerateFunctionCallback(@group);
        }

        public override void GenerateMethodGroup(List<Method> @group)
        {
            GenerateFunctionCallback(@group.OfType<Function>().ToList());
        }
        public override void GenerateFunctionCallback(List<Function> @group)
        {
            var function = @group.First();

            var type = function is Method ? "method" : "function";
            var callbackId = $"callback_{type}_{GetCIdentifier(Context, function)}";

            PushBlock();
            Write("extern \"C\" ");
            WriteLine($"JSValue {callbackId}(JSContext* ctx, JSValueConst this_val,");
            WriteLineIndent("int argc, JSValueConst* argv)");
            WriteOpenBraceAndIndent();

            GenerateFunctionCall(function);

            var needsReturn = !function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void);
            if (!needsReturn)
            {
                WriteLine("return JS_UNDEFINED;");
            }

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }
    }
}
