using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
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

            WriteLine("#include \"{0}{1}.h\"",
                TranslationUnit.FileNameWithoutExtension,
                Options.WrapperSuffix);

            GenerateForwardReferenceHeaders();
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
            foreach (var forwardRef in typeRefs.ForwardReferences)
            {
                var decl = forwardRef;

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

                includes.Add(string.Format("#include \"{0}.h\"", includeName));
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

            foreach (var @event in @class.Events)
            {
                GenerateDeclarationCommon(@event);
                GenerateEvent(@event, @class);
            }
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

            var @params = GetEventParameters(@event);
            var args = typePrinter.VisitParameters(@params, hasNames: false);

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
            var typePrinter = new CLITypePrinter(Driver.TypeDatabase, Library);

            var @params = GetEventParameters(@event);
            var args = typePrinter.VisitParameters(@params, hasNames: true);

            WriteLine("void {0}::{1}::raise({2})", QualifiedIdentifier(@class),
                      @event.Name, args);

            WriteStartBraceIndent();

            var paramNames = @params.Select(param => param.Name).ToList();
            WriteLine("_{0}({1});", @event.Name, string.Join(" ", paramNames));

            WriteCloseBraceIndent();
        }

        private void GenerateEventRaiseWrapper(Event @event, Class @class)
        {
            var typePrinter = new CppTypePrinter(Driver.TypeDatabase);

            var @params = GetEventParameters(@event);
            var args = typePrinter.VisitParameters(@params, hasNames: true);

            WriteLine("void {0}::_{1}Raise({2})", QualifiedIdentifier(@class),
                @event.Name, args);

            WriteStartBraceIndent();

            var returns = new List<string>();
            foreach (var param in @params)
            {
                var ctx = new MarshalContext(Driver)
                    {
                        ReturnVarName = param.Name,
                        ReturnType = param.Type
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(Driver, ctx);
                param.Visit(marshal);

                returns.Add(marshal.Return);
            }

            Write("{0}::raise(", @event.Name);
            Write("{0}", string.Join(", ", returns));
            WriteLine(");");

            WriteCloseBraceIndent();
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
                GenerateStructMarshaling(@class, nativePtr);
            }

            WriteCloseBraceIndent();
            NewLine();
        }

        private void GenerateStructMarshaling(Class @class, string nativePointer)
        {
            foreach (var field in @class.Fields)
            {
                var nativeField = string.Format("{0}->{1}",
                                                nativePointer, field.OriginalName);

                var ctx = new MarshalContext(Driver)
                {
                    ReturnVarName = nativeField,
                    ReturnType = field.Type
                };

                var marshal = new CLIMarshalNativeToManagedPrinter(Driver, ctx);
                field.Visit(marshal);

                WriteLine("{0} = {1};", field.Name, marshal.Return);
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
            }

            WriteCloseBraceIndent();
        }

        public void GenerateFunction(Function function, Namespace @namespace)
        {
            if (function.Ignore)
                return;

            GenerateDeclarationCommon(function);

            var classSig = string.Format("{0}::{1}{2}", QualifiedIdentifier(@namespace),
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
            var needsReturn = !retType.IsPrimitiveType(PrimitiveType.Void);

            var isValueType = @class != null && @class.IsValueType;
            if (isValueType)
            {
                WriteLine("auto this0 = (::{0}*) 0;", @class.QualifiedOriginalName);
            }

            var @params = GenerateFunctionParamsMarshal(function.Parameters, function);

            if (needsReturn)
                Write("auto ret = ");

            if (isValueType)
            {
                Write("this0->");
            }
            else
            {
                if (IsInstanceFunction(function))
                {
                    Write("((::{0}*)NativePtr)->", @class.QualifiedOriginalName);
                    Write("{0}(", function.Name);
                }
                else
                {
                    Write("::");
                    Write("{0}(", function.QualifiedOriginalName);
                }
            }

            GenerateFunctionParams(@params);
            WriteLine(");");

            if (needsReturn)
            {
                Write("return ");

                var ctx = new MarshalContext(Driver)
                    {
                        ReturnVarName = "ret",
                        ReturnType = retType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(Driver, ctx);
                function.ReturnType.Visit(marshal);

                WriteLine("{0};", marshal.Return);
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

            var ctx = new MarshalContext(Driver)
                {
                    Parameter = param,
                    ParameterIndex = paramIndex,
                    ArgName = argName,
                    Function = function
                };

            var marshal = new CLIMarshalManagedToNativePrinter(Driver.TypeDatabase,
                                                                ctx);

            param.Visit(marshal);

            if (string.IsNullOrEmpty(marshal.Return))
                throw new Exception("Cannot marshal argument of function");

            if (!string.IsNullOrWhiteSpace(marshal.SupportBefore))
                WriteLine(marshal.SupportBefore);

            WriteLine("auto {0}{1} = {2};", marshal.VarPrefix, argName, marshal.Return);

            if (!string.IsNullOrWhiteSpace(marshal.SupportAfter))
                WriteLine(marshal.SupportAfter);

            var argText = marshal.ArgumentPrefix + argName;
            return new ParamMarshal {Name = argText, Param = param};
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
