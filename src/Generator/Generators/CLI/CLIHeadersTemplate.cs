using System;
using System.Collections.Generic;

namespace Cxxi.Generators.CLI
{
    public class CLIHeadersTemplate : CLITextTemplate
    {
        public override string FileExtension { get { return "h"; } }

        protected override void Generate()
        {
            GenerateStart();

            WriteLine("#pragma once");
            NewLine();

            WriteLine("#include <{0}>", Module.IncludePath);
            NewLine();

            WriteLine("namespace {0}", SafeIdentifier(Library.Name));
            WriteLine("{");
            GenerateDeclarations();
            WriteLine("}");
        }

        public void GenerateForwardRefs()
        {
            // Use a set to remove duplicate entries.
            var forwardRefs = new HashSet<string>();

            foreach (var forwardRef in Module.ForwardReferences)
            {
                var printer = new CLIForwardRefeferencePrinter();
                forwardRefs.Add(forwardRef.Visit(printer));
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

            bool NeedsNewline = false;

            // Generate all the enum declarations for the module.
            for (int i = 0; i < Module.Enums.Count; ++i)
            {
                var E = Module.Enums[i];
                if (E.Ignore) continue;

                GenerateEnum(E);
                NeedsNewline = true;
                if (i < Module.Enums.Count - 1)
                    NewLine();
            }

            if (NeedsNewline)
                NewLine();

            NeedsNewline = false;

            // Generate all the typedef declarations for the module.
            for (var i = 0; i < Module.Typedefs.Count; ++i)
            {
                var T = Module.Typedefs[i];
                if (T.Ignore) continue;

                GenerateTypedef(T);
                NeedsNewline = true;

                if (i < Module.Typedefs.Count - 1)
                    NewLine();
            }

            if (NeedsNewline)
                NewLine();

            NeedsNewline = false;

            // Generate all the struct/class declarations for the module.
            for (var i = 0; i < Module.Classes.Count; ++i)
            {
                var @class = Module.Classes[i];

                if (@class.Ignore || @class.IsIncomplete)
                    continue;

                if (@class.IsOpaque)
                    continue;

                GenerateClass(@class);
                NeedsNewline = true;

                if (i < Module.Classes.Count - 1)
                    NewLine();
            }

            if (Module.HasFunctions)
            {
                if (NeedsNewline)
                    NewLine();

                WriteLine("public ref class {0}{1}", SafeIdentifier(Library.Name),
                    Module.FileNameWithoutExtension);
                WriteLine("{");
                WriteLine("public:");
                PushIndent();

                // Generate all the function declarations for the module.
                foreach (var function in Module.Functions)
                {
                    GenerateFunction(function);
                }

                PopIndent();
                WriteLine("};");
            }

            PopIndent();
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
                WriteLine("[StructLayout(LayoutKind.Explicit)]");
            Write("public ");

            if (@class.IsValueType)
                Write("value struct ");
            else
                Write("ref class ");

            Write("{0}", SafeIdentifier(@class.Name));

            if (@class.IsOpaque)
            {
                WriteLine(";");
                return;
            }

            if (@class.HasBase)
                Write(" : {0}", SafeIdentifier(@class.Bases[0].Class.Name));

            WriteLine(string.Empty);
            WriteLine("{");
            WriteLine("public:");

            if (!@class.IsValueType)
            {
                PushIndent();
                var nativeType = string.Format("::{0}*", @class.OriginalName);
                WriteLine("property {0} NativePtr;", nativeType);
                PopIndent();
                NewLine();

                // Output a default constructor that takes the native pointer.
                PushIndent();
                WriteLine("{0}({1} native);", SafeIdentifier(@class.Name), nativeType);
                PopIndent();
            }

            if (@class.IsValueType)
            {
                PushIndent();
                foreach(var field in @class.Fields)
                {
                    if (field.Ignore) continue;

                    GenerateDeclarationCommon(field);
                    if (@class.IsUnion)
                        WriteLine("[FieldOffset({0})]", field.Offset);
                    WriteLine("{0} {1};", field.Type, SafeIdentifier(field.Name));
                }
                PopIndent();
            }

            PushIndent();
            foreach (var method in @class.Methods)
            {
                if (CheckIgnoreMethod(@class, method))
                    continue;

                GenerateDeclarationCommon(method);
                GenerateMethod(method);
            }
            PopIndent();

            WriteLine("};");
        }

        public void GenerateMethod(Method method)
        {
            if (method.Ignore) return;

            if (method.Access != AccessSpecifier.Public)
                return;

            GenerateDeclarationCommon(method);

            if (method.Kind == CXXMethodKind.Constructor || method.Kind == CXXMethodKind.Destructor)
                Write("{0}(", SafeIdentifier(method.Name));
            else
                Write("{0} {1}(", method.ReturnType, SafeIdentifier(method.Name));

            for (int i = 0; i < method.Parameters.Count; ++i)
            {
                var param = method.Parameters[i];
                Write("{0}", TypeSig.GetArgumentString(param));
                if (i < method.Parameters.Count - 1)
                    Write(", ");
            }

            WriteLine(");");
        }

        public void GenerateTypedef(TypedefDecl typedef)
        {
            if (typedef.Ignore) return;
            GenerateDeclarationCommon(typedef);

            FunctionType func;
            TagType tag;

            //if (T.Type.IsPointerToPrimitiveType(PrimitiveType.Void)
            //  || T.Type.IsPointerTo<TagType>(out tag))
            //{
            //  WriteLine("public class " + SafeIdentifier(T.Name) + @" { }");
            //  NewLine();
            //}
            //else
            if (typedef.Type.IsPointerTo<FunctionType>(out func))
            {
                WriteLine("public {0};",
                    string.Format(TypeSig.ToDelegateString(func),
                    SafeIdentifier(typedef.Name)));
            }
            else if (typedef.Type.IsEnumType())
            {
                // Already handled in the parser.
            }
            else
            {
                Console.WriteLine("Unhandled typedef type: {0}", typedef);
            }
        }

        public void GenerateFunction(Function function)
        {
            if (function.Ignore) return;
            GenerateDeclarationCommon(function);

            Write("static {0} {1}(", function.ReturnType, SafeIdentifier(function.Name));

            for (int i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];
                Write("{0}", TypeSig.GetArgumentString(param));
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
            if (@enum.Ignore) return;
            GenerateDeclarationCommon(@enum);

            if (@enum.Modifiers.HasFlag(Enumeration.EnumModifiers.Flags))
                WriteLine("[System::Flags]");

            Write("public enum struct {0}", SafeIdentifier(@enum.Name));

            if (@enum.BuiltinType.Type != PrimitiveType.Int32)
                WriteLine(" : {0}", TypeSig.VisitPrimitiveType(@enum.BuiltinType.Type));
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
