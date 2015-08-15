using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI source files.
    /// </summary>
    public class CLISourcesTemplate : CLITextTemplate
    {
        public CLISourcesTemplate(Driver driver, IEnumerable<TranslationUnit> units)
            : base(driver, units)
        {
            
        }

        public override void Process()
        {
            PushBlock(BlockKind.Header);
            PopBlock();

            var file = Path.GetFileNameWithoutExtension(TranslationUnit.FileName)
                .Replace('\\', '/');

            if (Driver.Options.GenerateName != null)
                file = Driver.Options.GenerateName(TranslationUnit);

            PushBlock(CLIBlockKind.Includes);
            WriteLine("#include \"{0}.h\"", file);
            GenerateForwardReferenceHeaders();

            NewLine();
            PopBlock();

            PushBlock(CLIBlockKind.Usings);
            WriteLine("using namespace System;");
            WriteLine("using namespace System::Runtime::InteropServices;");
            foreach (var customUsingStatement in Options.DependentNameSpaces)
            {
                WriteLine(string.Format("using namespace {0};", customUsingStatement)); ;
            }
            NewLine();
            PopBlock();

            GenerateDeclContext(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateForwardReferenceHeaders()
        {
            PushBlock(CLIBlockKind.IncludesForwardReferences);

            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase, Driver.Options);
            typeReferenceCollector.Process(TranslationUnit, filterNamespaces: false);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                if (typeRef.Include.File == TranslationUnit.FileName)
                    continue;

                var include = typeRef.Include;
                if(!string.IsNullOrEmpty(include.File) && !include.InHeader)
                    includes.Add(include.ToString());
            }

            foreach (var include in includes)
                WriteLine(include);

            PopBlock();
        }

        private void GenerateDeclContext(DeclarationContext @namespace)
        {
            PushBlock(CLIBlockKind.Namespace);
            foreach (var @class in @namespace.Classes)
            {
                if (!@class.IsGenerated)
                    continue;

                if (@class.IsOpaque || @class.IsIncomplete)
                    continue;

                GenerateClass(@class);
            }

            if (@namespace.HasFunctions)
            {
                // Generate all the function declarations for the module.
                foreach (var function in @namespace.Functions)
                {
                    if (!function.IsGenerated)
                        continue;

                    GenerateFunction(function, @namespace);
                    NewLine();
                }
            }

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

            foreach(var childNamespace in @namespace.Namespaces)
                GenerateDeclContext(childNamespace);

            PopBlock();
        }

        public void GenerateClass(Class @class)
        {
            PushBlock(CLIBlockKind.Class);

            GenerateDeclContext(@class);

            GenerateClassConstructors(@class);

            GenerateClassMethods(@class, @class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                var qualifiedIdentifier = QualifiedIdentifier(@class);

                PushBlock(CLIBlockKind.Method);
                WriteLine("System::IntPtr {0}::{1}::get()",
                    qualifiedIdentifier, Helpers.InstanceIdentifier);
                WriteStartBraceIndent();
                WriteLine("return System::IntPtr(NativePtr);");
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);

                PushBlock(CLIBlockKind.Method);
                WriteLine("void {0}::{1}::set(System::IntPtr object)",
                    qualifiedIdentifier, Helpers.InstanceIdentifier);
                WriteStartBraceIndent();
                var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);
                WriteLine("NativePtr = ({0})object.ToPointer();", nativeType);
                WriteCloseBraceIndent();
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

            // Output a default constructor that takes the native pointer.
            GenerateClassConstructor(@class);

            if (@class.IsRefType)
            {
                var destructor = @class.Destructors
                    .FirstOrDefault(d => d.Parameters.Count == 0 && d.Access == AccessSpecifier.Public);
                if (destructor != null)
                {
                    GenerateClassDestructor(@class);
                    if (Options.GenerateFinalizers)
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
                if (ASTUtils.CheckIgnoreMethod(method, Options))
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
                p => !ASTUtils.CheckIgnoreProperty(p) && !p.IsInRefTypeAndBackedByValueClassField()))
                GenerateProperty(property, realOwner);
        }

        private void GenerateClassDestructor(Class @class)
        {
            PushBlock(CLIBlockKind.Destructor);

            WriteLine("{0}::~{1}()", QualifiedIdentifier(@class), @class.Name);
            WriteStartBraceIndent();

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                WriteLine("if ({0})", Helpers.OwnsNativeInstanceIdentifier);
                WriteLineIndent("delete NativePtr;");
            }

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassFinalizer(Class @class)
        {
            PushBlock(CLIBlockKind.Finalizer);

            WriteLine("{0}::!{1}()", QualifiedIdentifier(@class), @class.Name);
            WriteStartBraceIndent();

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
                WriteLine("delete NativePtr;");

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateFunctionTemplate(FunctionTemplate template)
        {
            var printer = TypePrinter;
            var oldCtx = printer.Context;

            PushBlock(CLIBlockKind.Template);

            var function = template.TemplatedFunction;

            var typeCtx = new CLITypePrinterContext()
                {
                    Kind = TypePrinterContextKind.Template,
                    Declaration = template
                };

            printer.Context = typeCtx;

            var typePrinter = new CLITypePrinter(Driver, typeCtx);
            var retType = function.ReturnType.Type.Visit(typePrinter,
                function.ReturnType.Qualifiers);

            var typeNames = "";
            var paramNames = template.Parameters.Select(param => param.Name).ToList();
            if (paramNames.Any())
                typeNames = "typename " + string.Join(", typename ", paramNames);

            WriteLine("generic<{0}>", typeNames);
            WriteLine("{0} {1}::{2}({3})", retType, 
                QualifiedIdentifier(function.Namespace), function.Name,
                GenerateParametersList(function.Parameters));

            WriteStartBraceIndent();

            var @class = function.Namespace as Class;
            GenerateFunctionCall(function, @class);

            WriteCloseBraceIndent();
            NewLine();

            PopBlock(NewLineKind.BeforeNextBlock);

            printer.Context = oldCtx;
        }

        private void GenerateProperty(Property property, Class realOwner)
        {
            PushBlock(CLIBlockKind.Property);

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
                args.Add(string.Format("{0} {1}", indexParameter.Type, indexParameter.Name));

            var function = decl as Function;
            var argName = function != null && !isIndexer ? function.Parameters[0].Name : "value";
            args.Add(string.Format("{0} {1}", type, argName));

            WriteLine("void {0}::{1}::set({2})", QualifiedIdentifier(@class),
                name, string.Join(", ", args));

            WriteStartBraceIndent();

            if (decl is Function && !isIndexer)
            {
                var func = decl as Function;
                GenerateFunctionCall(func, @class);
            }
            else
            {
                if (@class.IsValueType && decl is Field)
                {
                    WriteLine("{0} = value;", decl.Name);
                    WriteCloseBraceIndent();
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
                    variable = string.Format("::{0}::{1}",
                                             @class.QualifiedOriginalName, decl.OriginalName);
                else
                    variable = string.Format("((::{0}*)NativePtr)->{1}",
                                             @class.QualifiedOriginalName, decl.OriginalName);

                var ctx = new MarshalContext(Driver)
                {
                    Parameter = param,
                    ArgName = param.Name,
                    ReturnVarName = variable
                };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (isIndexer)
                {
                    var ctx2 = new MarshalContext(Driver)
                    {
                        Parameter = indexParameter,
                        ArgName = indexParameter.Name
                    };

                    var marshal2 = new CLIMarshalManagedToNativePrinter(ctx2);
                    indexParameter.Visit(marshal2);

                    variable += string.Format("({0})", marshal2.Context.Return);
                }

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (marshal.Context.Return.StringBuilder.Length > 0)
                {
                    if (isIndexer && decl.Type.IsPointer())
                        WriteLine("*({0}) = {1};", variable, marshal.Context.Return);
                    else
                        WriteLine("{0} = {1};", variable, marshal.Context.Return);
                }
            }

            WriteCloseBraceIndent();
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

            WriteStartBraceIndent();

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
                    WriteLine("return {0};", decl.Name);
                    WriteCloseBraceIndent();
                    NewLine();
                    return;
                }
                string variable;
                if (decl is Variable)
                    variable = string.Format("::{0}::{1}",
                                         @class.QualifiedOriginalName, decl.OriginalName);
                else
                    variable = string.Format("((::{0}*)NativePtr)->{1}",
                                             @class.QualifiedOriginalName, decl.OriginalName);

                var ctx = new MarshalContext(Driver)
                    {
                        Declaration = decl,
                        ArgName = decl.Name,
                        ReturnVarName = variable,
                        ReturnType = decl.QualifiedType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                decl.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }
            

            WriteCloseBraceIndent();
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
            WriteStartBraceIndent();

            var delegateName = string.Format("_{0}Delegate", @event.Name);

            WriteLine("if (!{0}Instance)", delegateName);
            WriteStartBraceIndent();

            var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
            var args = typePrinter.VisitParameters(@event.Parameters, hasNames: false);

            WriteLine("{0}Instance = gcnew {0}(this, &{1}::_{2}Raise);",
                      delegateName, QualifiedIdentifier(@class), @event.Name);

            WriteLine("auto _fptr = (void (*)({0}))Marshal::GetFunctionPointerForDelegate({1}Instance).ToPointer();",
                args, delegateName);

            WriteLine("((::{0}*)NativePtr)->{1}.Connect(_fptr);", @class.QualifiedOriginalName,
                @event.OriginalName);

            WriteCloseBraceIndent();

            WriteLine("_{0} = static_cast<{1}>(System::Delegate::Combine(_{0}, evt));",
                @event.Name, @event.Type);

            WriteCloseBraceIndent();
        }

        private void GenerateEventRemove(Event @event, Class @class)
        {
            WriteLine("void {0}::{1}::remove({2} evt)", QualifiedIdentifier(@class),
                      @event.Name, @event.Type);
            WriteStartBraceIndent();

            WriteLine("_{0} = static_cast<{1}>(System::Delegate::Remove(_{0}, evt));",
                @event.Name, @event.Type);

            WriteCloseBraceIndent();
        }

        private void GenerateEventRaise(Event @event, Class @class)
        {
            var typePrinter = new CLITypePrinter(Driver);
            var args = typePrinter.VisitParameters(@event.Parameters, hasNames: true);

            WriteLine("void {0}::{1}::raise({2})", QualifiedIdentifier(@class),
                      @event.Name, args);

            WriteStartBraceIndent();

            var paramNames = @event.Parameters.Select(param => param.Name).ToList();
            WriteLine("_{0}({1});", @event.Name, string.Join(", ", paramNames));

            WriteCloseBraceIndent();
        }

        private void GenerateEventRaiseWrapper(Event @event, Class @class)
        {
            var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
            var args = typePrinter.VisitParameters(@event.Parameters, hasNames: true);

            WriteLine("void {0}::_{1}Raise({2})", QualifiedIdentifier(@class),
                @event.Name, args);

            WriteStartBraceIndent();

            var returns = new List<string>();
            foreach (var param in @event.Parameters)
            {
                var ctx = new MarshalContext(Driver)
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

            WriteCloseBraceIndent();
        }

        private void GenerateVariable(Variable variable, Class @class)
        {
            GeneratePropertyGetter(variable, @class, variable.Name, variable.Type);

            if (!variable.QualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable, @class, variable.Name, variable.Type);
        }

        private void GenerateClassConstructor(Class @class)
        {
            string qualifiedIdentifier = QualifiedIdentifier(@class);

            Write("{0}::{1}(", qualifiedIdentifier, @class.Name);

            var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);
            WriteLine("{0} native)", nativeType);

            var hasBase = GenerateClassConstructorBase(@class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                PushIndent();
                Write(hasBase ? "," : ":");
                PopIndent();

                WriteLine(" {0}(false)", Helpers.OwnsNativeInstanceIdentifier);
            }

            WriteStartBraceIndent();

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

            WriteCloseBraceIndent();
            NewLine();
            WriteLine("{0}^ {0}::{1}(::System::IntPtr native)", qualifiedIdentifier, Helpers.CreateInstanceIdentifier);

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                WriteLine("return ::{0}::{1}(native, false);",
                    qualifiedIdentifier, Helpers.CreateInstanceIdentifier);
                WriteCloseBraceIndent();
                NewLine();

                WriteLine("{0}^ {0}::{1}(::System::IntPtr native, bool {2})",
                    qualifiedIdentifier, Helpers.CreateInstanceIdentifier, Helpers.OwnsNativeInstanceIdentifier);

                WriteStartBraceIndent();
                WriteLine("::{0}^ result = gcnew ::{0}(({1}) native.ToPointer());", qualifiedIdentifier, nativeType);
                if (@class.IsRefType)
                    WriteLine("result->{0} = {0};", Helpers.OwnsNativeInstanceIdentifier);
                WriteLine("return result;");
            }
            else
            {
                WriteLine("return gcnew ::{0}(({1}) native.ToPointer());", qualifiedIdentifier, nativeType);
            }

            WriteCloseBraceIndent();
            NewLine();
        }

        private void GenerateStructMarshaling(Class @class, string nativeVar)
        {
            foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
            {
                GenerateStructMarshaling(@base.Class, nativeVar);
            }

            int paramIndex = 0;
            foreach (var property in @class.Properties.Where( p => !ASTUtils.CheckIgnoreProperty(p)))
            {
                if (property.Field == null)
                    continue;

                var nativeField = string.Format("{0}{1}",
                                                nativeVar, property.Field.OriginalName);

                var ctx = new MarshalContext(Driver)
                {
                    ArgName = property.Name,
                    ReturnVarName = nativeField,
                    ReturnType = property.QualifiedType,
                    Declaration = property.Field,
                    ParameterIndex = paramIndex++
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                property.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};", property.Field.Name, marshal.Context.Return);
            }
        }

        private bool GenerateClassConstructorBase(Class @class, Method method = null)
        {
            var hasBase = @class.HasBase && @class.Bases[0].IsClass && @class.Bases[0].Class.IsDeclared;
            if (!hasBase)
                return false;

            if (!@class.IsValueType)
            {
                PushIndent();

                var baseClass = @class.Bases[0].Class;
                Write(": {0}(", QualifiedIdentifier(baseClass));

                // We cast the value to the base clas type since otherwise there
                // could be ambiguous call to overloaded constructors.
                var cppTypePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var nativeTypeName = baseClass.Visit(cppTypePrinter);
                Write("({0}*)", nativeTypeName);

                WriteLine("{0})", method != null ? "nullptr" : "native");

                PopIndent();
            }

            return true;
        }

        public void GenerateMethod(Method method, Class @class)
        {
            PushBlock(CLIBlockKind.Method, method);

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

            WriteStartBraceIndent();

            PushBlock(CLIBlockKind.MethodBody, method);

            if (method.IsConstructor && @class.IsRefType)
                WriteLine("{0} = true;", Helpers.OwnsNativeInstanceIdentifier);

            if (method.IsProxy)
                goto SkipImpl;

            if (@class.IsRefType)
            {
                if (method.IsConstructor)
                {
                    if (!@class.IsAbstract)
                    {
                        var @params = GenerateFunctionParamsMarshal(method.Parameters, method);
                        Write("NativePtr = new ::{0}(", method.Namespace.QualifiedOriginalName);
                        GenerateFunctionParams(@params);
                        WriteLine(");");
                    }
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

            WriteCloseBraceIndent();

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
                WriteStartBraceIndent();
                if (@class.IsRefType)
                {
                    WriteLine("return this == safe_cast<{0}^>(obj);", qualifiedIdentifier);
                }
                else
                {
                    WriteLine("return *this == safe_cast<{0}>(obj);", qualifiedIdentifier);
                }
                WriteCloseBraceIndent();
            }
        }

        private void GenerateValueTypeConstructorCall(Method method, Class @class)
        {
            var names = new List<string>();

            var paramIndex = 0;
            foreach (var param in method.Parameters)
            {
                var ctx = new MarshalContext(Driver)
                              {
                                  Function = method,
                                  Parameter = param,
                                  ArgName = param.Name,
                                  ParameterIndex = paramIndex++
                              };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                names.Add(marshal.Context.Return);
            }

            WriteLine("::{0} _native({1});", @class.QualifiedOriginalName,
                      string.Join(", ", names));

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

                var varName = string.Format("_native.{0}", property.Field.OriginalName);

                var ctx = new MarshalContext(Driver)
                    {
                        ReturnVarName = varName,
                        ReturnType = property.QualifiedType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                property.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("this->{0} = {1};", property.Name, marshal.Context.Return);
            }
        }

        public void GenerateFunction(Function function, DeclarationContext @namespace)
        {
            if (!function.IsGenerated)
                return;

            GenerateDeclarationCommon(function);

            var classSig = string.Format("{0}::{1}", QualifiedIdentifier(@namespace),
                TranslationUnit.FileNameWithoutExtension);

            Write("{0} {1}::{2}(", function.ReturnType, classSig,
                function.Name);

            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                Write("{0}", TypePrinter.VisitParameter(param));
                if (i < function.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(")");
            WriteStartBraceIndent();

            GenerateFunctionCall(function);

            WriteCloseBraceIndent();
        }

        public void GenerateFunctionCall(Function function, Class @class = null, Type publicRetType = null)
        {
            CheckArgumentRange(function);

            var retType = function.ReturnType;
            if (publicRetType == null)
                publicRetType = retType.Type;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            const string valueMarshalName = "_this0";
            var isValueType = @class != null && @class.IsValueType;
            if (isValueType && !IsNativeFunctionOrStaticMethod(function))
            {
                WriteLine("auto {0} = ::{1}();", valueMarshalName, @class.QualifiedOriginalName);

                var param = new Parameter { Name = "(*this)" };
                var ctx = new MarshalContext(Driver)
                    {
                        MarshalVarPrefix = valueMarshalName,
                        Parameter = param
                    };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                marshal.MarshalValueClassProperties(@class, valueMarshalName);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);
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
                var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var typeName = method.ConversionType.Visit(typePrinter);
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
                    Write("::{0}(", function.QualifiedOriginalName);
                }
                else
                {
                    if (isValueType)
                        Write("{0}.", valueMarshalName);
                    else if (IsNativeMethod(function))
                        Write("((::{0}*)NativePtr)->", @class.QualifiedOriginalName);
                    Write("{0}(", function.OriginalName);
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

                var nativeVarName = paramInfo.Name;

                var ctx = new MarshalContext(Driver)
                    {
                        ArgName = nativeVarName,
                        ReturnVarName = nativeVarName,
                        ReturnType = param.QualifiedType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);
                
                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};",param.Name,marshal.Context.Return);
            }

            if (isValueType && !IsNativeFunctionOrStaticMethod(function))
            {
                GenerateStructMarshaling(@class, valueMarshalName + ".");
            }

            if (needsReturn)
            {
                var retTypeName = retType.Visit(TypePrinter);
                var isIntPtr = retTypeName.Contains("IntPtr");

                if (retType.Type.IsPointer() && (isIntPtr || retTypeName.EndsWith("^")))
                {
                    WriteLine("if ({0} == nullptr) return {1};",
                        returnIdentifier,
                        isIntPtr ? "System::IntPtr()" : "nullptr");
                }

                var ctx = new MarshalContext(Driver)
                {
                    ArgName = returnIdentifier,
                    ReturnVarName = returnIdentifier,
                    ReturnType = retType
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                retType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

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
            if (Driver.Options.MarshalCharAsManagedChar)
            {
                foreach (var param in method.Parameters.Where(
                    p => p.Type.IsPrimitiveType(PrimitiveType.Char)))
                {
                    WriteLine("if ({0} < System::Char::MinValue || {0} > System::SByte::MaxValue)", param.Name);
                    WriteLineIndent(
                        "throw gcnew System::ArgumentException(\"{0} must be in the range {1} - {2}.\");",
                        param.Name, (int) char.MinValue, sbyte.MaxValue);
                }
            }
        }

        private static bool IsNativeMethod(Function function)
        {
            var method = function as Method;
            if (method == null) 
                return false;

            return method.Conversion == MethodConversionKind.None;
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

            var argName = "arg" + paramIndex.ToString(CultureInfo.InvariantCulture);

            var isRef = param.IsOut || param.IsInOut;
            // Since both pointers and references to types are wrapped as CLI
            // tracking references when using in/out, we normalize them here to be able
            // to use the same code for marshaling.
            var paramType = param.Type;
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

            var ctx = new MarshalContext(Driver)
            {
                Parameter = effectiveParam,
                ParameterIndex = paramIndex,
                ArgName = argName,
                Function = function
            };

            var marshal = new CLIMarshalManagedToNativePrinter(ctx);
            effectiveParam.Visit(marshal);

            if (string.IsNullOrEmpty(marshal.Context.Return))
                throw new Exception(string.Format("Cannot marshal argument of function '{0}'",
                    function.QualifiedOriginalName));

            if (isRef)
            {
                var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var type = paramType.Visit(typePrinter);

                if (param.IsInOut)
                {
                    if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                        Write(marshal.Context.SupportBefore);

                    WriteLine("{0} {1} = {2};", type, argName, marshal.Context.Return);
                }
                else
                    WriteLine("{0} {1};", type, argName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

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

        public override string FileExtension { get { return "cpp"; } }
    }
}
