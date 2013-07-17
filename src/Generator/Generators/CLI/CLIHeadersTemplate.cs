using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI header files.
    /// </summary>
    public class CLIHeadersTemplate : CLITextTemplate
    {
        public override string FileExtension { get { return "h"; } }

        public CLIHeadersTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
        {
        }

        public override void Process()
        {
            PushBlock(BlockKind.Header);
            PopBlock();

            PushBlock(CLIBlockKind.Includes);
            WriteLine("#pragma once");
            NewLine();

            PushBlock(CLIBlockKind.IncludesForwardReferences);
            WriteLine("#include <{0}>", TranslationUnit.IncludePath);
            GenerateIncludeForwardRefs();
            PopBlock(NewLineKind.BeforeNextBlock);

            PopBlock(NewLineKind.Always);

            GenerateNamespace(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateIncludeForwardRefs()
        {
            var typeRefs = TranslationUnit.TypeReferences as TypeRefsVisitor;

            var forwardRefsPrinter = new CLIForwardReferencePrinter(typeRefs);
            forwardRefsPrinter.Process();

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var include in forwardRefsPrinter.Includes)
            {
                if (string.IsNullOrWhiteSpace(include))
                    continue;

                if (include == Path.GetFileNameWithoutExtension(TranslationUnit.FileName))
                    continue;

                includes.Add(string.Format("#include \"{0}.h\"", include));
            }

            foreach (var include in Includes)
                includes.Add(include.ToString());

            foreach (var include in includes)
                WriteLine(include);
        }

        public void GenerateForwardRefs(Namespace @namespace)
        {
            var typeRefs = TranslationUnit.TypeReferences as TypeRefsVisitor;

            var forwardRefsPrinter = new CLIForwardReferencePrinter(typeRefs);
            forwardRefsPrinter.Process();

            // Use a set to remove duplicate entries.
            var forwardRefs = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var forwardRef in forwardRefsPrinter.Refs)
            {
                if (forwardRef.Namespace != @namespace)
                    continue;

                forwardRefs.Add(forwardRef.Text);
            }

            foreach (var forwardRef in forwardRefs)
                WriteLine(forwardRef);
        }

        public void GenerateNamespace(Namespace @namespace)
        {
            var isTopLevel = @namespace is TranslationUnit;
            var generateNamespace = !isTopLevel || Options.GenerateLibraryNamespace;

            if (generateNamespace)
            {
                PushBlock(CLIBlockKind.Namespace, @namespace);
                WriteLine("namespace {0}", isTopLevel
                                               ? Options.OutputNamespace
                                               : SafeIdentifier(@namespace.Name));
                WriteStartBraceIndent();
            }

            // Generate the forward references.
            PushBlock(CLIBlockKind.ForwardReferences);
            GenerateForwardRefs(@namespace);
            PopBlock(NewLineKind.BeforeNextBlock);

            // Generate all the enum declarations for the module.
            foreach (var @enum in @namespace.Enums)
            {
                if (@enum.Ignore || @enum.IsIncomplete)
                    continue;

                PushBlock(CLIBlockKind.Enum, @enum);
                GenerateEnum(@enum);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate all the typedef declarations for the module.
            GenerateTypedefs(@namespace);

            // Generate all the struct/class declarations for the module.
            foreach (var @class in @namespace.Classes)
            {
                if (@class.Ignore || @class.IsIncomplete)
                    continue;

                if (@class.IsOpaque)
                    continue;

                PushBlock(CLIBlockKind.Class, @class);
                GenerateClass(@class);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (@namespace.HasFunctions)
                GenerateFunctions(@namespace);

            foreach(var childNamespace in @namespace.Namespaces)
                GenerateNamespace(childNamespace);

            if (generateNamespace)
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateTypedefs(Namespace @namespace)
        {
            foreach (var typedef in @namespace.Typedefs)
            {
                if (typedef.Ignore)
                    continue;

                GenerateTypedef(typedef);
            }
        }

        public void GenerateFunctions(Namespace @namespace)
        {
            PushBlock(CLIBlockKind.FunctionsClass);

            WriteLine("public ref class {0}{1}", SafeIdentifier(Options.OutputNamespace),
                TranslationUnit.FileNameWithoutExtension);
            WriteLine("{");
            WriteLine("public:");
            PushIndent();

            // Generate all the function declarations for the module.
            foreach (var function in @namespace.Functions)
            {
                GenerateFunction(function);
            }

            PopIndent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateDeclarationCommon(Declaration T)
        {
            GenerateSummary(T.BriefComment);
            GenerateDebug(T);
        }

        public void GenerateClass(Class @class)
        {
            if (@class.Ignore || @class.IsIncomplete)
                return;

            GenerateDeclarationCommon(@class);

            if (@class.IsUnion)
            {
                // TODO: How to do wrapping of unions?
                //const string @namespace = "System::Runtime::InteropServices";
                //WriteLine("[{0}::StructLayout({0}::LayoutKind::Explicit)]",
                //    @namespace);
                //throw new NotImplementedException("Unions are not supported yet");
            }

            if (GenerateClassProlog(@class))
                return;

            var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);

            if (@class.IsRefType)
            {
                GenerateClassNativeField(@class, nativeType);
            }

            GenerateClassConstructors(@class, nativeType);

            GenerateClassFields(@class);

            // Generate a property for each field if class is not value type
            if (@class.IsRefType)
                GenerateClassProperties(@class);

            GenerateClassEvents(@class);
            GenerateClassMethods(@class);

            if (Options.GenerateFunctionTemplates)
                GenerateClassGenericMethods(@class);

            GenerateClassVariables(@class);

            WriteLine("};");
        }

        public void GenerateClassNativeField(Class @class, string nativeType)
        {
            Class baseClass = null;

            if (@class.HasBaseClass)
                baseClass = @class.Bases[0].Class;

            var hasRefBase = baseClass != null && baseClass.IsRefType
                             && !baseClass.Ignore;

            var hasIgnoredBase = baseClass != null && baseClass.Ignore;

            if (!@class.HasBase || !hasRefBase || hasIgnoredBase)
            {
                WriteLineIndent("property {0} NativePtr;", nativeType);
                NewLine();
            }
        }

        public void GenerateClassGenericMethods(Class @class)
        {
            var printer = TypePrinter as CLITypePrinter;
            var oldCtx = printer.Context;

            PushIndent();
            foreach (var template in @class.Templates)
            {
                if (template.Ignore) continue;

                var functionTemplate = template as FunctionTemplate;
                if (functionTemplate == null) continue;

                var function = functionTemplate.TemplatedFunction;

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
                WriteLine("{0} {1}({2});", retType, SafeIdentifier(function.Name),
                    GenerateParametersList(function.Parameters));
            }
            PopIndent();

            printer.Context = oldCtx;
        }

        public void GenerateClassConstructors(Class @class, string nativeType)
        {
            PushIndent();

            // Output a default constructor that takes the native pointer.
            WriteLine("{0}({1} native);", SafeIdentifier(@class.Name), nativeType);
            WriteLine("{0}({1} native);", SafeIdentifier(@class.Name), "System::IntPtr");

            foreach (var ctor in @class.Constructors)
            {
                if (ctor.IsCopyConstructor || ctor.IsMoveConstructor)
                    continue;

                // Default constructors are not supported in .NET value types.
                if (ctor.Parameters.Count == 0 && @class.IsValueType)
                    continue;

                GenerateMethod(ctor);
            }

            PopIndent();
        }

        public void GenerateClassFields(Class @class)
        {
            if (!@class.IsValueType)
                return;

            // Handle the case of struct (value-type) inheritance by adding the base
            // fields to the managed value subtypes.
            foreach (var @base in @class.Bases)
            {
                Class baseClass;
                if (!@base.Type.IsTagDecl(out baseClass))
                    continue;

                if (!baseClass.IsValueType || baseClass.Ignore)
                {
                    Console.WriteLine("Ignored base class of value type '{0}'",
                        baseClass.Name);
                    continue;
                }

                GenerateClassFields(baseClass);
            }

            PushIndent();
            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(@class, field)) continue;

                GenerateDeclarationCommon(field);
                if (@class.IsUnion)
                    WriteLine("[FieldOffset({0})]", field.Offset);
                WriteLine("{0} {1};", field.Type, SafeIdentifier(field.Name));
            }
            PopIndent();
        }

        public void GenerateClassEvents(Class @class)
        {
            foreach (var @event in @class.Events)
            {
                if (@event.Ignore) continue;

                var cppTypePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var cppArgs = cppTypePrinter.VisitParameters(@event.Parameters, hasNames: true);

                WriteLine("private:");
                PushIndent();

                var delegateName = string.Format("_{0}Delegate", @event.Name);
                WriteLine("delegate void {0}({1});", delegateName, cppArgs);
                WriteLine("{0}^ {0}Instance;", delegateName);

                WriteLine("void _{0}Raise({1});", @event.Name, cppArgs);
                WriteLine("{0} _{1};", @event.Type, @event.Name);

                PopIndent();
                WriteLine("public:");
                PushIndent();

                WriteLine("event {0} {1}", @event.Type, @event.Name);
                WriteStartBraceIndent();

                WriteLine("void add({0} evt);", @event.Type);
                WriteLine("void remove({0} evt);", @event.Type);

                var cliTypePrinter = new CLITypePrinter(Driver);
                var cliArgs = cliTypePrinter.VisitParameters(@event.Parameters, hasNames: true);

                WriteLine("void raise({0});", cliArgs);
                WriteCloseBraceIndent();
                PopIndent();
            }
        }

        public void GenerateClassMethods(Class @class)
        {
            PushIndent();

            var staticMethods = new List<Method>();
            foreach (var method in @class.Methods)
            {
                if (ASTUtils.CheckIgnoreMethod(@class, method))
                    continue;

                if (method.IsConstructor)
                    continue;

                if (method.IsStatic)
                {
                    staticMethods.Add(method);
                    continue;
                }

                GenerateMethod(method);
            }

            foreach(var method in staticMethods)
                GenerateMethod(method);

            PopIndent();
        }

        public void GenerateClassVariables(Class @class)
        {
            PushIndent();

            foreach(var variable in @class.Variables)
            {
                if (variable.Ignore) continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                var type = variable.Type;

                WriteLine("static property {0} {1}", type, variable.Name);

                WriteStartBraceIndent();

                WriteLine("{0} get();", type);

                if (!variable.QualifiedType.Qualifiers.IsConst)
                    WriteLine("void set({0});", type);

                WriteCloseBraceIndent();
            }

            PopIndent();
        }

        public bool GenerateClassProlog(Class @class)
        {
            Write("public ");

            Write(@class.IsValueType ? "value struct " : "ref class ");

            Write("{0}", SafeIdentifier(@class.Name));

            if (@class.IsOpaque)
            {
                WriteLine(";");
                return true;
            }

            if (@class.HasBase && !@class.IsValueType)
                if (!@class.Bases[0].Class.Ignore)
                    Write(" : {0}", QualifiedIdentifier(@class.Bases[0].Class));

            WriteLine(string.Empty);
            WriteLine("{");
            WriteLine("public:");
            return false;
        }

        public void GenerateClassProperties(Class @class)
        {
            PushIndent();
            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(@class, field))
                    continue;

                GenerateDeclarationCommon(field);
                GenerateFieldProperty(field);
            }
            PopIndent();
        }

        public void GenerateFieldProperty(Field field)
        {
            var type = field.Type.Visit(TypePrinter, field.QualifiedType.Qualifiers);

            WriteLine("property {0} {1}", type, field.Name);
            WriteStartBraceIndent();

            WriteLine("{0} get();", type);
            WriteLine("void set({0});", type);

            WriteCloseBraceIndent();
        }

        public void GenerateMethod(Method method)
        {
            if (method.Ignore) return;

            if (method.Access != AccessSpecifier.Public)
                return;

            PushBlock(CLIBlockKind.Method, method);

            GenerateDeclarationCommon(method);

            if (method.IsStatic)
                Write("static ");

            if (method.Kind == CXXMethodKind.Constructor || method.Kind == CXXMethodKind.Destructor)
                Write("{0}(", SafeIdentifier(method.Name));
            else
                Write("{0} {1}(", method.ReturnType, SafeIdentifier(method.Name));

            GenerateMethodParameters(method);

            WriteLine(");");

            PopBlock();
        }

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (typedef.Ignore)
                return false;

            FunctionType function;
            if (typedef.Type.IsPointerTo<FunctionType>(out function))
            {
                PushBlock(CLIBlockKind.Typedef, typedef);
                GenerateDeclarationCommon(typedef);

                WriteLine("public {0};",
                    string.Format(TypePrinter.VisitDelegate(function),
                    SafeIdentifier(typedef.Name)));
                PopBlock(NewLineKind.BeforeNextBlock);

                return true;
            }
            else if (typedef.Type.IsEnumType())
            {
                // Already handled in the parser.
            }
            else
            {
                Console.WriteLine("Unhandled typedef type: {0}", typedef);
            }

            return false;
        }

        public void GenerateFunction(Function function)
        {
            if (function.Ignore)
                return;

            PushBlock(CLIBlockKind.Function, function);

            GenerateDeclarationCommon(function);

            var retType = function.ReturnType.ToString();
            Write("static {0} {1}(", retType, SafeIdentifier(function.Name));

            Write(GenerateParametersList(function.Parameters));

            WriteLine(");");

            PopBlock();
        }

        public void GenerateDebug(Declaration decl)
        {
            if (Options.OutputDebug && !String.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public void GenerateEnum(Enumeration @enum)
        {
            if (@enum.Ignore || @enum.IsIncomplete)
                return;

            GenerateDeclarationCommon(@enum);

            if (@enum.Modifiers.HasFlag(Enumeration.EnumModifiers.Flags))
                WriteLine("[System::Flags]");

            Write("public enum struct {0}", SafeIdentifier(@enum.Name));

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int32)
                WriteLine(" : {0}", typeName);
            else
                NewLine();

            WriteLine("{");

            PushIndent();
            for (int i = 0; i < @enum.Items.Count; ++i)
            {
                var item = @enum.Items[i];
                GenerateInlineSummary(item.Comment);
                if (item.ExplicitValue)
                    Write(String.Format("{0} = {1}", SafeIdentifier(item.Name), item.Value));
                else
                    Write(String.Format("{0}", SafeIdentifier(item.Name)));

                if (i < @enum.Items.Count - 1)
                    WriteLine(",");
            }
            PopIndent();

            NewLine();
            WriteLine("};");
        }
    }
}
