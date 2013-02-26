using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                Path.GetFileNameWithoutExtension(unit.FileName),
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
            var includes = new HashSet<string>();

            var typeRefs = unit.TypeReferences as TypeRefsVisitor;

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

                if (includeName == Path.GetFileNameWithoutExtension(((TextTemplate) this).unit.FileName))
                    continue;

                includes.Add(includeName);
            }

            foreach (var include in includes)
            {
                WriteLine("#include \"{0}.h\"", include);
            }
        }

        public void GenerateDeclarations()
        {
            // Generate all the struct/class definitions for the module.
            for (var i = 0; i < unit.Classes.Count; ++i)
            {
                var @class = unit.Classes[i];

                if (@class.Ignore)
                    continue;

                if (@class.IsOpaque || @class.IsIncomplete)
                    continue;

                GenerateClass(@class);
            }

            if (unit.HasFunctions)
            {
                var staticClassName = Library.Name + unit.FileNameWithoutExtension;

                // Generate all the function declarations for the module.
                for (var i = 0; i < unit.Functions.Count; ++i)
                {
                    var function = unit.Functions[i];

                    if (function.Ignore)
                        continue;

                    GenerateFunction(function, staticClassName);
                    NewLine();
                }
            }
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
        }

        private void GenerateClassConstructor(Class @class, bool isIntPtr)
        {
            Write("{0}::{1}(", QualifiedIdentifier(@class), SafeIdentifier(@class.Name));

            var nativeType = string.Format("::{0}*", @class.OriginalName);
            WriteLine("{0} native)", isIntPtr ? "System::IntPtr" : nativeType);

            var hasBase = GenerateClassConstructorBase(@class);

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                if (!hasBase)
                {
                    Write("NativePtr = ");

                    if (isIntPtr)
                        Write("({0})", nativeType);
                    Write("native");
                    if (isIntPtr)
                        Write(".ToPointer()");
                    WriteLine(";");
                }
            }
            else
            {
                WriteLine("// TODO: Struct marshaling");
            }

            WriteCloseBraceIndent();
            NewLine();
        }

        private void GenerateStructMarshaling(Class @class)
        {
            foreach (var field in @class.Fields)
            {

                WriteLine("{0} = {1}");
            }
        }

        private bool GenerateClassConstructorBase(Class @class, Method method = null)
        {
            var hasBase = @class.HasBase && !@class.Bases[0].Class.Ignore;

            if (hasBase && !@class.IsValueType)
            {
                PushIndent();
                Write(": {0}(", @class.Bases[0].Class.Name);

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
                        var @params = GenerateFunctionParamsMarshal(method);
                        Write("NativePtr = new ::{0}(", method.QualifiedOriginalName);
                        GenerateFunctionParams(method, @params);
                        WriteLine(");");
                    }
                }
                else
                {
                    GenerateFunctionCall(method);
                }
            }
            else if (@class.IsValueType)
            {
                if (method.Kind != CXXMethodKind.Constructor)
                    GenerateFunctionCall(method, @class);
            }

            WriteCloseBraceIndent();
        }

        public void GenerateFunction(Function function, string className)
        {
            if (function.Ignore)
                return;

            GenerateDeclarationCommon(function);

            Write("{0} {1}::{2}::{3}(", function.ReturnType, Library.Name, className,
                SafeIdentifier(function.Name));

            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                Write("{0}", TypePrinter.GetArgumentString(param));
                if (i < function.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(")");
            WriteLine("{");
            PushIndent();

            GenerateFunctionCall(function);

            PopIndent();
            WriteLine("}");
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

            var @params = GenerateFunctionParamsMarshal(function);

            if (needsReturn)
                Write("auto ret = ");

            if (function.ReturnType.IsReference() && !isValueType)
            {
                Write("&");
            }

            if (isValueType)
            {
                Write("this0->");
                Write("{0}(", function.QualifiedOriginalName);
                GenerateFunctionParams(function, @params);
                WriteLine(");");
            }
            else
            {
                Write(IsInstanceFunction(function) ? "NativePtr->" : "::");

                Write("{0}(", function.QualifiedOriginalName);
                GenerateFunctionParams(function, @params);
                WriteLine(");");
            }

            if (needsReturn)
            {
                Write("return ");

                var ctx = new MarshalContext()
                    {
                        ReturnVarName = "ret",
                        ReturnType = retType
                    };

                var marshal = new CLIMarshalNativeToManagedPrinter(Driver.TypeDatabase,
                    Library, ctx);
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

        public List<ParamMarshal> GenerateFunctionParamsMarshal(Function function)
        {
            var @params = new List<ParamMarshal>();

            var method = function as Method;

            // Do marshaling of parameters
            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                
                if (param.Type is BuiltinType)
                {
                    @params.Add(new ParamMarshal {Name = param.Name, Param = param});
                }
                else
                {
                    var argName = "arg" + i.ToString(CultureInfo.InvariantCulture);

                    var ctx = new MarshalContext()
                        {
                            Parameter = param,
                            ParameterIndex = i,
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

                    WriteLine("auto {0} = {1};", argName, marshal.Return);

                    if (!string.IsNullOrWhiteSpace(marshal.SupportAfter))
                        WriteLine(marshal.SupportAfter);

                    var argText = marshal.ArgumentPrefix + argName;
                    @params.Add(new ParamMarshal { Name = argText, Param = param });
                }
            }

            return @params;
        }

        public void GenerateFunctionParams(Function function, List<ParamMarshal> @params)
        {
            for (var i = 0; i < @params.Count; ++i)
            {
                var param = @params[i];

                Write(param.Name);

                if (i < @params.Count - 1)
                    Write(", ");
            }
        }

        public void GenerateDebug(Declaration decl)
        {
            if (Options.OutputDebug && !String.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public override string FileExtension { get { return "cpp"; } }
    }
}
