using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI source files.
    /// </summary>
    public class CLISourcesTemplate : CLITextTemplate
    {
        public CLISourcesTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
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
            NewLine();
            PopBlock();

            GenerateDeclContext(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateForwardReferenceHeaders()
        {
            PushBlock(CLIBlockKind.IncludesForwardReferences);

            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase);
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
                if (@class.Ignore)
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
                    if (function.Ignore)
                        continue;

                    GenerateFunction(function, @namespace);
                    NewLine();
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

            // Output a default constructor that takes the native pointer.
            GenerateClassConstructor(@class, isIntPtr: false);
            GenerateClassConstructor(@class, isIntPtr: true);

            foreach (var method in @class.Methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method))
                    continue;

                GenerateMethod(method, @class);
            }

            if (@class.IsRefType)
            {
                if (!CLIHeadersTemplate.HasRefBase(@class))
                {
                    PushBlock(CLIBlockKind.Method);
                    WriteLine("System::IntPtr {0}::Instance::get()",
                              QualifiedIdentifier(@class));
                    WriteStartBraceIndent();
                    WriteLine("return System::IntPtr(NativePtr);");
                    WriteCloseBraceIndent();
                    PopBlock(NewLineKind.BeforeNextBlock);

                    PushBlock(CLIBlockKind.Method);
                    WriteLine("void {0}::Instance::set(System::IntPtr object)",
                              QualifiedIdentifier(@class));
                    WriteStartBraceIndent();
                    var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);
                    WriteLine("NativePtr = ({0})object.ToPointer();", nativeType);
                    WriteCloseBraceIndent();
                    PopBlock(NewLineKind.BeforeNextBlock);
                }
            }

            foreach (var property in @class.Properties)
                GenerateProperty(property);

            foreach (var @event in @class.Events)
            {
                if (@event.Ignore)
                    continue;

                GenerateDeclarationCommon(@event);
                GenerateEvent(@event, @class);
            }

            if (Options.GenerateFunctionTemplates)
            {
                foreach (var template in @class.Templates)
                {
                    if (template.Ignore) continue;

                    var functionTemplate = template as FunctionTemplate;
                    if (functionTemplate == null) continue;

                    GenerateDeclarationCommon(template);
                    GenerateFunctionTemplate(functionTemplate, @class);
                }
            }

            foreach (var variable in @class.Variables)
            {
                if (variable.Ignore)
                    continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                GenerateDeclarationCommon(variable);
                GenerateVariable(variable, @class);
            }

            PopBlock();
        }

        private void GenerateFunctionTemplate(FunctionTemplate template, Class @class)
        {
            var printer = TypePrinter as CLITypePrinter;
            var oldCtx = printer.Context;

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

            var typeNamesStr = "";
            var paramNames = template.Parameters.Select(param => param.Name).ToList();
            if (paramNames.Any())
                typeNamesStr = "typename " + string.Join(", typename ", paramNames);

            WriteLine("generic<{0}>", typeNamesStr);
            WriteLine("{0} {1}::{2}({3})", retType, 
                QualifiedIdentifier(@class), SafeIdentifier(function.Name),
                GenerateParametersList(function.Parameters));

            WriteStartBraceIndent();

            GenerateFunctionCall(function, @class);

            WriteCloseBraceIndent();
            NewLine();

            printer.Context = oldCtx;
        }

        private void GenerateProperty(Property property)
        {
            if (property.Ignore) return;

            PushBlock(CLIBlockKind.Property);
            var @class = property.Namespace as Class;

            if (property.Field != null)
            {
                if (property.HasGetter)
                    GeneratePropertyGetter(property.Field, @class, property.Name,
                        property.Type);

                if (property.HasSetter)
                    GeneratePropertySetter(property.Field, @class, property.Name,
                        property.Type);
            }
            else
            {
                if (property.HasGetter)
                    GeneratePropertyGetter(property.GetMethod, @class, property.Name,
                        property.Type);

                if (property.HasSetter)
                    GeneratePropertySetter(property.SetMethod, @class, property.Name,
                        property.Type);
            }
            PopBlock(); 
        }

        private void GeneratePropertySetter<T>(T decl, Class @class, string name, Type type)
            where T : Declaration, ITypedDecl
        {
            if (decl == null)
                return;

            WriteLine("void {0}::{1}::set({2} value)", QualifiedIdentifier(@class),
                      name, type);
            WriteStartBraceIndent();

            if (decl is Function)
            {
                var func = decl as Function;
                if(func.Parameters[0].Name != "value")
                    WriteLine("auto {0} = value;", func.Parameters[0].Name);
                GenerateFunctionCall(func, @class);
            }
            else
            {
                var param = new Parameter
                                {
                                    Name = "value",
                                    QualifiedType = decl.QualifiedType
                                };

                var ctx = new MarshalContext(Driver)
                              {
                                  Parameter = param,
                                  ArgName = param.Name,
                              };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                string variable;
                if (decl is Variable)
                    variable = string.Format("::{0}::{1}",
                                             @class.QualifiedOriginalName, decl.OriginalName);
                else
                    variable = string.Format("((::{0}*)NativePtr)->{1}",
                                             @class.QualifiedOriginalName, decl.OriginalName);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};", variable, marshal.Context.Return);
            }

            WriteCloseBraceIndent();
            NewLine();
        }

        private void GeneratePropertyGetter<T>(T decl, Class @class, string name, Type type)
            where T : Declaration, ITypedDecl
        {
            if (decl == null)
                return;

            WriteLine("{0} {1}::{2}::get()", type, QualifiedIdentifier(@class),
                      name);
            WriteStartBraceIndent();

            if (decl is Function)
            {
                var func = decl as Function;
                GenerateFunctionCall(func, @class);
            }
            else
            {
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

        private void GenerateClassConstructor(Class @class, bool isIntPtr)
        {
            Write("{0}::{1}(", QualifiedIdentifier(@class), SafeIdentifier(@class.Name));

            var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);
            WriteLine("{0} native)", isIntPtr ? "System::IntPtr" : nativeType);

            var hasBase = GenerateClassConstructorBase(@class, isIntPtr);

            WriteStartBraceIndent();

            var nativePtr = "native";

            if (isIntPtr)
            {
                WriteLine("auto __native = (::{0}*)native.ToPointer();",
                    @class.QualifiedOriginalName);
                nativePtr = "__native";
            }

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
        }

        private void GenerateStructMarshaling(Class @class, string nativeVar)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.Ignore) 
                    continue;

                var baseClass = @base.Class;
                GenerateStructMarshaling(baseClass, nativeVar);
            }

            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field)) continue;

                var nativeField = string.Format("{0}{1}",
                                                nativeVar, field.OriginalName);

                var ctx = new MarshalContext(Driver)
                {
                    ArgName = field.Name,
                    ReturnVarName = nativeField,
                    ReturnType = field.QualifiedType
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                field.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};", field.Name, marshal.Context.Return);
            }
        }

        private bool GenerateClassConstructorBase(Class @class, bool isIntPtr, Method method = null)
        {
            var hasBase = @class.HasBase && @class.Bases[0].IsClass && !@class.Bases[0].Class.Ignore;
            if (!hasBase)
                return false;

            if (!@class.IsValueType)
            {
                PushIndent();

                var baseClass = @class.Bases[0].Class;
                Write(": {0}(", QualifiedIdentifier(baseClass));

                // We cast the value to the base clas type since otherwise there
                // could be ambiguous call to overloaded constructors.
                if (!isIntPtr)
                {
                    var cppTypePrinter = new CppTypePrinter(Driver.TypeDatabase);
                    var nativeTypeName = baseClass.Visit(cppTypePrinter);
                    Write("({0}*)", nativeTypeName);
                }

                WriteLine("{0})", method != null ? "nullptr" : "native");

                PopIndent();
            }

            return true;
        }

        public void GenerateMethod(Method method, Class @class)
        {
            PushBlock(CLIBlockKind.Method, method);

            if (method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion)
                Write("{0}::{1}(", QualifiedIdentifier(@class), GetMethodName(method));
            else
                Write("{0} {1}::{2}(", method.ReturnType, QualifiedIdentifier(@class),
                      SafeIdentifier(method.Name));

            GenerateMethodParameters(method);

            WriteLine(")");

            if (method.IsConstructor)
                GenerateClassConstructorBase(@class, isIntPtr: false, method: method);

            WriteStartBraceIndent();

            PushBlock(CLIBlockKind.MethodBody, method);

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
            PopBlock(NewLineKind.Always);
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

            GenerateValueTypeConstructorCallFields(@class);
        }

        private void GenerateValueTypeConstructorCallFields(Class @class)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.Ignore) 
                    continue;

                var baseClass = @base.Class;
                GenerateValueTypeConstructorCallFields(baseClass);
            }

            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field)) continue;

                var varName = string.Format("_native.{0}", field.OriginalName);

                var ctx = new MarshalContext(Driver)
                    {
                        ReturnVarName = varName,
                        ReturnType = field.QualifiedType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                field.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("this->{0} = {1};", field.Name, marshal.Context.Return);
            }
        }

        public void GenerateFunction(Function function, DeclarationContext @namespace)
        {
            if (function.Ignore)
                return;

            GenerateDeclarationCommon(function);

            var classSig = string.Format("{0}{1}{2}", QualifiedIdentifier(@namespace),
                Options.OutputNamespace, TranslationUnit.FileNameWithoutExtension);

            Write("{0} {1}::{2}(", function.ReturnType, classSig,
                SafeIdentifier(function.Name));

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

        public void GenerateFunctionCall(Function function, Class @class = null)
        {
            var retType = function.ReturnType;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            const string valueMarshalName = "_this0";
            var isValueType = @class != null && @class.IsValueType;
            if (isValueType && !IsNativeFunctionOrStaticMethod(function))
            {
                WriteLine("auto {0} = ::{1}();", valueMarshalName, @class.QualifiedOriginalName);

                var param = new Parameter() { Name = "(*this)" };
                var ctx = new MarshalContext(Driver)
                    {
                        MarshalVarPrefix = valueMarshalName,
                        Parameter = param
                    };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                marshal.MarshalValueClassFields(@class, valueMarshalName);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);
            }

            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);

            if (needsReturn)
                Write("auto {0}{1} = ",(function.ReturnType.Type.IsReference())? "&": string.Empty,
                    Generator.GeneratedIdentifier("ret"));

            if (function.OperatorKind == CXXOperatorKind.Conversion)
            {
                var method = function as Method;
                var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var typeName = method.ConversionType.Visit(typePrinter);
                WriteLine("({0}) {1};", typeName, @params[0].Name);
            }
            else if (function.IsOperator)
            {
                var opName = function.Name.Replace("operator", "").Trim();

                switch (Operators.ClassifyOperator(function))
                {
                case CXXOperatorArity.Unary:
                    WriteLine("{0} {1};", opName, @params[0].Name);
                    break;
                case CXXOperatorArity.Binary:
                    WriteLine("{0} {1} {2};", @params[0].Name, opName,
                        @params[1].Name);
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
                        Generator.GeneratedIdentifier("ret"),
                        isIntPtr ? "System::IntPtr()" : "nullptr");
                }

                var ctx = new MarshalContext(Driver)
                    {
                        ArgName = Generator.GeneratedIdentifier("ret"),
                        ReturnVarName = Generator.GeneratedIdentifier("ret"),
                        ReturnType = retType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                function.ReturnType.Type.Visit(marshal, function.ReturnType.Qualifiers);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
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

            if (param.IsOut || param.IsInOut)
            {
                var paramType = param.Type;
                if (paramType is PointerType)
                {
                    if (!paramType.IsReference())
                        paramMarshal.Prefix = "&";
                    paramType = (paramType as PointerType).Pointee;
                }

                var typePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var type = paramType.Visit(typePrinter);

                WriteLine("{0} {1};", type, argName);
            }
            else
            {
                var ctx = new MarshalContext(Driver)
                        {
                            Parameter = param,
                            ParameterIndex = paramIndex,
                            ArgName = argName,
                            Function = function
                        };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (string.IsNullOrEmpty(marshal.Context.Return))
                    throw new Exception("Cannot marshal argument of function");

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("auto {0}{1} = {2};", marshal.VarPrefix, argName,
                    marshal.Context.Return);
                argName = marshal.ArgumentPrefix + argName;
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
