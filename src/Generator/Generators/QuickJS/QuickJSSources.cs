using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.AST;
using CppSharp.Generators.C;
using static CppSharp.Generators.Cpp.NAPISources;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.Cpp
{
    public class QuickJSCodeGenerator : NAPICodeGenerator
    {
        public QuickJSCodeGenerator(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalManagedToNativePrinter(MarshalContext ctx)
        {
            return new QuickJSMarshalManagedToNativePrinter(ctx);
        }

        public override MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalNativeToManagedPrinter(MarshalContext ctx)
        {
            return new QuickJSMarshalNativeToManagedPrinter(ctx);
        }
    }

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
                WriteInclude("CppSharp_QuickJS.h", CInclude.IncludeKind.Angled);
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

            GenerateExternClassIds();

            var registerGen = new QuickJSRegister(Context, TranslationUnits);
            registerGen.Process();
            WriteLine(registerGen.Generate());

            PushBlock();
            {
                var name = GetTranslationUnitName(TranslationUnit);
                WriteLine($"void register_{name}(JSContext *ctx, JSModuleDef *m, bool set, int phase)");
                WriteOpenBraceAndIndent();

                Block registerBlock;
                PushBlock();
                {
                    WriteLine("if (phase == 0)");

                    WriteOpenBraceAndIndent();
                    {
                        PushBlock();
                        VisitOptions.ClearFlags(VisitFlags.NamespaceClasses);
                        TranslationUnit.Visit(this);
                        registerBlock = PopBlock();

                    }
                    UnindentAndWriteCloseBrace();
                }
                var phase0Block = PopBlock(NewLineKind.IfNotEmpty);
                phase0Block.CheckGenerate = () => !registerBlock.IsEmpty;

                PushBlock();
                {
                    VisitOptions.ResetFlags(VisitFlags.Default);
                    VisitOptions.SetFlags(VisitFlags.NamespaceClasses);
                    TranslationUnit.Visit(this);
                }
                PopBlock(NewLineKind.IfNotEmpty);

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);
            NewLine();

            WriteLine("} // extern \"C\"");
        }

        public static string SignalClassId = "classId__Signal";

        private void GenerateExternClassIds()
        {
            PushBlock();
            {
                var classCollector = new RecordCollector(TranslationUnit);
                TranslationUnit.Visit(classCollector);

                var externClasses = new HashSet<Class>();
                foreach (var record in classCollector.Declarations)
                {
                    var @event = record.Value as Event;
                    Class @class;
                    if (@event != null)
                    {
                        foreach (var param in @event.Parameters)
                        {
                            if (!param.Type.GetFinalPointee().TryGetClass(out @class))
                                continue;

                            externClasses.Add(@class);
                        }
                        continue;
                    }

                    @class = record.Value as Class;
                    if (@class == null || !@class.IsGenerated)
                        continue;

                    externClasses.Add(@class);
                }

                foreach (var @class in externClasses)
                {
                    WriteLine($"extern JSClassID classId_{GetCIdentifier(Context, @class)};");
                }
            }

            // If any of the classes have events, then also generate an external for Signal.
            // TODO: Only generate if necessary and fix possible id conflict.
            WriteLine($"extern JSClassID {SignalClassId};");


            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

            PushBlock();
            WriteLine($"register_class_{GetCIdentifier(Context, @class)}(ctx, m, set, phase);");
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

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }
    }

    public class QuickJSClassFuncDef : NAPICodeGenerator
    {
        public QuickJSClassFuncDef(BindingContext context) : base(context, null)
        {
        }

        public override void VisitClassConstructors(IEnumerable<Method> ctors)
        {
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }

        public override void GenerateMethodGroup(List<Method> @group)
        {
            GenerateFunctionGroup(@group.OfType<Function>().ToList());
        }

        public override void GenerateFunctionGroup(List<Function> @group)
        {
            var function = @group.FirstOrDefault();
            var maxArgs = @group.Max(f => f.Parameters.Count);
            var type = function is Method ? "method" : "function";
            var callbackId = $"callback_{type}_{GetCIdentifier(Context, function)}";
            WriteLine($"JS_CFUNC_DEF(\"{function.Name}\", {maxArgs}, {callbackId}),");
        }

        public override bool VisitEvent(Event @event)
        {
            var getterId = $"callback_event_getter_{GetCIdentifier(Context, @event)}";
            WriteLine($"JS_CGETSET_DEF(\"{@event.Name}\", {getterId}, NULL),");
            return true;
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return true;
        }

        public void GenerateToString(Class @class)
        {
            var callbackId = $"callback_class_{GetCIdentifier(Context, @class)}_toString";
            WriteLine($"JS_CFUNC_DEF(\"toString\", 0, {callbackId}),");
        }
    }

    public class QuickJSRegister : NAPIRegister
    {
        public QuickJSRegister(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalManagedToNativePrinter(MarshalContext ctx)
        {
            return new QuickJSMarshalManagedToNativePrinter(ctx);
        }

        public override MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalNativeToManagedPrinter(MarshalContext ctx)
        {
            return new QuickJSMarshalNativeToManagedPrinter(ctx);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

            if (!VisitDeclContext(@class))
                return true;

            PushBlock(BlockKind.InternalsClass, @class);
            {
                WriteLine($"JSClassID classId_{GetCIdentifier(Context, @class)};");
                NewLine();

                GenerateClassExtraData(@class);

                PushBlock();
                {
                    var callbacks = new QuickJSInvokes(Context);
                    @class.Visit(callbacks);
                    callbacks.GenerateToString(@class);
                    Write(callbacks.Generate());
                }
                PopBlock(NewLineKind.BeforeNextBlock);

                var finalizerId = $"finalizer_{GetCIdentifier(Context, @class)}";
                PushBlock();
                {
                    WriteLine($"void {finalizerId}(JSRuntime *rt, JSValue val)");
                    WriteOpenBraceAndIndent();

                    //WriteLine($"printf(\"Calling finalizer for {@class.QualifiedOriginalName}\\n\");");

                    if (ClassNeedsExtraData(@class))
                    {
                        // Remove the event connection from the delegate.
                        // var invokeId = $"event_invoke_{@event.OriginalName}";
                        // WriteLine($"data->instance->{@event.OriginalName}.bind(data, &{classDataId}::{invokeId});");
                        // NewLine();

                        var instanceKind = "JS_INTEROP_INSTANCE_SIGNAL_CONTEXT";
                        WriteLine($"JS_Interop_CleanupObject(val, {instanceKind});");
                    }
                    else
                    {
                        Write($"{@class.QualifiedOriginalName}* instance = ");
                        WriteLine($"({@class.QualifiedOriginalName}*) JS_GetOpaque(val, 0);");
                    }

                    UnindentAndWriteCloseBrace();
                }
                PopBlock(NewLineKind.BeforeNextBlock);

                PushBlock();
                {
                    WriteLine($"static JSClassDef classDef_{GetCIdentifier(Context, @class)}");
                    WriteOpenBraceAndIndent();

                    WriteLine($"\"{@class.Name}\",");
                    WriteLine($".finalizer = {finalizerId}");

                    Unindent();
                    WriteLine("};");
                }
                PopBlock(NewLineKind.BeforeNextBlock);

                PushBlock();
                {
                    WriteLine($"static JSCFunctionListEntry funcDef_{GetCIdentifier(Context, @class)}[]");
                    WriteOpenBraceAndIndent();

                    var funcGen = new QuickJSClassFuncDef(Context);
                    funcGen.Indent(CurrentIndentation);
                    funcGen.VisitClassDeclContext(@class);
                    funcGen.GenerateToString(@class);

                    Write(funcGen.Generate());

                    Unindent();
                    WriteLine("};");
                }
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(BlockKind.Class, @class);
            {
                Write($"static void register_class_{GetCIdentifier(Context, @class)}");
                WriteLine("(JSContext *ctx, JSModuleDef *m, bool set, int phase)");
                WriteOpenBraceAndIndent();

                var sources = new QuickJSRegisterImpl(Context);
                sources.Indent(CurrentIndentation);
                @class.Visit(sources);
                Write(sources.Generate());

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            return false;
        }

        public void GenerateClassExtraData(Class @class)
        {
            if (!ClassNeedsExtraData(@class))
                return;

            PushBlock();
            {
                var classDataId = $"data_{GetCIdentifier(Context, @class)}";
                WriteLine($"struct {classDataId} : public JS_Interop_ClassData");
                WriteOpenBraceAndIndent();

                //WriteLine($"{@class.QualifiedOriginalName}* instance;");

                foreach (var @event in @class.Events)
                {
                    GenerateEventInvoke(@event);
                }

                Unindent();
                WriteLine("};");
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateEventInvoke(Event @event)
        {
            var invokeId = $"event_invoke_{@event.OriginalName}";
            PushBlock();
            {
                CTypePrinter.PushContext(TypePrinterContextKind.Native);
                var functionType = @event.Type as FunctionType;
                var retType = functionType.ReturnType.Visit(CTypePrinter);
                CTypePrinter.PopContext();

                Write($"{retType} {invokeId}(");

                var @params = new List<string>();
                foreach (var param in @event.Parameters)
                {
                    CTypePrinter.PushContext(TypePrinterContextKind.Native);
                    var paramType = param.Type.Visit(CTypePrinter);
                    CTypePrinter.PopContext();

                    @params.Add($"{paramType} {param.Name}");
                }

                Write(string.Join(", ", @params));
                WriteLine(")");
                WriteOpenBraceAndIndent();

                WriteLine($"JSValue event = JS_Interop_FindEvent(&events, {@event.GlobalId});");
                WriteLine($"if (JS_IsUndefined(event))");

                var isVoidReturn = functionType.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void);
                if (isVoidReturn)
                {
                    WriteLineIndent($"return;");
                }
                else
                {
                    var defaultValuePrinter = new CppDefaultValuePrinter(Context);
                    var defaultValue = functionType.ReturnType.Visit(defaultValuePrinter);
                    WriteLineIndent($"return {defaultValue};");
                }
                NewLine();

                // Marshal the arguments.
                var marshalers = new List<MarshalPrinter<MarshalContext, CppTypePrinter>>();
                foreach (var param in @event.Parameters)
                {
                    var ctx = new MarshalContext(Context, CurrentIndentation)
                    {
                        ArgName = param.Name,
                        ReturnVarName = param.Name,
                        ReturnType = param.QualifiedType,
                        Parameter = param
                    };

                    var marshal = GetMarshalNativeToManagedPrinter(ctx);
                    marshalers.Add(marshal);

                    param.Visit(marshal);

                    if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    {
                        Write(marshal.Context.Before);
                    }
                }

                var args = marshalers.Select(m => m.Context.Return.ToString());
                WriteLine($"JSValueConst argv[] = {{ { string.Join(", ", args)} }};");
                WriteLine($"auto data = (JS_SignalContext*) JS_GetOpaque(event, 0);");
                WriteLine($"JSValue ret = JS_Call(ctx, data->function, JS_UNDEFINED, {@event.Parameters.Count}, argv);");
                WriteLine($"JS_FreeValue(ctx, ret);");

                //WriteLine($"{@class.QualifiedOriginalName}* instance = data->instance;");

                /*

                                if (!isVoidReturn)
                                {
                                    CTypePrinter.PushContext(TypePrinterContextKind.Native);
                                    var returnType = function.ReturnType.Visit(CTypePrinter);
                                    CTypePrinter.PopContext();

                                    Write($"{returnType} {Helpers.ReturnIdentifier} = ");
                                }

                                var @class = function.Namespace as Class;
                */

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public static bool ClassNeedsExtraData(Class @class)
        {
            return @class.Events.Any() ||
                   @class.Bases.Any(b => b.IsClass && ClassNeedsExtraData(b.Class));
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
                WriteLine("(JSContext *ctx, JSModuleDef *m, bool set)");
                WriteOpenBraceAndIndent();

                WriteLine("if (!set)");
                WriteOpenBraceAndIndent();
                {
                    WriteLine($"int status = JS_AddModuleExport(ctx, m, \"{@enum.Name}\");");
                    WriteLine("assert(status != -1);");
                    WriteLine("return;");
                }
                UnindentAndWriteCloseBrace();
                NewLine();

                var sources = new QuickJSRegisterImpl(Context);
                sources.Indent(CurrentIndentation);
                @enum.Visit(sources);
                Write(sources.Generate());

                WriteLine($"int status = JS_SetModuleExport(ctx, m, \"{@enum.Name}\", val);");
                WriteLine("assert(status != -1);");

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return true;
        }
    }

    public class QuickJSRegisterImpl : QuickJSInvokes
    {
        public QuickJSRegisterImpl(BindingContext context)
            : base(context, null)
        {
        }

        public override bool VisitClassDecl(Class @class)
        {
            var ctors = @class.Constructors.Where(c => c.IsGenerated).ToArray();
            var ctor = ctors.FirstOrDefault();
            if (ctor == null)
                return true;

            var funcDef = $"funcDef_{GetCIdentifier(Context, @class)}";
            var classId = $"classId_{GetCIdentifier(Context, @class)}";
            var classDef = $"classDef_{GetCIdentifier(Context, @class)}";

            WriteLine("if (!set)");
            WriteOpenBraceAndIndent();
            {
                WriteLine($"JS_AddModuleExport(ctx, m, \"{ctor.Name}\");");
                WriteLine("return;");
            }
            UnindentAndWriteCloseBrace();
            NewLine();

            WriteLine("if (phase == 0)");
            WriteOpenBraceAndIndent();
            {
                WriteLine($"JS_NewClassID(&{classId});");
                NewLine();

                WriteLine($"JS_NewClass(JS_GetRuntime(ctx), {classId}, &{classDef});");
                NewLine();

                WriteLine("JSValue proto = JS_NewObject(ctx);");
                WriteLine(
                    $"JS_SetPropertyFunctionList(ctx, proto, {funcDef}, sizeof({funcDef}) / sizeof({funcDef}[0]));");
                WriteLine($"JS_SetClassProto(ctx, {classId}, proto);");
                NewLine();

                if (ctor.IsGenerated && !ctor.IsCopyConstructor)
                {
                    var maxArgs = ctors.Max(m => m.Parameters.Count);
                    var callbackId = $"callback_method_{GetCIdentifier(Context, ctor)}";

                    Write($"JSValue ctor = JS_NewCFunction2(ctx,");
                    WriteLine($" {callbackId}, \"{ctor.Name}\", {maxArgs}, JS_CFUNC_constructor, 0);");
                    WriteLine($"JS_SetConstructor(ctx, ctor, proto);");
                    NewLine();

                    WriteLine($"JS_SetModuleExport(ctx, m, \"{ctor.Name}\", ctor);");
                }
            }
            UnindentAndWriteCloseBrace();

            if (@class.HasBaseClass && @class.BaseClass.IsGenerated)
            {
                WriteLine("else if (phase == 1)");
                WriteOpenBraceAndIndent();
                {
                    var baseClassId = $"classId_{GetCIdentifier(Context, @class.BaseClass)}";

                    WriteLine($"JSValue proto = JS_GetClassProto(ctx, {classId});");
                    WriteLine($"JSValue baseProto = JS_GetClassProto(ctx, {baseClassId});");
                    WriteLine("int err = JS_SetPrototype(ctx, proto, baseProto);");
                    WriteLine("assert(err != -1);");
                    WriteLine("JS_FreeValue(ctx, baseProto);");
                    WriteLine("JS_FreeValue(ctx, proto);");
                }
                UnindentAndWriteCloseBrace();
            }

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (method.IsConstructor)
                return true;

            PushBlock(BlockKind.Method);
            {
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            WriteLine("JSValue val = JS_NewObject(ctx);");
            NewLine();

            PushBlock();
            {
                foreach (var item in @enum.Items)
                    item.Visit(this);
            }
            PopBlock(NewLineKind.Always);

            return true;
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            PushBlock(BlockKind.EnumItem);
            {
                WriteLine("// " + item.Name);
                WriteOpenBraceAndIndent();
                {
                    var @enum = item.Namespace as Enumeration;
                    var ctx = new MarshalContext(Context, CurrentIndentation)
                    {
                        ArgName = @enum.GetItemValueAsString(item),
                        ReturnVarName = "item"
                    };

                    var marshal = GetMarshalNativeToManagedPrinter(ctx);
                    item.Visit(marshal);

                    if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                        Write(marshal.Context.Before);

                    WriteLine($"JS_SetPropertyStr(ctx, val, \"{item.Name}\", {marshal.Context.Return});");
                }
                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            return true;
        }

        public override bool VisitEvent(Event @event)
        {
            Console.WriteLine(@event.QualifiedName);
            return base.VisitEvent(@event);
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

        public override MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalManagedToNativePrinter(MarshalContext ctx)
        {
            return new QuickJSMarshalManagedToNativePrinter(ctx);
        }

        public override MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalNativeToManagedPrinter(MarshalContext ctx)
        {
            return new QuickJSMarshalNativeToManagedPrinter(ctx);
        }

        public override void GenerateFunctionGroup(List<Function> @group)
        {
            PushBlock(BlockKind.Function, @group);
            {
                GenerateFunctionCallback(@group);
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodGroup(List<Method> @group)
        {
            PushBlock(BlockKind.Method, @group);
            {
                GenerateFunctionCallback(@group.OfType<Function>().ToList());
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateFunctionSignature(Function function)
        {
            var type = function is Method ? "method" : "function";
            var callbackId = $"callback_{type}_{GetCIdentifier(Context, function)}";

            GenerateFunctionSignature(callbackId);
        }

        private void GenerateFunctionSignature(string id)
        {
            WriteLine($"static JSValue {id}(JSContext* ctx, JSValueConst this_val,");
            WriteLineIndent("int argc, JSValueConst* argv)");
        }

        public override void GenerateFunctionCallback(List<Function> @group)
        {
            var function = @group.First();
            WriteLine($"// {function.QualifiedName}");

            GenerateFunctionSignature(function);
            WriteOpenBraceAndIndent();

            // Check if the arguments are in the expected range.
            CheckArgumentsRange(@group);
            NewLine();

            var method = function as Method;
            if (method != null && !method.IsStatic)
            {
                var @class = method.Namespace as Class;
                if (method.IsConstructor)
                {
                    WriteLine($"{@class.QualifiedOriginalName}* instance;");
                }
                else if (QuickJSRegister.ClassNeedsExtraData(@class))
                {
                    var classDataId = $"data_{GetCIdentifier(Context, @class)}";
                    WriteLine($"auto data = ({classDataId}*) JS_GetOpaque(this_val, 0);");
                    WriteLine($"{@class.QualifiedOriginalName}* instance = ({@class.QualifiedOriginalName}*) data->instance;");
                }
                else
                {
                    Write($"{@class.QualifiedOriginalName}* instance = ");
                    WriteLine($"({@class.QualifiedOriginalName}*) JS_GetOpaque(this_val, 0);");
                }

                NewLine();
            }

            // Handle the zero arguments case right away if one exists.
            CheckZeroArguments(@group);
            NewLineIfNeeded();

            var needsArguments = @group.Any(f => f.Parameters.Any(p => p.IsGenerated));
            if (needsArguments)
            {
                var stateMachine = CalculateOverloadStates(@group);
                CheckArgumentsOverload(@group, stateMachine);

                // Error state.
                Unindent();
                WriteLine($"error:");
                Indent();

                WriteLine("return JS_ThrowTypeError(ctx, \"Unsupported argument type\");");
                NewLine();

                GenerateOverloadCalls(@group, stateMachine);
            }
            else
            {
                GenerateFunctionCall(function);
            }

            if (method != null && method.IsConstructor)
            {
                NewLine();

                Unindent();
                {
                    WriteLine("wrap:");
                }
                Indent();

                var @class = method.Namespace as Class;
                var classId = $"classId_{GetCIdentifier(Context, @class)}";

                GenerateJSClassInstance(classId);
                NewLine();

                var instanceKind = QuickJSRegister.ClassNeedsExtraData(@class)
                    ? "JS_INTEROP_INSTANCE_SIGNAL_CONTEXT" : "JS_INTEROP_INSTANCE_RAW_POINTER";
                WriteLine($"JS_Interop_InitObject(ctx, __obj, {instanceKind}, instance);");

                var hasExternalInstanceField = @class.Fields.Any(f => f.OriginalName == "__ExternalInstance");
                @class.FindHierarchy(c =>
                {
                    hasExternalInstanceField |= c.Fields.Any(f => f.OriginalName == "__ExternalInstance");
                    return hasExternalInstanceField;
                });

                if (hasExternalInstanceField)
                {
                    WriteLine("JSObject* __js_obj = JS_VALUE_GET_OBJ(__obj);");
                    WriteLine($"instance->__ExternalInstance = (void*) __js_obj;");
                    NewLine();
                }

                NewLine();
                WriteLine($"return __obj;");
            }

            UnindentAndWriteCloseBrace();
        }

        public void GenerateJSClassInstance(string classId, bool lookupProtoFromInstance = true)
        {
            if (lookupProtoFromInstance)
            {
                WriteLine("JSValue proto;");
                WriteLine("if (JS_IsUndefined(this_val))");
                WriteLineIndent($"proto = JS_GetClassProto(ctx, {classId});");
                WriteLine("else");
                WriteLineIndent($"proto = JS_GetProperty(ctx, this_val, JS_ATOM_prototype);");
            }
            else
            {
                WriteLine($"JSValue proto = JS_GetClassProto(ctx, {classId});");
            }

            NewLine();

            WriteLine("if (JS_IsException(proto))");
            WriteLineIndent("return proto;");
            NewLine();

            WriteLine($"JSValue __obj = JS_NewObjectProtoClass(ctx, proto, {classId});");
            WriteLine("JS_FreeValue(ctx, proto);");
        }

        public override void CheckArgumentsRange(IEnumerable<Function> @group)
        {
            var functions = @group as List<Function> ?? @group.ToList();

            var (minArgs, maxArgs) = (functions.Min(m => m.Parameters.Count),
                functions.Max(m => m.Parameters.Count));

            var requiredArgs = functions.Min(f => f.Parameters.FindIndex(p => p.HasDefaultValue));
            if (requiredArgs != -1)
                minArgs = requiredArgs;

            string rangeCheck;
            if (minArgs > 0)
                rangeCheck = minArgs == maxArgs ? $"argc != {minArgs}" : $"argc < {minArgs} || argc > {maxArgs}";
            else
                rangeCheck = $"argc > {maxArgs}";

            WriteLine($"if ({rangeCheck})");
            WriteLineIndent("return JS_ThrowRangeError(ctx, \"Unsupported number of arguments\");");
        }

        public override string GenerateTypeCheckForParameter(int paramIndex, Type type)
        {
            var typeChecker = new QuickJSTypeCheckGen(paramIndex);
            type.Visit(typeChecker);

            var condition = typeChecker.Generate();
            if (string.IsNullOrWhiteSpace(condition))
                throw new NotSupportedException();

            return condition;
        }

        public override bool VisitEvent(Event @event)
        {
            var getterId = $"callback_event_getter_{GetCIdentifier(Context, @event)}";
            PushBlock();
            {
                WriteLine($"JSValue {getterId}(JSContext *ctx, JSValueConst this_val)");
                WriteOpenBraceAndIndent();

                var @class = @event.Namespace as Class;
                var classId = $"classId_{GetCIdentifier(Context, @class)}";
                var classDataId = $"data_{GetCIdentifier(Context, @class)}";
                WriteLine($"auto data = ({classDataId}*) JS_GetOpaque(this_val, 0);");

                WriteLine($"if (data == nullptr)");
                WriteLineIndent("return JS_ThrowTypeError(ctx, \"Could not find object instance\");");
                NewLine();

                WriteLine($"JSValue event = JS_Interop_FindEvent(&data->events, {@event.GlobalId});");
                WriteLine($"if (!JS_IsUndefined(event))");
                WriteLineIndent($"return JS_DupValue(ctx, event);");
                NewLine();

                //GenerateJSClassInstance(QuickJSSources.SignalClassId, lookupProtoFromInstance: false);

                // Lookup Signal object constructor
                WriteLine($"JSValue signalProto = JS_GetClassProto(ctx, {QuickJSSources.SignalClassId});");
                WriteLine($"JSValue signalCtor = JS_GetProperty(ctx, signalProto, JS_ATOM_constructor);");

                //WriteLine("JSValue argv[] = { JS_DupValue(ctx, this_val) };");
                //WriteLine("JS_FreeValue(ctx, JS_CallConstructor2(ctx, signalCtor, __obj, 1, argv));");
                WriteLine("JSValue argv[] = { this_val };");
                WriteLine("JSValue __obj = JS_CallConstructor(ctx, signalCtor, 1, argv);");

                WriteLine("JS_FreeValue(ctx, signalCtor);");
                WriteLine("JS_FreeValue(ctx, signalProto);");
                NewLine();

                WriteLine($"JS_Interop_InsertEvent(&data->events, {@event.GlobalId}, JS_DupValue(ctx, __obj));");
                //WriteLine($"data->events[{eventIndex}] = JS_DupValue(ctx, __obj);");
                NewLine();

                var invokeId = $"event_invoke_{@event.OriginalName}";
                WriteLine($"(({@class.QualifiedOriginalName}*)data->instance)->{@event.OriginalName}.bind(data, &{classDataId}::{invokeId});");
                NewLine();

                WriteLine("return __obj;");
                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return true;
        }

        public void GenerateToString(Class @class)
        {
            PushBlock();
            {
                var callbackId = $"callback_class_{GetCIdentifier(Context, @class)}_toString";
                GenerateFunctionSignature(callbackId);
                WriteOpenBraceAndIndent();

                WriteLine($"return JS_NewString(ctx, \"{@class.Name}\");");

                UnindentAndWriteCloseBrace();
            }
            PopBlock(NewLineKind.BeforeNextBlock);
        }
    }
}
