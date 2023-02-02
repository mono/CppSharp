using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.NAPI;
using CppSharp.Passes;
using CppSharp.Utils.FSM;
using static CppSharp.Generators.Cpp.NAPISources;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.Cpp
{
    public class NAPISources : NAPICodeGenerator
    {
        public override string FileExtension => "cpp";

        public NAPISources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
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

            var collector = new NAPIClassReturnCollector();
            TranslationUnit.Visit(collector);

            PushBlock();
            foreach (var @class in collector.Classes)
            {
                var ctor = @class.Methods.First(m => m.IsConstructor);
                WriteLine($"extern napi_ref ctor_{GetCIdentifier(Context, @ctor)};");
            }

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
            };
            cTypePrinter.PushScope(TypePrintScopeKind.Local);

            var functionName = cTypePrinter.VisitDeclaration(decl).ToString();
            if (scope == TypePrintScopeKind.Local)
                return functionName;

            cTypePrinter.PushScope(scope);
            var qualifiedParentName = cTypePrinter.VisitDeclaration(decl.Namespace).ToString();

            // HACK: CppTypePrinter code calls into decl.QualifiedName, which does not take into
            // account language flavor, that code needs to be reworked. For now, hack around it.
            qualifiedParentName = qualifiedParentName.Replace("::", "_");

            return $"{qualifiedParentName}_{functionName}";
        }
    }

    public class NAPIClassReturnCollector : TranslationUnitPass
    {
        public readonly HashSet<Class> Classes = new HashSet<Class>();
        private TranslationUnit translationUnit;

        public override bool VisitFunctionDecl(Function function)
        {
            if (!NAPICodeGenerator.ShouldGenerate(function))
                return false;

            var retType = function.ReturnType.Type.Desugar();
            if (retType.IsPointer())
                retType = retType.GetFinalPointee().Desugar();

            if (!(retType is TagType tagType))
                return base.VisitFunctionDecl(function);

            if (!(tagType.Declaration is Class @class))
                return base.VisitFunctionDecl(function);

            if (@class.TranslationUnit == translationUnit)
                return base.VisitFunctionDecl(function);

            Classes.Add(@class);
            return true;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            translationUnit = unit;
            return base.VisitTranslationUnit(unit);
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
            var callbacks = new NAPIInvokes(Context);
            @class.Visit(callbacks);
            Write(callbacks.Generate());
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(BlockKind.Class, @class);
            Write($"static napi_value register_class_{GetCIdentifier(Context, @class)}");
            WriteLine("(napi_env env)");
            WriteOpenBraceAndIndent();

            var sources = new NAPIRegisterImpl(Context);
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
            var callbacks = new NAPIInvokes(Context);
            callbacks.GenerateFunctionGroup(group);
            Write(callbacks.Generate());
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(BlockKind.Function);

            Write($"static napi_value register_function_{GetCIdentifier(Context, function)}");
            WriteLine("(napi_env env)");
            WriteOpenBraceAndIndent();

            WriteLine("napi_status status;");

            var qualifiedMethodId = GetCIdentifier(Context, function);
            var valueId = $"_{qualifiedMethodId}";
            WriteLine($"napi_value {valueId};");

            var callbackId = $"callback_function_{qualifiedMethodId}";
            WriteLine($"status = napi_create_function(env, \"{function.Name}\", NAPI_AUTO_LENGTH, " +
                      $"{callbackId}, 0, &{valueId});");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine($"return {valueId};");

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

            var sources = new NAPIRegisterImpl(Context);
            sources.Indent(CurrentIndentation);
            @enum.Visit(sources);
            Write(sources.Generate());

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }
    }

    public class NAPIRegisterImpl : NAPICodeGenerator
    {
        public NAPIRegisterImpl(BindingContext context)
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
            @class.FindHierarchy(c =>
            {
                WriteLine($"// {@class.QualifiedOriginalName}");
                return VisitClassDeclContext(c);
            });
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
            var (_, function) = NAPIMarshalNativeToManagedPrinter.GetNAPIPrimitiveType(@enum.BuiltinType.Type);
            WriteLine($"status = {function}(env, {@enum.GetItemValueAsString(item)}, &i_{enumItemIndex++});");
            WriteLine("assert(status == napi_ok);");

            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            return true;
        }
    }

    public class NAPIInvokes : NAPICodeGenerator
    {
        public NAPIInvokes(BindingContext context)
            : base(context, null)
        {
        }

        public NAPIInvokes(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void GenerateFunctionGroup(List<Function> @group)
        {
            var function = @group.First();

            PushBlock(BlockKind.Function, function);

            GenerateFunctionCallback(@group);
            NewLine();

            // TODO:
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
            NewLine();

            WriteLine("return _this;");
            UnindentAndWriteCloseBrace();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public virtual void GenerateFunctionCallback(List<Function> @group)
        {
            var function = @group.First();

            WriteLine($"// {function.QualifiedName}");

            var type = function is Method ? "method" : "function";
            var callbackId = $"callback_{type}_{GetCIdentifier(Context, function)}";

            Write($"static napi_value {callbackId}");
            WriteLine("(napi_env env, napi_callback_info info)");
            WriteOpenBraceAndIndent();

            WriteLine("napi_status status;");
            WriteLine("napi_value _this;");
            WriteLine("size_t argc;");
            WriteLine("status = napi_get_cb_info(env, info, &argc, nullptr, &_this, nullptr);");
            WriteLine("assert(status == napi_ok);");
            NewLine();

            WriteLine("napi_value args[argc];");
            WriteLine("napi_valuetype types[argc];");
            NewLine();

            // Handle the zero arguments case right away if one exists.
            CheckZeroArguments(@group);
            NewLineIfNeeded();

            // Check if the arguments are in the expected range.
            CheckArgumentsRange(@group);
            NewLine();

            var needsArguments = @group.Any(f => f.Parameters.Any(p => p.IsGenerated));
            if (needsArguments)
            {
                WriteLine("status = napi_get_cb_info(env, info, &argc, args, nullptr, nullptr);");
                WriteLine("assert(status == napi_ok);");
                NewLine();

                WriteLine("for (size_t i = 0; i < argc; i++)");
                WriteOpenBraceAndIndent();
                {
                    WriteLine("status = napi_typeof(env, args[i], &types[i]);");
                    WriteLine("assert(status == napi_ok);");
                }
                UnindentAndWriteCloseBrace();
                NewLine();
            }

            var method = function as Method;
            if (method != null)
            {
                var @class = method.Namespace as Class;
                if (method.IsConstructor)
                {
                    WriteLine($"{@class.QualifiedOriginalName}* instance = nullptr;");
                }
                else
                {
                    WriteLine($"{@class.QualifiedOriginalName}* instance;");
                    WriteLine("status = napi_unwrap(env, _this, (void**) &instance);");
                }

                NewLine();
            }

            if (needsArguments)
            {
                var stateMachine = CalculateOverloadStates(@group);
                CheckArgumentsOverload(@group, stateMachine);

                // Error state.
                Unindent();
                WriteLine($"error:");
                Indent();

                WriteLine("status = napi_throw_type_error(env, nullptr, \"Unsupported argument type\");");
                WriteLine("assert(status == napi_ok);");
                NewLine();

                WriteLine("return nullptr;");
                NewLine();

                GenerateOverloadCalls(@group, stateMachine);
            }
            else
            {
                GenerateFunctionCall(function);
            }

            if (method != null && method.IsConstructor)
            {
                WriteLine("napi_ref result;");
                WriteLine($"status = napi_wrap(env, _this, instance, dtor_{GetCIdentifier(Context, method)}" +
                          $", nullptr, &result);");
                WriteLine("assert(status == napi_ok);");
                NewLine();
            }
        }

        public virtual void CheckZeroArguments(List<Function> @group)
        {
            var zeroParamsOverload = @group.Where(f => f.Parameters.Count == 0 ||
                (f.Parameters.Count >= 1 && f.Parameters.First().HasDefaultValue)).ToArray();

            if (zeroParamsOverload.Length == 0 || @group.Count <= 1)
                return;

            if (zeroParamsOverload.Length > 1)
                throw new Exception($"Found ambiguity between default parameter overloads for: " +
                                    $"{@group.First().QualifiedOriginalName}");

            var index = @group.FindIndex(f => f == zeroParamsOverload.First());
            WriteLine($"if (argc == 0)");
            WriteLineIndent($"goto overload{index};");
            NeedNewLine();
        }

        public virtual void CheckArgumentsRange(IEnumerable<Function> @group)
        {
            var enumerable = @group as List<Function> ?? @group.ToList();
            var (minArgs, maxArgs) = (enumerable.Min(m => m.Parameters.Count),
                enumerable.Max(m => m.Parameters.Count));

            var rangeCheck = minArgs > 0 ? $"argc < {minArgs} || argc > {maxArgs}" : $"argc > {maxArgs}";

            WriteLine($"if ({rangeCheck})");
            WriteOpenBraceAndIndent();
            {
                WriteLine("status = napi_throw_type_error(env, nullptr, \"Unsupported number of arguments\");");
                WriteLine("assert(status == napi_ok);");
                NewLine();

                WriteLine("return nullptr;");
            }
            UnindentAndWriteCloseBrace();
        }

        public virtual void CheckArgumentsOverload(List<Function> @group, DFSM stateMachine)
        {
            int GetParamIndex(Transition transition, string state)
            {
                var isInitialState = stateMachine.Q0.Contains(state);
                var paramIndex = isInitialState ? 0 : int.Parse(transition.StartState.Split(' ').Last().Split('_').Last()) + 1;
                return paramIndex;
            }

            var typeCheckStates = stateMachine.Q.Except(stateMachine.F).ToList();
            var finalStates = stateMachine.F;

            // Create a set of unique parameter types.
            var uniqueTypes = @group.SelectMany(method => method.Parameters)
                .Select(p => p.Type).Distinct().ToList();

            // Type check states.
            for (var i = 0; i < typeCheckStates.Count; i++)
            {
                NewLineIfNeeded();

                if (i > 0)
                {
                    Unindent();
                    WriteLine($"typecheck{i}:");
                    Indent();
                }

                var state = typeCheckStates[i];
                var transitions = stateMachine.Delta.Where(t => t.StartState == state).ToArray();

                // Deal with default parameters.
                var currentParamIndex = GetParamIndex(transitions.First(), state);
                var defaultParams = @group.Where(f => f.Parameters.Count > currentParamIndex)
                                                    .Select(f => f.Parameters[currentParamIndex])
                                                    .Where(p => p.HasDefaultValue).ToArray();

                // Check for an ambiguous case of overload resolution and throw. This does prevents some technically
                // legal overloads from being generated, so in the future this can be updated to just throw
                // a runtime error in JS instead, which would allow the ambiguous overloads to be called when
                // resolved with all explicit arguments.
                if (defaultParams.Length > 1)
                    throw new Exception($"Found ambiguity between default parameter overloads for: " +
                                        $"{@group.First().QualifiedOriginalName}");

                if (defaultParams.Length == 1)
                {
                    WriteLine($"if (argc == {currentParamIndex})");

                    var overload = defaultParams.First().Namespace as Function;
                    var index = @group.FindIndex(f => f == overload);
                    WriteLineIndent($"goto overload{index};");
                    NewLine();
                }

                foreach (var transition in transitions)
                {
                    NewLineIfNeeded();

                    var paramIndex = GetParamIndex(transition, state);
                    var type = uniqueTypes[transition.Symbol];
                    var condition = GenerateTypeCheckForParameter(paramIndex, type);

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
        }

        public virtual string GenerateTypeCheckForParameter(int paramIndex, Type type)
        {
            var typeChecker = new NAPITypeCheckGen(paramIndex);
            type.Visit(typeChecker);

            var condition = typeChecker.Generate();
            if (string.IsNullOrWhiteSpace(condition))
                throw new NotSupportedException();

            return condition;
        }

        public virtual void GenerateOverloadCalls(IList<Function> @group, DFSM stateMachine)
        {
            // Final states.
            for (var i = 0; i < stateMachine.F.Count; i++)
            {
                NewLineIfNeeded();

                var function = @group[i];
                WriteLine($"// {function.Signature}");

                Unindent();
                WriteLine($"overload{i}:");
                Indent();

                WriteOpenBraceAndIndent();
                {
                    GenerateFunctionCall(function);
                }
                UnindentAndWriteCloseBrace();
                NeedNewLine();
            }
        }

        public virtual void GenerateFunctionCall(Function function)
        {
            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);
            var method = function as Method;
            var isVoidReturn = function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void);

            PushBlock();
            {
                if (!isVoidReturn)
                {
                    CTypePrinter.PushContext(TypePrinterContextKind.Native);
                    var returnType = function.ReturnType.Visit(CTypePrinter);
                    CTypePrinter.PopContext();

                    Write($"{returnType} {Helpers.ReturnIdentifier} = ");
                }

                var @class = function.Namespace as Class;
                var property = method?.AssociatedDeclaration as Property;
                var field = property?.Field;
                if (field != null)
                {
                    Write($"instance->{field.OriginalName}");

                    var isGetter = property.GetMethod == method;
                    if (isGetter)
                        WriteLine(";");
                    else
                        WriteLine($" = {@params[0].Name};");
                }
                else
                {
                    if (method != null && method.IsConstructor)
                    {
                        Write($"instance = new {@class.QualifiedOriginalName}(");
                    }
                    else if (IsNativeFunctionOrStaticMethod(function))
                    {
                        Write($"::{function.QualifiedOriginalName}(");
                    }
                    else
                    {
                        if (function.IsNativeMethod())
                            Write($"instance->");

                        Write($"{base.GetMethodIdentifier(function, TypePrinterContextKind.Native)}(");
                    }

                    GenerateFunctionParams(@params);
                    WriteLine(");");
                }
            }
            PopBlock(NewLineKind.IfNotEmpty);

            GenerateFunctionParamsMarshalCleanups(@params);

            var isCtor = method != null && method.IsConstructor;
            var needsReturn = !isVoidReturn || (this is QuickJSInvokes && !isCtor);
            if (needsReturn)
            {
                NewLine();
                GenerateFunctionCallReturnMarshal(function);
            }

            if (isCtor)
            {
                WriteLine("goto wrap;");
            }
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

        public void GenerateFunctionParams(List<ParamMarshal> @params)
        {
            var names = @params.Select(param =>
                string.IsNullOrWhiteSpace(param.Prefix) ? param.Name : (param.Prefix + param.Name))
                .ToList();

            Write(string.Join(", ", names));
        }

        public static DFSM CalculateOverloadStates(IEnumerable<Function> group)
        {
            var functionGroup = group.ToList();

            // Create a set of unique parameter types.
            var uniqueTypes = functionGroup.SelectMany(method => method.Parameters)
                .Select(p => p.Type).Distinct().ToList();

            // Consider the alphabet as sequential ordered numbers, one per type.
            var Sigma = Enumerable.Range(0, uniqueTypes.Count).Select(i => (char)i).ToArray();

            var Q = new List<string> { "S" };

            var overloadStates = Enumerable.Range(0, functionGroup.Count).Select(i => $"F{i}")
                .ToArray();
            Q.AddRange(overloadStates);

            var Delta = new List<Transition>();

            // Setup states and transitions.
            for (var methodIndex = 0; methodIndex < functionGroup.Count; methodIndex++)
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

                    Delta.Add(new Transition(curState, (char)typeIndex, nextState));
                    curState = nextState;
                }
            }

            var Q0 = new List<string> { "S" };
            var F = overloadStates;

            var NDFSM = new NDFSM(Q, Sigma, Delta, Q0, F);
            var DFSM = Minimize.PowersetConstruction(NDFSM);

            // Add the zero-parameters overload manually if one exists since it got optimized out.
            var zeroParamsOverload = functionGroup.SingleOrDefault(f => f.Parameters.Count == 0);
            if (zeroParamsOverload != null)
            {
                var index = functionGroup.FindIndex(f => f == zeroParamsOverload);
                DFSM.F.Insert(index, $"F{index}");
            }

#if OPTIMIZE_STATES
            DFSM = Minimize.MinimizeDFSM(DFSM);
#endif

            // The construction step above can result in unordered final states, so re-order them to
            // make the following code generation steps easier.

            DFSM.F = DFSM.F.OrderBy(f => f).ToList();

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
}