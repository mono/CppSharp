using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    public class CLISourcesTemplate : CLITextTemplate
    {
        public CLISourcesTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
        {
            
        }

        public override void Generate()
        {
            GenerateStart();

            var file = Path.GetFileNameWithoutExtension(TranslationUnit.FileName).Replace('\\', '/');

            if (Driver.Options.GenerateName != null)
                file = Driver.Options.GenerateName(TranslationUnit);

            WriteLine("#include \"{0}.h\"", file);

            GenerateForwardReferenceHeaders();

            if (Options.OutputInteropIncludes)
                WriteLine("#include <clix.hpp>");

            NewLine();

            WriteLine("using namespace System;");
            WriteLine("using namespace System::Runtime::InteropServices;");
            GenerateAfterNamespaces();
            NewLine();

            GenerateDeclarations();
        }

        public void GenerateForwardReferenceHeaders()
        {
            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            var typeRefs = TranslationUnit.TypeReferences as TypeRefsVisitor;

            // Generate the forward references.
            foreach (var forwardRef in typeRefs.References)
            {
                var decl = forwardRef.Declaration;

                if (decl.IsIncomplete && decl.CompleteDeclaration != null)
                     decl = decl.CompleteDeclaration;

                var @namespace = decl.Namespace;
                var translationUnit = @namespace.TranslationUnit;

                if (translationUnit.Ignore)
                    continue;

                if (translationUnit.IsSystemHeader)
                    continue;

                var includeName = Path.GetFileNameWithoutExtension(translationUnit.FileName);

                if (includeName == Path.GetFileNameWithoutExtension(((TextTemplate) this).TranslationUnit.FileName))
                    continue;

                includes.Add(string.Format("#include \"{0}.h\"", includeName.Replace('\\', '/')));
            }

            foreach (var include in Includes)
                includes.Add(include.ToString());

            foreach (var include in includes)
                WriteLine(include);
        }

        public void GenerateDeclarations()
        {
            GenerateNamespace(TranslationUnit);
        }

        private void GenerateNamespace(Namespace @namespace)
        {
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
                GenerateNamespace(childNamespace);
        }

        public void GenerateDeclarationCommon(Declaration decl)
        {
            if (!string.IsNullOrWhiteSpace(decl.BriefComment))
                WriteLine("// {0}", decl.BriefComment);
        }

        public void GenerateClass(Class @class)
        {
            //GenerateDeclarationCommon(@class);

            // Output a default constructor that takes the native pointer.
            GenerateClassConstructor(@class, isIntPtr: false);
            GenerateClassConstructor(@class, isIntPtr: true);

            foreach (var method in @class.Methods)
            {
                if (CheckIgnoreMethod(@class, method))
                    continue;

                GenerateDeclarationCommon(method);
                GenerateMethod(method, @class);

                NewLine();
            }

            if (@class.IsRefType)
            {
                foreach (var field in @class.Fields)
                {
                    if (CheckIgnoreField(@class, field))
                        continue;

                    GenerateFieldProperty(field);
                }
            }

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
        }

        private void GenerateFunctionTemplate(FunctionTemplate template, Class @class)
        {
            var printer = TypePrinter as CLITypePrinter;
            var oldCtx = printer.Context;

            var function = template.TemplatedFunction;

            var typeNames = template.Parameters.Select(
                param => "typename " + param.Name).ToList();

            var typeCtx = new CLITypePrinterContext()
                {
                    Kind = TypePrinterContextKind.Template,
                    Declaration = template
                };

            printer.Context = typeCtx;

            var typePrinter = new CLITypePrinter(Driver, typeCtx);
            var retType = function.ReturnType.Type.Visit(typePrinter,
                function.ReturnType.Qualifiers);

            WriteLine("generic<{0}>", string.Join(", ", typeNames));
            WriteLine("{0} {1}::{2}({3})", retType, QualifiedIdentifier(@class),
                      SafeIdentifier(function.Name),
                      GenerateParametersList(function.Parameters));

            WriteStartBraceIndent();

            GenerateFunctionCall(function, @class);

            WriteCloseBraceIndent();
            NewLine();

            printer.Context = oldCtx;
        }

        private void GenerateFieldProperty(Field field)
        {
            var @class = field.Class;

            GeneratePropertyGetter(field, @class);
            GeneratePropertySetter(field, @class);
        }

        private void GeneratePropertySetter<T>(T decl, Class @class)
            where T : Declaration, ITypedDecl
        {
            WriteLine("void {0}::{1}::set({2} value)", QualifiedIdentifier(@class),
                      decl.Name, decl.Type);
            WriteStartBraceIndent();

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

            WriteCloseBraceIndent();
            NewLine();
        }

        private void GeneratePropertyGetter<T>(T decl, Class @class)
            where T : Declaration, ITypedDecl
        {
            WriteLine("{0} {1}::{2}::get()", decl.Type, QualifiedIdentifier(@class),
                      decl.Name);
            WriteStartBraceIndent();

            string variable;
            if (decl is Variable)
                variable = string.Format("::{0}::{1}",
                                         @class.QualifiedOriginalName, decl.OriginalName);
            else
                variable = string.Format("((::{0}*)NativePtr)->{1}",
                                         @class.QualifiedOriginalName, decl.OriginalName);

            var ctx = new MarshalContext(Driver)
                {
                    ArgName = decl.Name,
                    ReturnVarName = variable,
                    ReturnType = decl.QualifiedType
                };

            var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
            decl.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Write(marshal.Context.SupportBefore);

            WriteLine("return {0};", marshal.Context.Return);

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
            GeneratePropertyGetter(variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable, @class);
        }

        private void GenerateClassConstructor(Class @class, bool isIntPtr)
        {
            Write("{0}::{1}(", QualifiedIdentifier(@class), SafeIdentifier(@class.Name));

            var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);
            WriteLine("{0} native)", isIntPtr ? "System::IntPtr" : nativeType);

            var hasBase = GenerateClassConstructorBase(@class);

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
                if (CheckIgnoreField(@class, field)) continue;

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

        private bool GenerateClassConstructorBase(Class @class, Method method = null)
        {
            var hasBase = @class.HasBase && !@class.Bases[0].Class.Ignore;

            if (hasBase && !@class.IsValueType)
            {
                PushIndent();
                Write(": {0}(", QualifiedIdentifier(@class.Bases[0].Class));

                if (method != null)
                    Write("nullptr");
                else
                    Write("native");

                WriteLine(")");
                PopIndent();
            }

            return hasBase;
        }

        public void GenerateMethod(Method method, Class @class)
        {
            GenerateDeclarationCommon(method);

            if (method.Kind == CXXMethodKind.Constructor || method.Kind == CXXMethodKind.Destructor)
                Write("{0}::{1}(", QualifiedIdentifier(@class), SafeIdentifier(method.Name));
            else
                Write("{0} {1}::{2}(", method.ReturnType, QualifiedIdentifier(@class),
                      SafeIdentifier(method.Name));

            GenerateMethodParameters(method);

            WriteLine(")");

            if (method.Kind == CXXMethodKind.Constructor)
                GenerateClassConstructorBase(@class, method);

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                if (method.Kind == CXXMethodKind.Constructor)
                {
                    if (!@class.IsAbstract)
                    {
                        var @params = GenerateFunctionParamsMarshal(method.Parameters, method);
                        Write("NativePtr = new ::{0}(", method.QualifiedOriginalName);
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
                if (method.Kind != CXXMethodKind.Constructor)
                    GenerateFunctionCall(method, @class);
                else
                    GenerateValueTypeConstructorCall(method, @class);
            }

            WriteCloseBraceIndent();
        }

        private void GenerateValueTypeConstructorCall(Method method, Class @class)
        {
            var names = new List<string>();

            foreach (var param in method.Parameters)
            {
                var ctx = new MarshalContext(Driver)
                              {
                                  Parameter = param,
                                  ArgName = param.Name,
                              };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                names.Add(marshal.Context.Return);
            }

            WriteLine("auto _native = ::{0}({1});", @class.QualifiedOriginalName,
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
                if (CheckIgnoreField(@class, field)) continue;

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

        public void GenerateFunction(Function function, Namespace @namespace)
        {
            if (function.Ignore)
                return;

            GenerateDeclarationCommon(function);

            var classSig = string.Format("{0}{1}{2}", QualifiedIdentifier(@namespace),
                Library.Name, TranslationUnit.FileNameWithoutExtension);

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
            if (isValueType)
            {
                WriteLine("auto {0} = ::{1}();", valueMarshalName, @class.QualifiedOriginalName);

                var param = new Parameter() { Name = "(*this)" };
                var ctx = new MarshalContext(Driver) { Parameter = param };

                var marshal = new CLIMarshalManagedToNativePrinter(ctx);
                marshal.MarshalValueClassFields(@class, valueMarshalName);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);
            }

            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);

            if (needsReturn)
                Write("auto {0}ret = ",(function.ReturnType.Type.IsReference())? "&": string.Empty);

            if (isValueType)
            {
                Write("{0}.", valueMarshalName);
            }
            else if (IsInstanceFunction(function))
            {
                Write("((::{0}*)NativePtr)->", @class.QualifiedOriginalName);
            }

            if (IsInstanceFunction(function))
            {
                Write("{0}(", function.OriginalName);
            }
            else
            {
                Write("::");
                Write("{0}(", function.QualifiedOriginalName);
            }

            GenerateFunctionParams(@params);
            WriteLine(");");

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

            if (isValueType)
            {
                GenerateStructMarshaling(@class, valueMarshalName + ".");
            }

            if (needsReturn)
            {
                var ctx = new MarshalContext(Driver)
                    {
                        ArgName = "ret",
                        ReturnVarName = "ret",
                        ReturnType = retType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(ctx);
                function.ReturnType.Type.Visit(marshal, function.ReturnType.Qualifiers);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }
        }

        private static bool IsInstanceFunction(Function function)
        {
            var isInstanceFunction = false;

            var method = function as Method;
            if (method != null)
                isInstanceFunction = method.Conversion == MethodConversionKind.None;
            return isInstanceFunction;
        }

        public struct ParamMarshal
        {
            public string Name;
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
            if (param.Type is BuiltinType)
            {
                return new ParamMarshal {Name = param.Name, Param = param};
            }

            var argName = "arg" + paramIndex.ToString(CultureInfo.InvariantCulture);

            if (param.Usage == ParameterUsage.Out)
            {
                var paramType = param.Type;
                if (paramType.IsReference())
                    paramType = (paramType as PointerType).Pointee;

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

            return new ParamMarshal {Name = argName, Param = param};
        }

        public void GenerateFunctionParams(List<ParamMarshal> @params)
        {
            var names = @params.Select(param => param.Name).ToList();
            Write(string.Join(", ", names));
        }

        public void GenerateDebug(Declaration decl)
        {
            if (Options.OutputDebug && !String.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public override string FileExtension { get { return "cpp"; } }
    }
}
