using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using CppSharp.Generators.C;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI source files.
    /// </summary>
    public class CLISources : CLITemplate
    {
        public CLISources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override string FileExtension => "cpp";

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            var file = Context.Options.GetIncludePath(TranslationUnit);
            WriteLine($"#include \"{file}\"");
            GenerateForwardReferenceHeaders();

            NewLine();
            PopBlock();

            PushBlock(BlockKind.Usings);
            WriteLine("using namespace System;");
            WriteLine("using namespace System::Runtime::InteropServices;");
            foreach (var customUsingStatement in Options.DependentNameSpaces)
            {
                WriteLine($"using namespace {customUsingStatement};"); ;
            }
            NewLine();
            PopBlock();

            GenerateDeclContext(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateForwardReferenceHeaders()
        {
            PushBlock(BlockKind.IncludesForwardReferences);

            var typeReferenceCollector = new CLITypeReferenceCollector(Context.TypeMaps, Context.Options);
            typeReferenceCollector.Process(TranslationUnit, filterNamespaces: false);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                var filename = Context.Options.GenerateName != null ? $"{Context.Options.GenerateName(TranslationUnit)}{Path.GetExtension(TranslationUnit.FileName)}" : TranslationUnit.FileName;
                if (typeRef.Include.File == filename)
                    continue;

                var include = typeRef.Include;
                if (!string.IsNullOrEmpty(include.File) && !include.InHeader)
                    includes.Add(include.ToString());
            }

            foreach (var include in includes)
                WriteLine(include);

            PopBlock();
        }

        private void GenerateDeclContext(DeclarationContext @namespace)
        {
            PushBlock(BlockKind.Namespace);
            foreach (var @class in @namespace.Classes)
            {
                if (!@class.IsGenerated || @class.IsDependent)
                    continue;

                if (@class.IsOpaque || @class.IsIncomplete)
                    continue;

                GenerateClass(@class);
            }

            PushBlock(BlockKind.FunctionsClass, @namespace);
            // Generate all the function declarations for the module.
            foreach (var function in @namespace.Functions.Where(f => f.IsGenerated))
            {
                GenerateFunction(function, @namespace);
                NewLine();
            }
            PopBlock();

            if (Options.GenerateFunctionTemplates)
            {
                foreach (var template in @namespace.Templates)
                {
                    if (!template.IsGenerated) continue;

                    var functionTemplate = template as FunctionTemplate;
                    if (functionTemplate == null) continue;

                    if (!functionTemplate.IsGenerated)
                        continue;

                    GenerateFunctionTemplate(functionTemplate);
                }
            }

            foreach (var childNamespace in @namespace.Namespaces)
                GenerateDeclContext(childNamespace);

            PopBlock();
        }

        public void GenerateClass(Class @class)
        {
            PushBlock(BlockKind.Class);

            GenerateDeclContext(@class);

            GenerateClassConstructors(@class);

            GenerateClassMethods(@class, @class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                var qualifiedIdentifier = QualifiedIdentifier(@class);

                PushBlock(BlockKind.Method);
                WriteLine("::System::IntPtr {0}::{1}::get()",
                    qualifiedIdentifier, Helpers.InstanceIdentifier);
                WriteOpenBraceAndIndent();
                WriteLine("return ::System::IntPtr(NativePtr);");
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);

                PushBlock(BlockKind.Method);
                WriteLine("void {0}::{1}::set(::System::IntPtr object)",
                    qualifiedIdentifier, Helpers.InstanceIdentifier);
                WriteOpenBraceAndIndent();
                var nativeType = $"{typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*";
                WriteLine("NativePtr = ({0})object.ToPointer();", nativeType);
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            GenerateClassProperties(@class, @class);

            foreach (var @event in @class.Events)
            {
                if (!@event.IsGenerated)
                    continue;

                GenerateDeclarationCommon(@event);
                GenerateEvent(@event, @class);
            }

            foreach (var variable in @class.Variables)
            {
                if (!variable.IsGenerated)
                    continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                GenerateDeclarationCommon(variable);
                GenerateVariable(variable, @class);
            }

            PopBlock();
        }

        private void GenerateClassConstructors(Class @class)
        {
            if (@class.IsStatic)
                return;

            // Output the default constructors taking the native pointer and ownership info.
            GenerateClassConstructor(@class);
            GenerateClassConstructor(@class, true);

            if (@class.IsRefType)
            {
                var destructor = @class.Destructors
                    .FirstOrDefault(d => d.Parameters.Count == 0 && d.Access == AccessSpecifier.Public);
                if (destructor != null)
                {
                    GenerateClassDestructor(@class);
                    if (Options.GenerateFinalizerFor(@class))
                        GenerateClassFinalizer(@class);
                }
            }
        }

        private void GenerateClassMethods(Class @class, Class realOwner)
        {
            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    GenerateClassMethods(@base.Class, realOwner);

            foreach (var method in @class.Methods.Where(m => @class == realOwner || !m.IsOperator))
            {
                if (ASTUtils.CheckIgnoreMethod(method) || CLIHeaders.FunctionIgnored(method))
                    continue;

                // C++/CLI does not allow special member funtions for value types.
                if (@class.IsValueType && method.IsCopyConstructor)
                    continue;

                // Do not generate constructors or destructors from base classes.
                var declaringClass = method.Namespace as Class;
                if (declaringClass != realOwner && (method.IsConstructor || method.IsDestructor))
                    continue;

                GenerateMethod(method, realOwner);
            }
        }

        private void GenerateClassProperties(Class @class, Class realOwner)
        {
            if (@class.IsValueType)
            {
                foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
                {
                    GenerateClassProperties(@base.Class, realOwner);
                }
            }

            foreach (var property in @class.Properties.Where(
                p => !ASTUtils.CheckIgnoreProperty(p) && !p.IsInRefTypeAndBackedByValueClassField() &&
                        !CLIHeaders.TypeIgnored(p.Type)))
                GenerateProperty(property, realOwner);
        }

        private void GenerateClassDestructor(Class @class)
        {
            PushBlock(BlockKind.Destructor);

            WriteLine("{0}::~{1}()", QualifiedIdentifier(@class), @class.Name);
            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.DestructorBody, @class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                WriteLine("delete NativePtr;");
            }
            else if (@class.HasNonTrivialDestructor)
            {
                WriteLine("if (NativePtr)");
                WriteOpenBraceAndIndent();
                WriteLine("auto __nativePtr = NativePtr;");
                WriteLine("NativePtr = 0;");
                WriteLine($"delete ({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*) __nativePtr;", @class.QualifiedOriginalName);
                UnindentAndWriteCloseBrace();
            }

            PopBlock();

            UnindentAndWriteCloseBrace();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassFinalizer(Class @class)
        {
            PushBlock(BlockKind.Finalizer);

            WriteLine("{0}::!{1}()", QualifiedIdentifier(@class), @class.Name);
            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.FinalizerBody, @class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
                WriteLine("delete NativePtr;");

            PopBlock();

            UnindentAndWriteCloseBrace();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateFunctionTemplate(FunctionTemplate template)
        {
            PushBlock(BlockKind.Template);

            var function = template.TemplatedFunction;

            var typePrinter = new CLITypePrinter(Context);
            typePrinter.PushContext(TypePrinterContextKind.Template);

            var retType = function.ReturnType.Visit(typePrinter);

            var typeNames = "";
            var paramNames = template.Parameters.Select(param => param.Name).ToList();
            if (paramNames.Any())
                typeNames = "typename " + string.Join(", typename ", paramNames);

            WriteLine("generic<{0}>", typeNames);
            WriteLine("{0} {1}::{2}({3})", retType,
                QualifiedIdentifier(function.Namespace), function.Name,
                GenerateParametersList(function.Parameters));

            WriteOpenBraceAndIndent();

            var @class = function.Namespace as Class;
            GenerateFunctionCall(function, @class);

            UnindentAndWriteCloseBrace();
            NewLine();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateProperty(Property property, Class realOwner)
        {
            PushBlock(BlockKind.Property);

            if (property.Field != null)
            {
                if (property.HasGetter)
                    GeneratePropertyGetter(property.Field, realOwner, property.Name,
                        property.Type);

                if (property.HasSetter)
                    GeneratePropertySetter(property.Field, realOwner, property.Name,
                        property.Type);
            }
            else
            {
                if (property.HasGetter)
                    GeneratePropertyGetter(property.GetMethod, realOwner, property.Name,
                        property.Type);

                if (property.HasSetter)
                    if (property.IsIndexer)
                        GeneratePropertySetter(property.SetMethod, realOwner, property.Name,
                            property.Type, property.GetMethod.Parameters[0]);
                    else
                        GeneratePropertySetter(property.SetMethod, realOwner, property.Name,
                            property.Type);
            }
            PopBlock();
        }

        private void GeneratePropertySetter<T>(T decl, Class @class, string name, Type type, Parameter indexParameter = null)
            where T : Declaration, ITypedDecl
        {
            if (decl == null)
                return;

            var args = new List<string>();
            var isIndexer = indexParameter != null;
            if (isIndexer)
                args.Add($"{indexParameter.Type} {indexParameter.Name}");

            var function = decl as Function;
            var argName = function != null && !isIndexer ? function.Parameters[0].Name : "value";
            args.Add($"{type} {argName}");

            WriteLine("void {0}::{1}::set({2})", QualifiedIdentifier(@class),
                name, string.Join(", ", args));

            WriteOpenBraceAndIndent();

            if (decl is Function && !isIndexer)
            {
                var func = decl as Function;
                var @void = new BuiltinType(PrimitiveType.Void);
                GenerateFunctionCall(func, @class, @void);
            }
            else
            {
                if (@class.IsValueType && decl is Field)
                {
                    WriteLine("{0} = value;", decl.Name);
                    UnindentAndWriteCloseBrace();
                    NewLine();
                    return;
                }
                var param = new Parameter
                {
                    Name = "value",
                    QualifiedType = new QualifiedType(type)
                };

                string variable;
                if (decl is Variable)
                    variable = $"::{@class.QualifiedOriginalName}::{decl.OriginalName}";
                else
                    variable = $"(({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)NativePtr)->{decl.OriginalName}";

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    Parameter = param,
                    ArgName = param.Name,
                    ReturnVarName = variable
                };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (isIndexer)
                {
                    var ctx2 = new MarshalContext(Context, CurrentIndentation)
                    {
                        Parameter = indexParameter,
                        ArgName = indexParameter.Name
                    };

                    var marshal2 = new CLIMarshalManagedToNativePrinter(ctx2);
                    indexParameter.Visit(marshal2);

                    variable += string.Format("({0})", marshal2.Context.Return);
                }

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                if (marshal.Context.Return.StringBuilder.Length > 0)
                {
                    if (isIndexer && decl.Type.IsPointer())
                        WriteLine("*({0}) = {1};", variable, marshal.Context.Return);
                    else
                        WriteLine("{0} = {1};", variable, marshal.Context.Return);
                }
            }

            UnindentAndWriteCloseBrace();
            NewLine();
        }

        private void GeneratePropertyGetter<T>(T decl, Class @class, string name, Type type)
            where T : Declaration, ITypedDecl
        {
            if (decl == null)
                return;

            var method = decl as Method;
            var isIndexer = method != null &&
                method.OperatorKind == CXXOperatorKind.Subscript;

            var args = new List<string>();
            if (isIndexer)
            {
                var indexParameter = method.Parameters[0];
                args.Add(string.Format("{0} {1}", indexParameter.Type, indexParameter.Name));
            }

            WriteLine("{0} {1}::{2}::get({3})", type, QualifiedIdentifier(@class),
                      name, string.Join(", ", args));

            WriteOpenBraceAndIndent();

            if (decl is Function)
            {
                var func = decl as Function;
                if (isIndexer && func.Type.IsAddress())
                    GenerateFunctionCall(func, @class, type);
                else
                    GenerateFunctionCall(func, @class);
            }
            else
            {
                if (@class.IsValueType && decl is Field)
                {
                    WriteLine($"return {decl.Name};");
                    UnindentAndWriteCloseBrace();
                    NewLine();
                    return;
                }

                string variable;
                if (decl is Variable)
                    variable = $"::{@class.QualifiedOriginalName}::{decl.OriginalName}";
                else if (CLIGenerator.ShouldGenerateClassNativeField(@class))
                    variable = $"NativePtr->{decl.OriginalName}";
                else
                    variable = $"(({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)NativePtr)->{decl.OriginalName}";

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = decl.Name,
                    ReturnVarName = variable,
                    ReturnType = decl.QualifiedType
                };
                ctx.PushMarshalKind(MarshalKind.NativeField);

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                decl.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine($"return {marshal.Context.Return};");
            }


            UnindentAndWriteCloseBrace();
            NewLine();
        }

        private void GenerateEvent(Event @event, Class @class)
        {
            GenerateEventAdd(@event, @class);
            NewLine();

            GenerateEventRemove(@event, @class);
            NewLine();

            GenerateEventRaise(@event, @class);
            NewLine();

            GenerateEventRaiseWrapper(@event, @class);
            NewLine();
        }

        private void GenerateEventAdd(Event @event, Class @class)
        {
            WriteLine("void {0}::{1}::add({2} evt)", QualifiedIdentifier(@class),
                      @event.Name, @event.Type);
            WriteOpenBraceAndIndent();

            var delegateName = string.Format("_{0}Delegate", @event.Name);

            WriteLine("if (!{0}Instance)", delegateName);
            WriteOpenBraceAndIndent();

            var args = typePrinter.VisitParameters(@event.Parameters, hasNames: false);

            WriteLine("{0}Instance = gcnew {0}(this, &{1}::_{2}Raise);",
                      delegateName, QualifiedIdentifier(@class), @event.Name);

            WriteLine("auto _fptr = (void (*)({0}))Marshal::GetFunctionPointerForDelegate({1}Instance).ToPointer();",
                args, delegateName);

            WriteLine($"(({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)NativePtr)->{@event.OriginalName}.Connect(_fptr);");

            UnindentAndWriteCloseBrace();

            WriteLine("_{0} = static_cast<{1}>(::System::Delegate::Combine(_{0}, evt));",
                @event.Name, @event.Type);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateEventRemove(Event @event, Class @class)
        {
            WriteLine("void {0}::{1}::remove({2} evt)", QualifiedIdentifier(@class),
                      @event.Name, @event.Type);
            WriteOpenBraceAndIndent();

            WriteLine("_{0} = static_cast<{1}>(::System::Delegate::Remove(_{0}, evt));",
                @event.Name, @event.Type);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateEventRaise(Event @event, Class @class)
        {
            var typePrinter = new CLITypePrinter(Context);
            var args = typePrinter.VisitParameters(@event.Parameters, hasNames: true);

            WriteLine("void {0}::{1}::raise({2})", QualifiedIdentifier(@class),
                      @event.Name, args);

            WriteOpenBraceAndIndent();

            var paramNames = @event.Parameters.Select(param => param.Name).ToList();
            WriteLine("_{0}({1});", @event.Name, string.Join(", ", paramNames));

            UnindentAndWriteCloseBrace();
        }

        private void GenerateEventRaiseWrapper(Event @event, Class @class)
        {
            var args = typePrinter.VisitParameters(@event.Parameters, hasNames: true);

            WriteLine("void {0}::_{1}Raise({2})", QualifiedIdentifier(@class),
                @event.Name, args);

            WriteOpenBraceAndIndent();

            var returns = new List<string>();
            foreach (var param in @event.Parameters)
            {
                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ReturnVarName = param.Name,
                    ReturnType = param.QualifiedType
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);

                returns.Add(marshal.Context.Return);
            }

            Write("{0}::raise(", @event.Name);
            Write("{0}", string.Join(", ", returns));
            WriteLine(");");

            UnindentAndWriteCloseBrace();
        }

        private void GenerateVariable(Variable variable, Class @class)
        {
            GeneratePropertyGetter(variable, @class, variable.Name, variable.Type);

            var arrayType = variable.Type as ArrayType;
            var qualifiedType = arrayType != null ? arrayType.QualifiedType : variable.QualifiedType;
            if (!qualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable, @class, variable.Name, variable.Type);
        }

        private void GenerateClassConstructor(Class @class, bool withOwnNativeInstanceParam = false)
        {
            string qualifiedIdentifier = QualifiedIdentifier(@class);

            Write("{0}::{1}(", qualifiedIdentifier, @class.Name);

            string nativeType = $"{typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*";
            WriteLine(!withOwnNativeInstanceParam ? "{0} native)" : "{0} native, bool ownNativeInstance)", nativeType);

            var hasBase = GenerateClassConstructorBase(@class, null, withOwnNativeInstanceParam);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                Indent();
                Write(hasBase ? "," : ":");
                Unindent();

                WriteLine(!withOwnNativeInstanceParam ? " {0}(false)" : " {0}(ownNativeInstance)", Helpers.OwnsNativeInstanceIdentifier);
            }

            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.ConstructorBody, @class);

            const string nativePtr = "native";

            if (@class.IsRefType)
            {
                if (!hasBase)
                {
                    WriteLine("NativePtr = {0};", nativePtr);
                }
            }
            else
            {
                GenerateStructMarshaling(@class, nativePtr + "->");
            }

            PopBlock();

            UnindentAndWriteCloseBrace();

            string createInstanceParams = withOwnNativeInstanceParam ? $"::System::IntPtr native, bool {Helpers.OwnsNativeInstanceIdentifier}" : "::System::IntPtr native";
            string createInstanceParamsValues = withOwnNativeInstanceParam ? $"({nativeType}) native.ToPointer(), {Helpers.OwnsNativeInstanceIdentifier}" : $"({nativeType}) native.ToPointer()";

            NewLine();
            WriteLine($"{qualifiedIdentifier}^ {qualifiedIdentifier}::{Helpers.CreateInstanceIdentifier}({createInstanceParams})");

            WriteOpenBraceAndIndent();

            WriteLine($"return gcnew ::{qualifiedIdentifier}({createInstanceParamsValues});");

            UnindentAndWriteCloseBrace();
            NewLine();
        }

        private void GenerateStructMarshaling(Class @class, string nativeVar)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
            {
                GenerateStructMarshaling(@base.Class, nativeVar);
            }

            int paramIndex = 0;
            foreach (var property in @class.Properties.Where(
                p => !ASTUtils.CheckIgnoreProperty(p) && !CLIHeaders.TypeIgnored(p.Type)))
            {
                if (property.Field == null)
                    continue;

                var nativeField = string.Format("{0}{1}",
                                                nativeVar, property.Field.OriginalName);

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = property.Name,
                    ReturnVarName = nativeField,
                    ReturnType = property.QualifiedType,
                    ParameterIndex = paramIndex++
                };
                ctx.PushMarshalKind(MarshalKind.NativeField);
                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                property.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine("{0} = {1};", property.Field.Name, marshal.Context.Return);
            }
        }

        private bool GenerateClassConstructorBase(Class @class, Method method = null,
            bool withOwnNativeInstanceParam = false)
        {
            if (!@class.NeedsBase)
                return false;

            if (@class.IsValueType)
                return true;

            Indent();

            Write($": {QualifiedIdentifier(@class.BaseClass)}(");

            // We cast the value to the base class type since otherwise there
            // could be ambiguous call to overloaded constructors.
            var cppTypePrinter = new CppTypePrinter(Context);
            var nativeTypeName = @class.BaseClass.Visit(cppTypePrinter);

            Write($"({nativeTypeName}*)");
            WriteLine("{0}{1})", method != null ? "nullptr" : "native",
                !withOwnNativeInstanceParam ? "" : ", ownNativeInstance");

            Unindent();

            return true;
        }

        public void GenerateMethod(Method method, Class @class)
        {
            if (CLIHeaders.FunctionIgnored(method))
                return;
            PushBlock(BlockKind.Method, method);

            if (method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                Write("{0}::{1}(", QualifiedIdentifier(@class), GetMethodName(method));
            else
                Write("{0} {1}::{2}(", method.ReturnType, QualifiedIdentifier(@class),
                      method.Name);

            GenerateMethodParameters(method);

            WriteLine(")");

            if (method.IsConstructor)
                GenerateClassConstructorBase(@class, method: method);

            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.MethodBody, method);

            if (method.IsConstructor && @class.IsRefType)
            {
                PushBlock(BlockKind.ConstructorBody, @class);

                WriteLine("{0} = true;", Helpers.OwnsNativeInstanceIdentifier);
            }

            if (method.IsProxy)
                goto SkipImpl;

            if (@class.IsRefType)
            {
                if (method.IsConstructor)
                {
                    if (!@class.IsAbstract)
                    {
                        var @params = GenerateFunctionParamsMarshal(method.Parameters, method);
                        Write($@"NativePtr = new {typePrinter.PrintTag(@class)}::{
                            @class.QualifiedOriginalName}(");
                        GenerateFunctionParams(@params);
                        WriteLine(");");
                    }

                    PopBlock();
                }
                else
                {
                    GenerateFunctionCall(method, @class);
                }
            }
            else if (@class.IsValueType)
            {
                if (!method.IsConstructor)
                    GenerateFunctionCall(method, @class);
                else
                    GenerateValueTypeConstructorCall(method, @class);
            }

        SkipImpl:

            PopBlock();

            UnindentAndWriteCloseBrace();

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                GenerateEquals(method, @class);
            }

            PopBlock(NewLineKind.Always);
        }

        private void GenerateEquals(Function method, Class @class)
        {
            Class leftHandSide;
            Class rightHandSide;
            if (method.Parameters[0].Type.SkipPointerRefs().TryGetClass(out leftHandSide) &&
                leftHandSide.OriginalPtr == @class.OriginalPtr &&
                method.Parameters[1].Type.SkipPointerRefs().TryGetClass(out rightHandSide) &&
                rightHandSide.OriginalPtr == @class.OriginalPtr)
            {
                NewLine();
                var qualifiedIdentifier = QualifiedIdentifier(@class);
                WriteLine("bool {0}::Equals(::System::Object^ obj)", qualifiedIdentifier);
                WriteOpenBraceAndIndent();
                if (@class.IsRefType)
                {
                    WriteLine("return this == safe_cast<{0}^>(obj);", qualifiedIdentifier);
                }
                else
                {
                    WriteLine("return *this == safe_cast<{0}>(obj);", qualifiedIdentifier);
                }
                UnindentAndWriteCloseBrace();
            }
        }

        private void GenerateValueTypeConstructorCall(Method method, Class @class)
        {
            var names = new List<string>();

            var paramIndex = 0;
            foreach (var param in method.Parameters)
            {
                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    Function = method,
                    Parameter = param,
                    ArgName = param.Name,
                    ParameterIndex = paramIndex++
                };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                names.Add(marshal.Context.Return);
            }

            WriteLine($@"{typePrinter.PrintTag(@class)}::{
                @class.QualifiedOriginalName} _native({string.Join(", ", names)});");

            GenerateValueTypeConstructorCallProperties(@class);
        }

        private void GenerateValueTypeConstructorCallProperties(Class @class)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
            {
                GenerateValueTypeConstructorCallProperties(@base.Class);
            }

            foreach (var property in @class.Properties)
            {
                if (!property.IsDeclared || property.Field == null) continue;

                var varName = $"_native.{property.Field.OriginalName}";

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ReturnVarName = varName,
                    ReturnType = property.QualifiedType
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                property.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine("this->{0} = {1};", property.Name, marshal.Context.Return);
            }
        }

        public void GenerateFunction(Function function, DeclarationContext @namespace)
        {
            if (!function.IsGenerated || CLIHeaders.FunctionIgnored(function))
                return;

            GenerateDeclarationCommon(function);

            var classSig = string.Format("{0}::{1}", QualifiedIdentifier(@namespace),
                Options.GenerateFreeStandingFunctionsClassName(TranslationUnit));

            Write("{0} {1}::{2}(", function.ReturnType, classSig,
                function.Name);

            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                Write("{0}", CTypePrinter.VisitParameter(param));
                if (i < function.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(")");
            WriteOpenBraceAndIndent();

            GenerateFunctionCall(function);

            UnindentAndWriteCloseBrace();
        }

        public void GenerateFunctionCall(Function function, Class @class = null, Type publicRetType = null)
        {
            CheckArgumentRange(function);

            if (function.OperatorKind == CXXOperatorKind.EqualEqual ||
                function.OperatorKind == CXXOperatorKind.ExclaimEqual)
            {
                WriteLine("bool {0}Null = ReferenceEquals({0}, nullptr);",
                    function.Parameters[0].Name);
                WriteLine("bool {0}Null = ReferenceEquals({0}, nullptr);",
                    function.Parameters[1].Name);
                WriteLine("if ({0}Null || {1}Null)",
                    function.Parameters[0].Name, function.Parameters[1].Name);
                WriteLineIndent("return {0}{1}Null && {2}Null{3};",
                    function.OperatorKind == CXXOperatorKind.EqualEqual ? string.Empty : "!(",
                    function.Parameters[0].Name, function.Parameters[1].Name,
                    function.OperatorKind == CXXOperatorKind.EqualEqual ? string.Empty : ")");
            }

            var retType = function.ReturnType;
            if (publicRetType == null)
                publicRetType = retType.Type;
            var needsReturn = !publicRetType.IsPrimitiveType(PrimitiveType.Void);

            const string valueMarshalName = "_this0";
            var isValueType = @class != null && @class.IsValueType;
            if (isValueType && !IsNativeFunctionOrStaticMethod(function))
            {
                WriteLine($"auto {valueMarshalName} = {typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}();");

                var param = new Parameter { Name = "(*this)", Namespace = function.Namespace };
                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    Parameter = param
                };
                ctx.VarPrefix.Write(valueMarshalName);

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                marshal.MarshalValueClassProperties(@class, valueMarshalName);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);
            }

            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);

            var returnIdentifier = Helpers.ReturnIdentifier;
            if (needsReturn)
                if (retType.Type.IsReference())
                    Write("auto &{0} = ", returnIdentifier);
                else
                    Write("auto {0} = ", returnIdentifier);

            if (function.OperatorKind == CXXOperatorKind.Conversion ||
                function.OperatorKind == CXXOperatorKind.ExplicitConversion)
            {
                var method = function as Method;
                var typeName = method.ConversionType.Visit(new CppTypePrinter(Context));
                WriteLine("({0}) {1};", typeName, @params[0].Name);
            }
            else if (function.IsOperator &&
                     function.OperatorKind != CXXOperatorKind.Subscript)
            {
                var opName = function.Name.Replace("operator", "").Trim();

                switch (Operators.ClassifyOperator(function))
                {
                    case CXXOperatorArity.Unary:
                        WriteLine("{0} {1};", opName, @params[0].Name);
                        break;
                    case CXXOperatorArity.Binary:
                        WriteLine("{0} {1} {2};", @params[0].Name, opName, @params[1].Name);
                        break;
                }
            }
            else
            {
                if (IsNativeFunctionOrStaticMethod(function))
                {
                    Write($"::{function.QualifiedOriginalName}(");
                }
                else
                {
                    if (isValueType)
                        Write($"{valueMarshalName}.");
                    else if (function.IsNativeMethod())
                        Write($"(({typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*)NativePtr)->");
                    Write("{0}(", function.OriginalName);
                }

                GenerateFunctionParams(@params);
                WriteLine(");");
            }

            foreach (var paramInfo in @params)
            {
                var param = paramInfo.Param;
                if (param.Usage != ParameterUsage.Out && param.Usage != ParameterUsage.InOut)
                    continue;

                if (param.Type.IsPointer() && !param.Type.GetFinalPointee().IsPrimitiveType())
                    param.QualifiedType = new QualifiedType(param.Type.GetFinalPointee());

                var nativeVarName = paramInfo.Name;

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = nativeVarName,
                    ReturnVarName = nativeVarName,
                    ReturnType = param.QualifiedType
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine("{0} = {1};", param.Name, marshal.Context.Return);
            }

            if (isValueType && !IsNativeFunctionOrStaticMethod(function))
            {
                GenerateStructMarshaling(@class, valueMarshalName + ".");
            }

            if (needsReturn)
            {
                var retTypeName = retType.Visit(CTypePrinter).ToString();
                var isIntPtr = retTypeName.Contains("IntPtr");

                if (retType.Type.IsPointer() && (isIntPtr || retTypeName.EndsWith("^", StringComparison.Ordinal)))
                {
                    WriteLine("if ({0} == nullptr) return {1};",
                        returnIdentifier,
                        isIntPtr ? "::System::IntPtr()" : "nullptr");
                }

                var ctx = new MarshalContext(Context, CurrentIndentation)
                {
                    ArgName = returnIdentifier,
                    ReturnVarName = returnIdentifier,
                    ReturnType = retType
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                retType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                // Special case for indexer - needs to dereference if the internal
                // function is a pointer type and the property is not.
                if (retType.Type.IsPointer() &&
                    retType.Type.GetPointee().Equals(publicRetType) &&
                    publicRetType.IsPrimitiveType())
                    WriteLine("return *({0});", marshal.Context.Return);
                else if (retType.Type.IsReference() && publicRetType.IsReference())
                    WriteLine("return ({0})({1});", publicRetType, marshal.Context.Return);
                else
                    WriteLine("return {0};", marshal.Context.Return);
            }
        }

        private void CheckArgumentRange(Function method)
        {
            if (Context.Options.MarshalCharAsManagedChar)
            {
                foreach (var param in method.Parameters.Where(
                    p => p.Type.IsPrimitiveType(PrimitiveType.Char)))
                {
                    WriteLine("if ({0} < ::System::Char::MinValue || {0} > ::System::SByte::MaxValue)", param.Name);
                    // C++/CLI can actually handle char -> sbyte in all case, this is for compatibility with the C# generator
                    WriteLineIndent(
                        "throw gcnew ::System::OverflowException(\"{0} must be in the range {1} - {2}.\");",
                        param.Name, (int)char.MinValue, sbyte.MaxValue);
                }
            }
        }

        private static bool IsNativeFunctionOrStaticMethod(Function function)
        {
            var method = function as Method;
            if (method == null)
                return true;

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

        private ParamMarshal GenerateFunctionParamMarshal(Parameter param, int paramIndex,
            Function function = null)
        {
            var paramMarshal = new ParamMarshal { Name = param.Name, Param = param };

            if (param.Type is BuiltinType)
                return paramMarshal;

            var argName = Generator.GeneratedIdentifier("arg") + paramIndex.ToString(CultureInfo.InvariantCulture);

            var isRef = param.IsOut || param.IsInOut;

            var paramType = param.Type;

            // Get actual type if the param type is a typedef but not a function type because function types have to be typedef.
            // We need to get the actual type this early before we visit any marshalling code to ensure we hit the marshalling
            // logic for the actual type and not the typedef.
            // This fixes issues where typedefs to primitive pointers are involved.
            FunctionType functionType;
            var paramTypeAsTypedef = paramType as TypedefType;
            if (paramTypeAsTypedef != null && !paramTypeAsTypedef.Declaration.Type.IsPointerTo(out functionType))
            {
                paramType = param.Type.Desugar();
            }

            // Since both pointers and references to types are wrapped as CLI
            // tracking references when using in/out, we normalize them here to be able
            // to use the same code for marshaling.            
            if (paramType is PointerType && isRef)
            {
                if (!paramType.IsReference())
                    paramMarshal.Prefix = "&";
                paramType = (paramType as PointerType).Pointee;
            }

            var effectiveParam = new Parameter(param)
            {
                QualifiedType = new QualifiedType(paramType)
            };

            var ctx = new MarshalContext(Context, CurrentIndentation)
            {
                Parameter = effectiveParam,
                ParameterIndex = paramIndex,
                ArgName = argName,
                Function = function
            };

            var marshal = new CLIMarshalManagedToNativePrinter(ctx);
            effectiveParam.Visit(marshal);

            if (string.IsNullOrEmpty(marshal.Context.Return))
                throw new Exception($"Cannot marshal argument of function '{function.QualifiedOriginalName}'");

            if (isRef)
            {
                var typePrinter = new CppTypePrinter(Context) { ResolveTypeMaps = false };
                var type = paramType.Visit(typePrinter);

                if (param.IsInOut)
                {
                    if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                        Write(marshal.Context.Before);

                    WriteLine("{0} {1} = {2};", type, argName, marshal.Context.Return);
                }
                else
                    WriteLine("{0} {1};", type, argName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                WriteLine("auto {0}{1} = {2};", marshal.VarPrefix, argName,
                    marshal.Context.Return);
                paramMarshal.Prefix = marshal.ArgumentPrefix;
            }

            paramMarshal.Name = argName;
            return paramMarshal;
        }

        public void GenerateFunctionParams(List<ParamMarshal> @params)
        {
            var names = @params.Select(param =>
                {
                    if (!string.IsNullOrWhiteSpace(param.Prefix))
                        return param.Prefix + param.Name;
                    return param.Name;
                }).ToList();
            Write(string.Join(", ", names));
        }
    }
}
