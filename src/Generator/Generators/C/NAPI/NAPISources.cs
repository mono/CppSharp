using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.NAPI;
using CppSharp.Types.Std;
using CppSharp.Utils;
using CppSharp.Utils.FSM;
using static CppSharp.Generators.Cpp.NAPISources;

namespace CppSharp.Generators.Cpp
{
    public class NAPICodeGenerator : CCodeGenerator
    {
        public override string FileExtension => "cpp";

        public NAPICodeGenerator(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void VisitDeclContextFunctions(DeclarationContext context)
        {
            var functions = context.Functions.Where(f => !ASTUtils.CheckIgnoreFunction(f)).ToList();
            var unique = functions.GroupBy(m => m.Name);
            foreach (var group in unique)
                GenerateFunctionGroup(group.ToList());
        }

        public virtual void GenerateFunctionGroup(List<Function> group)
        {
            foreach (var function in group)
            {
                function.Visit(this);
                return;
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            return VisitClassDeclContext(@class);
        }

        public override void VisitClassConstructors(Class @class)
        {
            var constructors = @class.Constructors.Where(c => !ASTUtils.CheckIgnoreMethod(c)).ToList();
            if (!constructors.Any())
                return;

            GenerateMethodGroup(constructors);
        }

        public override void VisitClassMethods(Class @class)
        {
            var methods = @class.Methods.Where(m => !ASTUtils.CheckIgnoreMethod(m) &&
                !m.IsConstructor);
            var uniqueMethods = methods.GroupBy(m => m.Name);
            foreach (var group in uniqueMethods)
                GenerateMethodGroup(group.ToList());
        }

        public virtual void GenerateMethodGroup(List<Method> @group)
        {
            foreach (var method in @group)
            {
                method.Visit(this);
                return;
            }
        }
    }

    public class NAPISources : NAPICodeGenerator
    {
        public override string FileExtension => "cpp";

        public NAPISources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public static string GetTranslationUnitName(TranslationUnit unit)
        {
            var paths = unit.FileRelativePath.Split('/').ToList();
            paths = paths.Select(p => Path.GetFileNameWithoutExtension(p.ToLowerInvariant())).ToList();
            var name = string.Join('_', paths);
            return name;
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            WriteInclude(TranslationUnit.IncludePath, CInclude.IncludeKind.Angled);
            WriteInclude("node/node_api.h", CInclude.IncludeKind.Angled);
            WriteInclude("assert.h", CInclude.IncludeKind.Angled);
            WriteInclude("stdio.h", CInclude.IncludeKind.Angled);
            WriteInclude("NAPIHelpers.h", CInclude.IncludeKind.Quoted);
            PopBlock(NewLineKind.BeforeNextBlock);

            var registerGen = new NAPIRegister(Context, TranslationUnits);
            registerGen.Process();
            WriteLine(registerGen.Generate());

            var name = GetTranslationUnitName(TranslationUnit);
            WriteLine($"void register_{name}(napi_env env, napi_value exports)");
            WriteOpenBraceAndIndent();

            WriteLine("napi_value value;");
            NewLine();
            TranslationUnit.Visit(this);

            UnindentAndWriteCloseBrace();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

            PushBlock();
            WriteLine($"value = register_class_{GetCIdentifier(Context, @class)}(env);");
            WriteLine($"NAPI_CALL_NORET(env, napi_set_named_property(env, exports, \"{@class.Name}\", value));");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.IsOperator)
                return true;

            PushBlock();
            WriteLine($"value = register_function_{GetCIdentifier(Context, function)}(env);");
            WriteLine($"NAPI_CALL_NORET(env, napi_set_named_property(env, exports, \"{@function.Name}\", value));");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return false;

            PushBlock();
            WriteLine($"value = register_enum_{GetCIdentifier(Context, @enum)}(env, exports);");
            WriteLine($"NAPI_CALL_NORET(env, napi_set_named_property(env, exports, \"{@enum.Name}\", value));");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public static string GetCIdentifier(BindingContext context, Declaration decl,
            TypePrintScopeKind scope = TypePrintScopeKind.Qualified)
        {
            var cTypePrinter = new CppTypePrinter(context)
            {
                PrintFlavorKind = CppTypePrintFlavorKind.C,
                ScopeKind = TypePrintScopeKind.Local
            };

            var functionName = cTypePrinter.VisitDeclaration(decl).ToString();
            if (scope == TypePrintScopeKind.Local)
                return functionName;

            cTypePrinter.ScopeKind = scope;
            var qualifiedParentName = cTypePrinter.VisitDeclaration(decl.Namespace).ToString();

            // HACK: CppTypePrinter code calls into decl.QualifiedName, which does not take into
            // account language flavor, that code needs to be reworked. For now, hack around it.
            qualifiedParentName = qualifiedParentName.Replace("::", "_");

            return $"{qualifiedParentName}_{functionName}";
        }
    }

    public class NAPIRegister : NAPICodeGenerator
    {
        public NAPIRegister(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void Process()
        {
            TranslationUnit.Visit(this);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

            PushBlock(BlockKind.InternalsClass, @class);
            var callbacks = new NAPICallbacks(Context);
            @class.Visit(callbacks);
            Write(callbacks.Generate());
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(BlockKind.Class, @class);
            Write($"static napi_value register_class_{GetCIdentifier(Context, @class)}");
            WriteLine("(napi_env env)");
            WriteOpenBraceAndIndent();

            var sources = new NAPISourcesClass(Context);
            sources.Indent(CurrentIndentation);
            @class.Visit(sources);
            Write(sources.Generate());

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
            return false;
        }

        public override void GenerateFunctionGroup(List<Function> group)
        {
            var function = group.First();
            if (function.IsOperator)
                return;

            PushBlock(BlockKind.Function);
            var callbacks = new NAPICallbacks(Context);
            callbacks.GenerateFunctionGroup(group);
            Write(callbacks.Generate());
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(BlockKind.Function);

            Write($"static napi_value register_function_{GetCIdentifier(Context, function)}");
            WriteLine("(napi_env env)");
            WriteOpenBraceAndIndent();

            var sources = new NAPISourcesClass(Context);
            sources.Indent(CurrentIndentation);
            @function.Visit(sources);
            Write(sources.Generate());

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return false;

            PushBlock(BlockKind.Enum);

            Write($"static napi_value register_enum_{GetCIdentifier(Context, @enum)}");
            WriteLine("(napi_env env, napi_value exports)");
            WriteOpenBraceAndIndent();

            var sources = new NAPISourcesEnum(Context);
            sources.Indent(CurrentIndentation);
            @enum.Visit(sources);
            Write(sources.Generate());

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }
    }

    public class NAPICallbacks : NAPICodeGenerator
    {
        public NAPICallbacks(BindingContext context)
            : base(context, null)
        {
        }

        public override void GenerateFunctionGroup(List<Function> @group)
        {
            var function = @group.First();

            PushBlock(BlockKind.Function, function);
            GenerateFunctionCallback(@group);

            GenerateNativeCall(group);
            NewLine();

            WriteLine("return nullptr;");

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodGroup(List<Method> @group)
        {
            var method = @group.First();

            if (method.IsConstructor)
            {
                GenerateMethodDestructor(method);

                WriteLine($"static napi_ref ctor_{GetCIdentifier(Context, method)};");
                NewLine();
            }

            PushBlock(BlockKind.Method);
            GenerateFunctionCallback(@group.OfType<Function>().ToList());

            if (method.IsConstructor)
            {
                WriteLine("napi_ref result;");
                //WriteLine("napi_value result;");
                //WriteLine("NAPI_CALL(env, napi_create_object(env, &result));");

                var @class = method.Namespace as Class;
                if (@group.Any(m => m.IsDefaultConstructor))
                    WriteLine($"{@class.QualifiedOriginalName}* obj = new {@class.QualifiedOriginalName}();");
                else
                    WriteLine($"{@class.QualifiedOriginalName}* obj = nullptr;");

                WriteLine($"status = napi_wrap(env, _this, obj, dtor_{GetCIdentifier(Context, method)}, nullptr, &result);");
                WriteLine("assert(status == napi_ok);");
                NewLine();
            }
            else
            {
                GenerateNativeCall(@group);
            }

            WriteLine($"printf(\"{method.QualifiedName}: %lu\\n\", argc);");
            WriteLine("return _this;");

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public virtual void GenerateNativeCall(IEnumerable<Function> group)
        {

        }

        public virtual void GenerateFunctionCallback(IList<Function> @group)
        {
            var function = @group.First();

            WriteLine($"// {function.QualifiedName}");

            var type = function is Method ? "method" : "function";
            Write($"static napi_value callback_{type}_{GetCIdentifier(Context, function)}");
            WriteLine("(napi_env env, napi_callback_info info)");
            WriteOpenBraceAndIndent();

            WriteLine("napi_status status;");
            WriteLine("size_t argc;");
            WriteLine("status = napi_get_cb_info(env, info, &argc, nullptr, nullptr, nullptr);");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            // Check if the arguments are in the expected range.
            CheckArgumentsRange(@group);
            NewLine();

            var needsArguments = @group.Any(f => f.Parameters.Any(p => p.IsGenerated));
            if (!needsArguments)
            {
                GenerateFunctionCall(function);
                return;
            }

            WriteLine("napi_value args[argc], _this;");
            //WriteLine("void* data;");
            WriteLine("status = napi_get_cb_info(env, info, &argc, args, &_this, nullptr);");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            // Next we need to disambiguate which overload to call based on:
            // 1. Number of arguments passed to the method
            // 2. Type of arguments

            CheckArgumentsTypes(@group);
            NewLine();

            CheckArgumentsOverload(@group);
        }

        private void CheckArgumentsRange(IEnumerable<Function> @group)
        {
            var enumerable = @group as List<Function> ?? @group.ToList();
            var (minArgs, maxArgs) = (enumerable.Min(m => m.Parameters.Count),
                enumerable.Max(m => m.Parameters.Count));

            var rangeCheck = minArgs > 0 ? $"argc < {minArgs} || argc > {maxArgs}" : $"argc > {maxArgs}";

            WriteLine($"if ({rangeCheck})");
            WriteOpenBraceAndIndent();

            WriteLine("status = napi_throw_type_error(env, nullptr, \"Unsupported number of arguments\");");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine("return nullptr;");

            UnindentAndWriteCloseBrace();
        }

        private void CheckArgumentsTypes(IEnumerable<Function> @group)
        {
            WriteLine("napi_valuetype types[argc];");
            WriteLine("for (int i = 0; i < argc; i++)");
            WriteOpenBraceAndIndent();
            WriteLine("status = napi_typeof(env, args[i], &types[i]);");
            WriteLine("assert(status == napi_ok);");
            UnindentAndWriteCloseBrace();
        }

        private void CheckArgumentsOverload(IList<Function> @group)
        {
            // First handle the easy case of zero arguments.
            //WriteLine("if (argc == 0)");

            var stateMachine = CalculateOverloadStates(@group);
            var typeCheckStates = stateMachine.Q.Except(stateMachine.F).ToList();
            var finalStates = stateMachine.F;

            // Create a set of unique parameter types.
            var uniqueTypes = @group.SelectMany(method => method.Parameters)
                .Select(p => p.Type).Distinct().ToList();

            // Type check states.
            for (var i = 0; i < typeCheckStates.Count; i++)
            {
                NewLineIfNeeded();

                Unindent();
                WriteLine($"typecheck{i}:");
                Indent();

                var state = typeCheckStates[i];
                var transitions = stateMachine.Delta.Where(t => t.StartState == state).ToArray();

                foreach (var transition in transitions)
                {
                    NewLineIfNeeded();

                    var isInitialState = stateMachine.Q0.Contains(state);
                    var paramIndex =  isInitialState ? 0 :
                        int.Parse(transition.StartState.Split(' ').Last().Split('_').Last()) + 1;

                    var type = uniqueTypes[(int) transition.Symbol];
                    var condition = GenerateTypeCheckCondition(paramIndex, type);
                    WriteLine($"if ({condition})");

                    var nextState = typeCheckStates.Contains(transition.EndState)
                        ? $"typecheck{typeCheckStates.FindIndex(s => s == transition.EndState)}"
                        : $"overload{finalStates.FindIndex(s => s == transition.EndState)}";
                    WriteLineIndent($"goto {nextState};");
                    NewLine();
                }

                WriteLine("goto error;");
                NeedNewLine();

                NeedNewLine();
            }
            NewLineIfNeeded();

            // Error state.
            Unindent();
            WriteLine($"error:");
            Indent();

            WriteLine("status = napi_throw_type_error(env, nullptr, \"Unsupported argument type\");");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine("return nullptr;");
            NewLine();

            // Final states.
            for (var i = 0; i < finalStates.Count; i++)
            {
                NewLineIfNeeded();

                var function = group[i];
                WriteLine($"// {function.Signature}");

                Unindent();
                WriteLine($"overload{i}:");
                Indent();

                WriteOpenBraceAndIndent();
                GenerateFunctionCall(function);
                UnindentAndWriteCloseBrace();

                NeedNewLine();
            }
        }

        public void GenerateFunctionCall(Function function)
        {
            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);

            var needsReturn = !function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void);
            if (needsReturn)
            {
                CTypePrinter.PushContext(TypePrinterContextKind.Native);
                var returnType = function.ReturnType.Visit(CTypePrinter);
                CTypePrinter.PopContext();

                Write($"{returnType} {Helpers.ReturnIdentifier} = ");
            }

            var method = function as Method;
            var @class = function.Namespace as Class;

            var property = method?.AssociatedDeclaration as Property;
            var field = property?.Field;
            if (field != null)
            {
                Write($"((::{@class.QualifiedOriginalName}*){Helpers.InstanceIdentifier})->");
                Write($"{field.OriginalName}");

                var isGetter = property.GetMethod == method;
                if (isGetter)
                    WriteLine(";");
                else
                    WriteLine($" = {@params[0].Name};");
            }
            else
            {
                if (IsNativeFunctionOrStaticMethod(function))
                {
                    Write($"::{function.QualifiedOriginalName}(");
                }
                else
                {
                    if (function.IsNativeMethod())
                        Write($"((::{@class.QualifiedOriginalName}*){Helpers.InstanceIdentifier})->");

                    Write($"{base.GetMethodIdentifier(function, TypePrinterContextKind.Native)}(");
                }

                GenerateFunctionParams(@params);
                WriteLine(");");
            }

            foreach(var paramInfo in @params)
            {
                var param = paramInfo.Param;
                if(param.Usage != ParameterUsage.Out && param.Usage != ParameterUsage.InOut)
                    continue;

                if (param.Type.IsPointer() && !param.Type.GetFinalPointee().IsPrimitiveType())
                    param.QualifiedType = new QualifiedType(param.Type.GetFinalPointee());

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = paramInfo.Name,
                    ReturnVarName = paramInfo.Name,
                    ReturnType = param.QualifiedType
                };

                var marshal = new NAPIMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine($"{param.Name} = {marshal.Context.Return};");
            }

            if (needsReturn)
            {
                GenerateFunctionCallReturnMarshal(function);
            }
        }

        public virtual void GenerateFunctionCallReturnMarshal(Function function)
        {
            var ctx = new MarshalContext(Context, CurrentIndentation)
            {
                ArgName = Helpers.ReturnIdentifier,
                ReturnVarName = Helpers.ReturnIdentifier,
                ReturnType = function.ReturnType
            };

            var marshal = new NAPIMarshalNativeToManagedPrinter(ctx);
            function.ReturnType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
            {
                Write(marshal.Context.Before);
                NewLine();
            }

            WriteLine($"return {marshal.Context.Return};");
        }

        private string GenerateTypeCheckCondition(int paramIndex, CppSharp.AST.Type type)
        {
            if (type.IsPrimitiveType(out PrimitiveType primitive))
            {
                var condition = $"NAPI_IS_NUMBER(types[{paramIndex}])";

                if (primitive == PrimitiveType.Bool)
                    condition =$"NAPI_IS_BOOL(types[{paramIndex}])";

                else if (primitive == PrimitiveType.UInt)
                    condition =$"NAPI_IS_UINT32(types[{paramIndex}], args[{paramIndex}])";

                else if (primitive == PrimitiveType.LongLong)
                    condition =$"NAPI_IS_INT64(types[{paramIndex}], args[{paramIndex}])";

                else if (primitive == PrimitiveType.ULongLong)
                    condition =$"NAPI_IS_UINT64(types[{paramIndex}], args[{paramIndex}])";

                else if (primitive.IsIntegerType())
                    condition =$"NAPI_IS_INT32(types[{paramIndex}], args[{paramIndex}])";

                else if (primitive == PrimitiveType.Null)
                    condition =$"NAPI_IS_NULL(types[{paramIndex}])";

                return condition;
            }

            throw new NotImplementedException();
        }

        public bool IsNativeFunctionOrStaticMethod(Function function)
        {
            var method = function as Method;
            if (method == null)
                return true;

            if (!IsCLIGenerator && method.IsOperator)
                return false;

            if (method.IsOperator && Operators.IsBuiltinOperator(method.OperatorKind))
                return true;

            return method.IsStatic || method.Conversion != MethodConversionKind.None;
        }

        public struct ParamMarshal
        {
            public string Name;
            public string Prefix;

            public Parameter Param;
        }

        public List<ParamMarshal> GenerateFunctionParamsMarshal(IEnumerable<Parameter> @params,
            Function function = null)
        {
            var marshals = new List<ParamMarshal>();

            var paramIndex = 0;
            foreach (var param in @params)
            {
                marshals.Add(GenerateFunctionParamMarshal(param, paramIndex, function));
                paramIndex++;
            }

            return marshals;
        }

        public virtual ParamMarshal GenerateFunctionParamMarshal(Parameter param, int paramIndex,
            Function function = null)
        {
            var paramMarshal = new ParamMarshal { Name = param.Name, Param = param };

            var argName = Generator.GeneratedIdentifier("arg") + paramIndex.ToString(CultureInfo.InvariantCulture);

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

            var marshal = new NAPIMarshalManagedToNativePrinter(ctx);
            effectiveParam.Visit(marshal);

            if (string.IsNullOrEmpty(marshal.Context.Return))
                throw new Exception($"Cannot marshal argument of function '{function.QualifiedOriginalName}'");

            if (isRef)
            {
                var type = paramType.Visit(CTypePrinter);

                if (param.IsInOut)
                {
                    if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    {
                        Write(marshal.Context.Before);
                        NeedNewLine();
                    }

                    WriteLine($"{type} {argName} = {marshal.Context.Return};");
                }
                else
                    WriteLine($"{type} {argName};");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                {
                    Write(marshal.Context.Before);
                    NeedNewLine();
                }

                WriteLine($"auto {marshal.VarPrefix}{argName} = {marshal.Context.Return};");
                paramMarshal.Prefix = marshal.ArgumentPrefix;
                NewLineIfNeeded();
            }

            paramMarshal.Name = argName;
            return paramMarshal;
        }

        public void GenerateFunctionParams(List<ParamMarshal> @params)
        {
            var names = @params.Select(param =>
                string.IsNullOrWhiteSpace(param.Prefix) ? param.Name : (param.Prefix + param.Name))
                .ToList();

            Write(string.Join(", ", names));
        }

        private static DFSM CalculateOverloadStates(IEnumerable<Function> group)
        {
            var functionGroup = group.Where(m => m.Parameters.Count != 0).ToArray();

            // Create a set of unique parameter types.
            var uniqueTypes = functionGroup.SelectMany(method => method.Parameters)
                .Select(p => p.Type).Distinct().ToList();

            // Consider the alphabet as sequential ordered numbers, one per type.
            var Sigma = Enumerable.Range(0, uniqueTypes.Count).Select(i => (char) i).ToArray();

            var Q = new List<string> {"S"};

            var overloadStates = Enumerable.Range(0, functionGroup.Length).Select(i => $"F{i}")
                .ToArray();
            Q.AddRange(overloadStates);

            var Delta = new List<Transition>();

            // Setup states and transitions.
            for (var methodIndex = 0; methodIndex < functionGroup.Length; methodIndex++)
            {
                var method = functionGroup[methodIndex];
                var curState = "S";

                for (var paramIndex = 0; paramIndex < method.Parameters.Count; paramIndex++)
                {
                    var param = method.Parameters[paramIndex];
                    var typeIndex = uniqueTypes.FindIndex(p => p.Equals(param.Type));

                    var isLastTransition = paramIndex == method.Parameters.Count - 1;
                    var nextState = isLastTransition ? $"F{methodIndex}" : $"{methodIndex}_{paramIndex}";

                    if (!isLastTransition)
                        Q.Add(nextState);

                    Delta.Add(new Transition(curState, (char) typeIndex, nextState));
                    curState = nextState;
                }
            }

            var Q0 = new List<string> {"S"};
            var F = overloadStates;

            var NDFSM = new NDFSM(Q, Sigma, Delta, Q0, F);
            var DFSM = Minimize.PowersetConstruction(NDFSM);

#if OPTIMIZE_STATES
            DFSM = Minimize.MinimizeDFSM(DFSM);
#endif

            return DFSM;
        }

        private void GenerateMethodDestructor(Method method)
        {
            PushBlock(BlockKind.Destructor);

            Write($"static void dtor_{GetCIdentifier(Context, method)}");
            WriteLine("(napi_env env, void* finalize_data, void* finalize_hint)");
            WriteOpenBraceAndIndent();

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitMethodDecl(Method method)
        {
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            return true;
        }
    }

    public class NAPISourcesClass : NAPICodeGenerator
    {
        public NAPISourcesClass(BindingContext context)
            : base(context, null)
        {
        }

        public override bool VisitClassDecl(Class @class)
        {
            WriteLine("napi_status status;");

            WriteLine("napi_property_attributes attributes = (napi_property_attributes) (napi_default | napi_enumerable);");
            WriteLine("napi_property_descriptor props[] =");
            WriteOpenBraceAndIndent();
            WriteLine("// { utf8name, name, method, getter, setter, value, attributes, data }");

            VisitClassDeclContext(@class);
            NewLine();

            Unindent();
            WriteLine("};");
            NewLine();

            string ctorCallbackId = "nullptr";
            var ctor = @class.Constructors.FirstOrDefault();
            if (ctor != null)
            {
                var ctorQualifiedId = GetCIdentifier(Context, ctor);
                ctorCallbackId = $"callback_method_{ctorQualifiedId}";
            }

            WriteLine("napi_value constructor;");
            WriteLine($"status = napi_define_class(env, \"{@class.Name}\", NAPI_AUTO_LENGTH, " +
                      $"{ctorCallbackId}, nullptr, sizeof(props) / sizeof(props[0]), props, &constructor);");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine($"status = napi_create_reference(env, constructor, 1, &ctor_{GetCIdentifier(Context, ctor)});");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine("return constructor;");
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            PushBlock(BlockKind.Function);

            WriteLine("napi_status status;");

            var qualifiedId = GetCIdentifier(Context, function);
            var callbackId = $"callback_function_{qualifiedId}";

            CreateJSFunction(function);

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (method.IsConstructor)
                return true;

            PushBlock(BlockKind.Method);

            var attributes = "attributes";
            if (method.IsStatic)
                attributes += " | napi_static";

            var qualifiedId = GetCIdentifier(Context, method);
            var callbackId = $"callback_method_{qualifiedId}";
            Write($"{{ \"{method.Name}\", nullptr, {callbackId}, nullptr, nullptr, nullptr, (napi_property_attributes)({attributes}), nullptr }},");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        private void CreateJSFunction(Function function)
        {
            var qualifiedMethodId = GetCIdentifier(Context, function);
            var valueId = $"_{qualifiedMethodId}";
            var callbackId = $"callback_function_{qualifiedMethodId}";
            WriteLine($"napi_value {valueId};");
            WriteLine($"status = napi_create_function(env, \"{function.Name}\", NAPI_AUTO_LENGTH, " +
                      $"{callbackId}, 0, &{valueId});");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine($"return {valueId};");
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            return true;
        }
    }

    public class NAPISourcesEnum : NAPICodeGenerator
    {
        public NAPISourcesEnum(BindingContext context)
            : base(context, null)
        {
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            enumItemIndex = 0;

            WriteLine("napi_status status;");
            WriteLine("napi_value result;");
            WriteLine("NAPI_CALL(env, napi_create_object(env, &result));");
            NewLine();

            PushBlock();

            foreach (var item in @enum.Items)
                item.Visit(this);

            PopBlock(NewLineKind.Always);

            WriteLine("napi_property_attributes attributes = (napi_property_attributes) (napi_default | napi_enumerable);");
            WriteLine("napi_property_descriptor props[] =");
            WriteOpenBraceAndIndent();
            WriteLine("// { utf8name, name, method, getter, setter, value, attributes, data }");

            var items = @enum.Items.Where(item => item.IsGenerated).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var isLast = i == items.Count - 1;

                Write($"{{ \"{item.Name}\", nullptr, nullptr, nullptr, nullptr, i_{i}, attributes, nullptr }}");
                WriteLine(isLast ? string.Empty : ",");
            }

            Unindent();
            WriteLine("};");
            NewLine();

            var @sizeof = $"sizeof(props) / sizeof(props[0])";
            WriteLine($"NAPI_CALL(env, napi_define_properties(env, result, {@sizeof}, props));");
            NewLine();

            WriteLine("return result;");
            return true;
        }

        static int enumItemIndex = 0;

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            PushBlock(BlockKind.EnumItem);
            WriteLine("// " + item.Name);
            WriteLine($"napi_value i_{enumItemIndex};");

            var @enum = item.Namespace as Enumeration;
            var function = GetIntNAPICreateFunction(@enum.BuiltinType.Type);
            WriteLine($"status = {function}(env, {@enum.GetItemValueAsString(item)}, &i_{enumItemIndex++});");
            WriteLine("assert(status == napi_ok);");

            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            return true;
        }

        public static string GetIntNAPICreateFunction(PrimitiveType type)
        {
            switch(type)
            {
                case PrimitiveType.Bool:
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Short:
                case PrimitiveType.Int:
                case PrimitiveType.Long:
                    return "napi_create_int32";
                case PrimitiveType.UChar:
                case PrimitiveType.UShort:
                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                    return "napi_create_uint32";
                case PrimitiveType.LongLong:
                    return "napi_create_bigint_int64";
                case PrimitiveType.ULongLong:
                    return "napi_create_bigint_uint64";
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                case PrimitiveType.LongDouble:
                    return "napi_create_double";
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                    return "api_create_bigint_words";
                case PrimitiveType.String:
                    return "napi_create_string_utf8";
                case PrimitiveType.Null:
                case PrimitiveType.Void:
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Decimal:
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}