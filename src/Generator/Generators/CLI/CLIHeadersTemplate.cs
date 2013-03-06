using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLIHeadersTemplate : CLITextTemplate
    {
        public override string FileExtension { get { return "h"; } }

        private CLIForwardRefeferencePrinter forwardRefsPrinter;

        public CLIHeadersTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
        {
            
        }

        public override void Generate()
        {
            GenerateStart();

            WriteLine("#pragma once");
            NewLine();

            WriteLine("#include <{0}>", unit.IncludePath);
            GenerateIncludeForwardRefs();

            NewLine();

            WriteLine("namespace {0}", SafeIdentifier(Library.Name));
            WriteLine("{");
            GenerateDeclarations();
            WriteLine("}");
        }

        public void GenerateIncludeForwardRefs()
        {
            var typeRefs = unit.TypeReferences as TypeRefsVisitor;

            forwardRefsPrinter = new CLIForwardRefeferencePrinter(typeRefs);
            forwardRefsPrinter.Process();

            var includes = new HashSet<string>();

            foreach (var include in forwardRefsPrinter.Includes)
            {
                if (string.IsNullOrWhiteSpace(include))
                    continue;

                if (include == Path.GetFileNameWithoutExtension(unit.FileName))
                    continue;

                includes.Add(string.Format("#include \"{0}.h\"", include));
            }

            foreach (var include in includes)
                WriteLine(include);
        }

        public void GenerateForwardRefs()
        {
            // Use a set to remove duplicate entries.
            var forwardRefs = new HashSet<string>();

            foreach (var forwardRef in forwardRefsPrinter.Refs)
            {
                forwardRefs.Add(forwardRef);
            }

            foreach (var forwardRef in forwardRefs)
            {
                WriteLine(forwardRef);
            }

            if (forwardRefs.Count > 0)
                NewLine();
        }

        public void GenerateDeclarations()
        {
            PushIndent();

            // Generate the forward references.
            GenerateForwardRefs();

            bool needsNewline = false;

            // Generate all the enum declarations for the module.
            for (var i = 0; i < unit.Enums.Count; ++i)
            {
                var @enum = unit.Enums[i];

                if (@enum.Ignore || @enum.IsIncomplete)
                    continue;

                GenerateEnum(@enum);
                NeedNewLine();
                if (i < unit.Enums.Count - 1)
                    NewLine();
            }

            NewLineIfNeeded();

            // Generate all the typedef declarations for the module.
            GenerateTypedefs();

            needsNewline = false;

            // Generate all the struct/class declarations for the module.
            for (var i = 0; i < unit.Classes.Count; ++i)
            {
                var @class = unit.Classes[i];

                if (@class.Ignore || @class.IsIncomplete)
                    continue;

                if (@class.IsOpaque)
                    continue;

                GenerateClass(@class);
                needsNewline = true;

                if (i < unit.Classes.Count - 1)
                    NewLine();
            }

            if (unit.HasFunctions)
            {
                if (needsNewline)
                    NewLine();

                GenerateFunctions();
            }

            PopIndent();
        }

        public void GenerateTypedefs()
        {
            foreach (var typedef in unit.Typedefs)
            {
                if (typedef.Ignore)
                    continue;

                if (!GenerateTypedef(typedef))
                    continue;

                NewLine();
            }
        }

        public void GenerateFunctions()
        {
            WriteLine("public ref class {0}{1}", SafeIdentifier(Library.Name),
                      unit.FileNameWithoutExtension);
            WriteLine("{");
            WriteLine("public:");
            PushIndent();

            // Generate all the function declarations for the module.
            foreach (var function in unit.Functions)
            {
                GenerateFunction(function);
            }

            PopIndent();
            WriteLine("};");
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
                Class baseClass = null;

                if (@class.HasBaseClass)
                    baseClass = @class.Bases[0].Class;

                var hasRefBase = baseClass != null && baseClass.IsRefType
                    && !baseClass.Ignore;

                var hasIgnoredBase = baseClass != null && baseClass.Ignore;

                if (!@class.HasBase || !hasRefBase || hasIgnoredBase)
                {
                    PushIndent();
                    WriteLine("property {0} NativePtr;", nativeType);
                    PopIndent();
                    NewLine();
                }
            }

            GenerateClassConstructors(@class, nativeType);

            GenerateClassFields(@class);

            // Generate a property for each field if class is not value type
            if (@class.IsRefType)
                GenerateClassProperties(@class);

            GenerateClassEvents(@class);
            GenerateClassMethods(@class);

            WriteLine("};");
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

            PushIndent();
            foreach (var field in @class.Fields)
            {
                if (field.Ignore) continue;

                GenerateDeclarationCommon(field);
                if (@class.IsUnion)
                    WriteLine("[FieldOffset({0})]", field.Offset);
                WriteLine("{0} {1};", field.Type, SafeIdentifier(field.Name));
            }
            PopIndent();

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
        }

        public void GenerateClassEvents(Class @class)
        {
            PushIndent();
            foreach (var @event in @class.Events)
            {
                if (@event.Ignore) continue;

                var typePrinter = new CppTypePrinter(Driver.TypeDatabase, Library);

                var @params = GetEventParameters(@event);
                var args = typePrinter.VisitParameters(@params, hasNames: true);

                PopIndent();
                WriteLine("private:");
                PushIndent();

                var delegateName = string.Format("_{0}Delegate", @event.Name);
                WriteLine("delegate void {0}({1});", delegateName, args);
                WriteLine("{0}^ {0}Instance;", delegateName);

                WriteLine("void _{0}Raise({1});", @event.Name, args);
                WriteLine("{0} _{1};", @event.Type, @event.Name);

                PopIndent();
                WriteLine("public:");
                PushIndent();

                WriteLine("event {0} {1}", @event.Type, @event.Name);
                WriteStartBraceIndent();

                WriteLine("void add({0} evt);", @event.Type);
                WriteLine("void remove({0} evt);", @event.Type);

                var paramNames = @event.Parameters.Select(param => param.ToString()).
                    ToList();
                var parameters = string.Join(", ", paramNames);

                WriteLine("void raise({0});", parameters);
                WriteCloseBraceIndent();
            }
            PopIndent();
        }

        public void GenerateClassMethods(Class @class)
        {
            PushIndent();

            var staticMethods = new List<Method>();
            foreach (var method in @class.Methods)
            {
                if (CheckIgnoreMethod(@class, method))
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
                    Write(" : {0}", SafeIdentifier(@class.Bases[0].Class.Name));

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
                if (CheckIgnoreField(@class, field))
                    continue;

                GenerateDeclarationCommon(field);
                GenerateFieldProperty(field);
            }
            PopIndent();
        }

        public void GenerateFieldProperty(Field field)
        {
            var type = field.Type.Visit(Type.TypePrinter, field.QualifiedType.Qualifiers);

            WriteLine("property {0} {1};", type, field.Name);
        }

        public void GenerateMethod(Method method)
        {
            if (method.Ignore) return;

            if (method.Access != AccessSpecifier.Public)
                return;

            GenerateDeclarationCommon(method);

            if (method.IsStatic)
                Write("static ");

            if (method.Kind == CXXMethodKind.Constructor || method.Kind == CXXMethodKind.Destructor)
                Write("{0}(", SafeIdentifier(method.Name));
            else
                Write("{0} {1}(", method.ReturnType, SafeIdentifier(method.Name));

            GenerateMethodParameters(method);

            WriteLine(");");
        }

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (typedef.Ignore)
                return false;

            GenerateDeclarationCommon(typedef);

            FunctionType function;
            if (typedef.Type.IsPointerTo<FunctionType>(out function))
            {
                WriteLine("public {0};",
                    string.Format(TypePrinter.VisitDelegate(function),
                    SafeIdentifier(typedef.Name)));
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
            if (function.Ignore) return;
            GenerateDeclarationCommon(function);

            var retType = function.ReturnType.ToString();
            Write("static {0} {1}(", retType, SafeIdentifier(function.Name));

            for (int i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                Write("{0}", TypePrinter.VisitParameter(param));
                if (i < function.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(");");
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
                var I = @enum.Items[i];
                GenerateInlineSummary(I.Comment);
                if (I.ExplicitValue)
                    Write(String.Format("{0} = {1}", SafeIdentifier(I.Name), I.Value));
                else
                    Write(String.Format("{0}", SafeIdentifier(I.Name)));

                if (i < @enum.Items.Count - 1)
                    WriteLine(",");
            }
            PopIndent();

            NewLine();
            WriteLine("};");
        }
    }
}
