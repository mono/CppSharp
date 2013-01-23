using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLISourcesTemplate : CLITextTemplate
    {
        protected override void Generate()
        {
            GenerateStart();

            WriteLine("#include \"{0}.h\"",
                Path.GetFileNameWithoutExtension(Module.FileName));
            NewLine();

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

            // Generate the forward references.
            foreach (var decl in Module.ForwardReferences)
            {
                var @namespace = decl.Namespace;
                var unit = @namespace.TranslationUnit;

                if (unit.Ignore)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                var includeName = Path.GetFileNameWithoutExtension(unit.FileName);

                if (includeName == Path.GetFileNameWithoutExtension(Module.FileName))
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
            for (var i = 0; i < Module.Classes.Count; ++i)
            {
                var @class = Module.Classes[i];

                if (@class.Ignore)
                    continue;

                if (@class.IsOpaque || @class.IsIncomplete)
                    continue;

                GenerateClass(@class);
            }

            if (Module.HasFunctions)
            {
                var staticClassName = Library.Name + Module.FileNameWithoutExtension;

                // Generate all the function declarations for the module.
                for (var i = 0; i < Module.Functions.Count; ++i)
                {
                    var function = Module.Functions[i];

                    if (function.Ignore)
                        continue;

                    GenerateFunction(function, staticClassName);
                    NewLine();
                }
            }
        }

        public void GenerateDeclarationCommon(Declaration decl)
        {
        }

        public void GenerateClass(Class @class)
        {
            GenerateDeclarationCommon(@class);

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
            WriteLine("{");
            PushIndent();

            if (@class.IsRefType)
            {
                Write("NativePtr = ");
                if (isIntPtr)
                    Write("({0})", nativeType);
                Write("native");
                if (isIntPtr)
                    Write(".ToPointer()");
                WriteLine(";");
            }
            else
            {
                WriteLine("// TODO: Struct marshaling");
            }

            PopIndent();
            WriteLine("}");
            NewLine();
        }

        public void GenerateMethod(Method method, Class @class)
        {
            GenerateDeclarationCommon(method);

            if (method.Kind == CXXMethodKind.Constructor || method.Kind == CXXMethodKind.Destructor)
                Write("{0}::{1}(", QualifiedIdentifier(@class), SafeIdentifier(method.Name));
            else
                Write("{0} {1}::{2}(", method.ReturnType, QualifiedIdentifier(@class),
                      SafeIdentifier(method.Name));

            for (var i = 0; i < method.Parameters.Count; ++i)
            {
                var param = method.Parameters[i];
                Write("{0}", TypeSig.GetArgumentString(param));
                if (i < method.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(")");
            WriteLine("{");
            PushIndent();

            if (@class.IsRefType)
            {
                if (method.Kind == CXXMethodKind.Constructor)
                {
                    var @params = GenerateFunctionParamsMarshal(method);
                    Write("NativePtr = new ::{0}(", method.QualifiedOriginalName);
                    GenerateFunctionParams(method, @params);
                    WriteLine(");");
                }
                else
                {
                    GenerateFunctionCall(method);
                }
            }

            PopIndent();
            WriteLine("}");
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
                Write("{0}", TypeSig.GetArgumentString(param));
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

        public void GenerateFunctionCall(Function function)
        {
            var retType = function.ReturnType;
            var needsReturn = !retType.IsPrimitiveType(PrimitiveType.Void);

            var @params = GenerateFunctionParamsMarshal(function);

            if (needsReturn)
                Write("auto ret = ");

            if (function is Method)
                Write("NativePtr->");
            else
                Write("::");

            Write("{0}(", function.QualifiedOriginalName);
            GenerateFunctionParams(function, @params);
            WriteLine(");");

            if (needsReturn)
            {
                Write("return ");

                var ctx = new MarshalContext() { ReturnVarName = "ret" };
                var marshal = new CLIMarshalNativeToManagedPrinter(Generator, ctx);
                function.ReturnType.Visit(marshal);

                WriteLine("{0};", marshal.Return);
            }
        }

        public List<string> GenerateFunctionParamsMarshal(Function function)
        {
            var @params = new List<string>();

            // Do marshalling of parameters
            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                
                if (param.Type is BuiltinType)
                {
                    @params.Add(param.Name);
                }
                else
                {
                    var argName = "arg" + i.ToString(CultureInfo.InvariantCulture);
                    var ctx = new MarshalContext() { Parameter = param, ArgName = argName };
                    var marshal = new CLIMarshalManagedToNativePrinter(Generator, ctx);
                    param.Visit(marshal);

                    if (string.IsNullOrEmpty(marshal.Return))
                        return null;

                    if (!string.IsNullOrWhiteSpace(marshal.Support))
                        WriteLine(marshal.Support);

                    WriteLine("auto {0} = {1};", argName, marshal.Return);
                    @params.Add(argName);
                }
            }

            return @params;
        }

        public void GenerateFunctionParams(Function function, List<string> @params)
        {
            for (var i = 0; i < @params.Count; ++i)
            {
                var param = @params[i];
                Write(param);

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
