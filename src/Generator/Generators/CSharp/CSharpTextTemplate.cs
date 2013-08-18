using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CppSharp.AST;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public static class Helpers
    {
        // from https://github.com/mono/mono/blob/master/mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
        private static readonly string[] Keywords = new string[]
            {
                "abstract", "event", "new", "struct", "as", "explicit", "null", "switch",
                "base", "extern", "this", "false", "operator", "throw", "break", "finally",
                "out", "true", "fixed", "override", "try", "case", "params", "typeof",
                "catch", "for", "private", "foreach", "protected", "checked", "goto",
                "public", "unchecked", "class", "if", "readonly", "unsafe", "const",
                "implicit", "ref", "continue", "in", "return", "using", "virtual", "default",
                "interface", "sealed", "volatile", "delegate", "internal", "do", "is",
                "sizeof", "while", "lock", "stackalloc", "else", "static", "enum",
                "namespace", "object", "bool", "byte", "float", "uint", "char", "ulong",
                "ushort", "decimal", "int", "sbyte", "short", "double", "long", "string",
                "void", "partial", "yield", "where"
            };

        public static string GeneratedIdentifier(string id)
        {
            return "_" + id;
        }

        public static string SafeIdentifier(string id)
        {
            id = new string(((IEnumerable<char>)id)
                .Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            return Keywords.Contains(id) ? "@" + id : id;
        }

        public static string ToCSharpCallConv(CallingConvention convention)
        {
            switch (convention)
            {
                case CallingConvention.Default:
                    return "Winapi";
                case CallingConvention.C:
                    return "Cdecl";
                case CallingConvention.StdCall:
                    return "StdCall";
                case CallingConvention.ThisCall:
                    return "ThisCall";
                case CallingConvention.FastCall:
                    return "FastCall";
            }

            return "Winapi";
        }

        public static string InstanceIdentifier
        {
            get { return GeneratedIdentifier("Instance"); }
        }
    }

    public class CSharpBlockKind
    {
        private const int FIRST = BlockKind.LAST + 1000;
        public const int Usings = FIRST + 1;
        public const int Namespace = FIRST + 2;
        public const int Enum = FIRST + 3;
        public const int Typedef = FIRST + 4;
        public const int Class = FIRST + 5;
        public const int InternalsClass = FIRST + 6;
        public const int InternalsClassMethod = FIRST + 7;
        public const int InternalsClassField = FIRST + 15;
        public const int Functions = FIRST + 8;
        public const int Function = FIRST + 9;
        public const int Method = FIRST + 10;
        public const int Event = FIRST + 11;
        public const int Variable = FIRST + 12;
        public const int Property = FIRST + 13;
        public const int Field = FIRST + 14;
        public const int VTableDelegate = FIRST + 16;
        public const int Region = FIRST + 17;
    }

    public class CSharpTextTemplate : Template
    {
        public CSharpTypePrinter TypePrinter { get; private set; }

        public override string FileExtension
        {
            get { return "cs"; }
        }

        public CSharpTextTemplate(Driver driver, TranslationUnit unit,
            CSharpTypePrinter typePrinter)
            : base(driver, unit)
        {
            TypePrinter = typePrinter;
        }

        #region Identifiers

        public string QualifiedIdentifier(Declaration decl)
        {
            var names = new List<string> { decl.Name };

            var ctx = decl.Namespace;
            while (ctx != null)
            {
                if (!string.IsNullOrWhiteSpace(ctx.Name))
                    names.Add(ctx.Name);
                ctx = ctx.Namespace;
            }

            if (Options.GenerateLibraryNamespace)
                names.Add(Options.OutputNamespace);

            names.Reverse();
            return string.Join(".", names);
        }

        public static string GeneratedIdentifier(string id)
        {
            return Helpers.GeneratedIdentifier(id);
        }

        public static string SafeIdentifier(string id)
        {
            return Helpers.SafeIdentifier(id);
        }

        #endregion

        public override void Process()
        {
            GenerateHeader();

            PushBlock(CSharpBlockKind.Usings);
            WriteLine("using System;");
            WriteLine("using System.Runtime.InteropServices;");
            WriteLine("using System.Security;");
            PopBlock(NewLineKind.BeforeNextBlock);

            if (Options.GenerateLibraryNamespace)
            {
                PushBlock(CSharpBlockKind.Namespace);
                WriteLine("namespace {0}", SafeIdentifier(Driver.Options.OutputNamespace));
                WriteStartBraceIndent();
            }

            GenerateDeclContext(TranslationUnit);

            if (Options.GenerateLibraryNamespace)
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateHeader()
        {
            PushBlock(BlockKind.Header);
            WriteLine("//----------------------------------------------------------------------------");
            WriteLine("// This is autogenerated code by CppSharp.");
            WriteLine("// Do not edit this file or all your changes will be lost after re-generation.");
            WriteLine("//----------------------------------------------------------------------------");
            PopBlock();
        }

        private void GenerateDeclContext(DeclarationContext context)
        {
            var isNamespace = context is Namespace;
            var isTranslationUnit = context is TranslationUnit;

            var shouldGenerateNamespace = isNamespace && !isTranslationUnit;

            if (shouldGenerateNamespace)
            {
                PushBlock(CSharpBlockKind.Namespace);
                WriteLine("namespace {0}", context.Name);
                WriteStartBraceIndent();
            }

            // Generate all the enum declarations.
            foreach (var @enum in context.Enums)
            {
                if (@enum.Ignore || @enum.IsIncomplete)
                    continue;

                GenerateEnum(@enum);
            }

            // Generate all the typedef declarations.
            foreach (var typedef in context.Typedefs)
            {
                if (typedef.Ignore) continue;

                GenerateTypedef(typedef);
            }

            // Generate all the struct/class declarations.
            foreach (var @class in context.Classes)
            {
                if (@class.Ignore || @class.IsIncomplete)
                    continue;

                if (@class.IsDependent)
                    continue;

                GenerateClass(@class);
            }

            if (context.HasFunctions)
            {
                PushBlock(CSharpBlockKind.Functions);
                WriteLine("public partial class {0}{1}", SafeIdentifier(Options.OutputNamespace),
                    TranslationUnit.FileNameWithoutExtension);
                WriteStartBraceIndent();

                PushBlock(CSharpBlockKind.InternalsClass);
                GenerateClassInternalHead();
                WriteStartBraceIndent();

                // Generate all the internal function declarations.
                foreach (var function in context.Functions)
                {
                    if (function.Ignore) continue;

                    GenerateInternalFunction(function);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);

                foreach (var function in context.Functions)
                {
                    if (function.Ignore) continue;

                    GenerateFunction(function);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            foreach (var @event in context.Events)
            {
                if (@event.Ignore) continue;

                GenerateEvent(@event);
            }

            foreach(var childNamespace in context.Namespaces)
                GenerateDeclContext(childNamespace);

            if (shouldGenerateNamespace)
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateDeclarationCommon(Declaration decl)
        {
            if (decl.Comment == null)
                return;

            GenerateSummary(decl.Comment.BriefText);
            GenerateDebug(decl);
        }

        public void GenerateDebug(Declaration decl)
        {
            if (Options.OutputDebug && !String.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public void GenerateSummary(string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return;

            PushBlock(BlockKind.BlockComment);
            WriteLine("/// <summary>");
            WriteLine("/// {0}", comment);
            WriteLine("/// </summary>");
            PopBlock();
        }

        public void GenerateInlineSummary(string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return;

            PushBlock(BlockKind.InlineComment);
            WriteLine("/// <summary>{0}</summary>", comment);
            PopBlock();
        }

        #region Classes

        public void GenerateClass(Class @class)
        {
            if (@class.Ignore || @class.IsIncomplete)
                return;

            PushBlock(CSharpBlockKind.Class);
            GenerateDeclarationCommon(@class);

            GenerateClassProlog(@class);

            NewLine();
            WriteStartBraceIndent();

            if (!@class.IsOpaque)
            {
                GenerateClassInternals(@class);

                GenerateDeclContext(@class);

                if (ShouldGenerateClassNativeField(@class))
                {
                    PushBlock(CSharpBlockKind.Field);
                    WriteLine("public global::System.IntPtr {0} {{ get; protected set; }}",
                        Helpers.InstanceIdentifier);
                    PopBlock(NewLineKind.BeforeNextBlock);
                }

                GenerateClassConstructors(@class);

                if (@class.IsValueType)
                    GenerateValueClassFields(@class);

                GenerateClassMethods(@class);
                GenerateClassVariables(@class);
                GenerateClassProperties(@class);

                if (Options.GenerateVirtualTables && @class.IsDynamic)
                    GenerateVTable(@class);
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateValueClassFields(Class @class)
        {
            GenerateClassFields(@class, field =>
            {
                var fieldClass = (Class) field.Namespace;
                if (!fieldClass.IsValueType)
                    return;
                GenerateClassField(field);
            });
        }

        public void GenerateClassInternalsFields(Class @class)
        {
            GenerateClassFields(@class, GenerateClassInternalsField);

            foreach (var prop in @class.Properties)
            {
                if (prop.Ignore || prop.Field == null)
                    continue;

                GenerateClassInternalsField(prop.Field);
            }
        }

        public void GenerateClassInternals(Class @class)
        {
            PushBlock(CSharpBlockKind.InternalsClass);
            WriteLine("[StructLayout(LayoutKind.Explicit, Size = {0})]",
                @class.Layout.Size);

            GenerateClassInternalHead(@class);
            WriteStartBraceIndent();

            var typePrinter = TypePrinter as CSharpTypePrinter;
            typePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            GenerateClassInternalsFields(@class);
            GenerateVTablePointers(@class);

            var functions = GatherClassInternalFunctions(@class);

            foreach (var function in functions)
            {
                GenerateInternalFunction(function);
            }

            typePrinter.PopContext();

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private static ISet<Function> GatherClassInternalFunctions(Class @class)
        {
            var functions = new HashSet<Function>();

            Action<Method> tryAddOverload = method =>
            {
                if (method.IsSynthetized)
                    return;

                if (method.IsPure)
                    return;

                if (method.IsProxy)
                    return;

                functions.Add(method);
            };

            foreach (var ctor in @class.Constructors)
            {
                if (ctor.IsMoveConstructor)
                    continue;

                tryAddOverload(ctor);
            }

            foreach (var method in @class.Methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method))
                    continue;

                if (method.IsConstructor)
                    continue;

                tryAddOverload(method);
            }

            foreach (var prop in @class.Properties)
            {
                if (prop.GetMethod != null)
                    tryAddOverload(prop.GetMethod);

                if (prop.SetMethod != null)
                    tryAddOverload(prop.SetMethod);
            }

            return functions;
        }

        List<string> GatherInternalParams(Function function, out CSharpTypePrinterResult retType)
        {
            var @params = new List<string>();

            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            var retParam = new Parameter { QualifiedType = function.ReturnType };
            retType = retParam.CSharpType(TypePrinter);

            var method = function as Method;
            if (method != null && !method.IsStatic)
            {
                @params.Add("global::System.IntPtr instance");

                if (method.IsConstructor && base.Options.IsMicrosoftAbi)
                    retType = "global::System.IntPtr";
            }

            foreach (var param in function.Parameters)
            {
                if (param.Kind == ParameterKind.OperatorParameter)
                    continue;

                var typeName = param.CSharpType(TypePrinter);
                string paramName = param.IsSynthetized ?
                    GeneratedIdentifier(param.Name) : SafeIdentifier(param.Name);
                @params.Add(string.Format("{0} {1}", typeName, paramName));
            }

            if (method != null && method.IsConstructor)
            {
                var @class = (Class) method.Namespace;
                if (Options.IsMicrosoftAbi && @class.Layout.HasVirtualBases)
                    @params.Add("int " + CSharpTextTemplate.GeneratedIdentifier("forBases"));
            }

            TypePrinter.PopContext();

            return @params;
        }

        private void GenerateClassInternalHead(Class @class = null)
        {
            Write("public ");

            if (@class != null && @class.HasBaseClass)
                Write("new ");

            WriteLine("struct Internal");
        }

        private void GenerateStructMarshalingFields(Class @class)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.Ignore)
                    continue;

                GenerateStructMarshalingFields(@base.Class);
            }

            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field)) continue;

                var nativeField = string.Format("{0}->{1}",
                    Helpers.GeneratedIdentifier("ptr"), field.OriginalName);

                var ctx = new CSharpMarshalContext(Driver)
                {
                    Kind = CSharpMarshalKind.NativeField,
                    ArgName = field.Name,
                    ReturnVarName = nativeField,
                    ReturnType = field.QualifiedType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                field.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};", field.Name, marshal.Context.Return);
            }
        }

        private void GenerateStructInternalMarshaling(Class @class)
        {
            var marshalVar = Helpers.GeneratedIdentifier("native");

            WriteLine("var {0} = new {1}.Internal();", marshalVar, @class.Name);
            GenerateStructInternalMarshalingFields(@class, marshalVar);

            WriteLine("return {0};", marshalVar);
        }

        private void GenerateStructInternalMarshalingFields(Class @class, string marshalVar)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass || @base.Class.Ignore)
                    continue;

                var baseClass = @base.Class;
                GenerateStructInternalMarshalingFields(baseClass, marshalVar);
            }

            foreach (var field in @class.Fields)
            {
                if (field.Ignore)
                    continue;

                GenerateStructInternalMarshalingField(field, marshalVar);
            }
        }

        private void GenerateStructInternalMarshalingField(Field field, string marshalVar)
        {
            var marshalCtx = new CSharpMarshalContext(Driver)
            {
                ArgName = field.Name,
            };

            var marshal = new CSharpMarshalManagedToNativePrinter(marshalCtx);
            field.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
               WriteLine(marshal.Context.SupportBefore);

            if (field.Type.IsPointer())
            {
                WriteLine("if ({0} != null)", field.Name);
                PushIndent();
            }

           WriteLine("{0}.{1} = {2};", marshalVar, field.OriginalName, marshal.Context.Return);

            if (field.Type.IsPointer())
                PopIndent();
        }

        public bool ShouldGenerateClassNativeField(Class @class)
        {
            if (!@class.IsRefType)
                return false;

            Class baseClass = null;

            if (@class.HasBaseClass)
                baseClass = @class.Bases[0].Class;

            var hasRefBase = baseClass != null && baseClass.IsRefType
                             && !baseClass.Ignore;

            var hasIgnoredBase = baseClass != null && baseClass.Ignore;

            return !@class.HasBase || !hasRefBase || hasIgnoredBase;
        }

        public void GenerateClassProlog(Class @class)
        {
            if (@class.IsUnion)
                WriteLine("[StructLayout(LayoutKind.Explicit)]");

            Write("public unsafe ");

            if (Options.GeneratePartialClasses)
                Write("partial ");

            Write(@class.IsValueType ? "struct " : "class ");
            Write("{0}", SafeIdentifier(@class.Name));

            var needsBase = @class.HasBaseClass && !@class.IsValueType
                && !@class.Bases[0].Class.IsValueType
                && !@class.Bases[0].Class.Ignore;

            if (needsBase || @class.IsRefType)
                Write(" : ");

            if (needsBase)
            {
                var qualifiedBase = QualifiedIdentifier(@class.Bases[0].Class);
                Write("{0}", qualifiedBase);

                if (@class.IsRefType)
                    Write(", ");
            }

            if (@class.IsRefType)
                Write("IDisposable");
        }

        public void GenerateClassFields(Class @class, Action<Field> action)
        {
            foreach (var @base in @class.Bases)
            {
                if (!@base.IsClass) continue;
                var baseClass = @base.Class;

                if (baseClass.Ignore)
                    continue;

                GenerateClassFields(baseClass, action);
            }

            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field)) continue;
                action(field);
            }
        }

        private void GenerateClassInternalsField(Field field)
        {
            PushBlock(CSharpBlockKind.Field);

            WriteLine("[FieldOffset({0})]", field.OffsetInBytes);

            var result = field.QualifiedType.CSharpType(TypePrinter);
            Write("public {0} {1}", result.Type, SafeIdentifier(field.OriginalName));

            if (!string.IsNullOrWhiteSpace(result.NameSuffix))
                Write(result.NameSuffix);

            WriteLine(";");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassField(Field field)
        {
            PushBlock(CSharpBlockKind.Field);

            GenerateDeclarationCommon(field);

            var @class = (Class) field.Namespace;
            if (@class.IsUnion)
                WriteLine("[FieldOffset({0})]", field.Offset);

            WriteLine("public {0} {1};", field.Type, SafeIdentifier(field.Name));

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #endregion

        private Tuple<string, string> GetDeclarationLibrarySymbol(IMangledDecl decl)
        {
            var library = Options.SharedLibraryName;

            if (!Options.CheckSymbols)
                goto Out;

            NativeLibrary nativeLib;
            if (!Driver.LibrarySymbols.FindLibraryBySymbol(decl.Mangled, out nativeLib))
                goto Out;

            library = Path.GetFileNameWithoutExtension(nativeLib.FileName);

            Out:
            return Tuple.Create(library, decl.Mangled);
        }

        private void GeneratePropertySetter<T>(T decl, Class @class)
            where T : Declaration, ITypedDecl
        {
            PushBlock(CSharpBlockKind.Method);
            WriteLine("set");
            WriteStartBraceIndent();

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = decl.QualifiedType
            };

            var ctx = new CSharpMarshalContext(Driver)
            {
                Parameter = param,
                ArgName = param.Name,
            };

            if (decl is Function)
            {
                var function = decl as Function;

                if (function.Parameters.Count == 0)
                    throw new NotSupportedException("Expected at least one parameter in setter");

                param.QualifiedType = function.Parameters[0].QualifiedType;

                var parameters = new List<Parameter> { param };
                GenerateInternalFunctionCall(function, parameters);
            }
            else if (decl is Field)
            {
                var field = decl as Field;

                WriteLine("var {0} = (Internal*){1}.ToPointer();",
                    Helpers.GeneratedIdentifier("ptr"), Helpers.InstanceIdentifier);

                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                Write("{0}->{1} = {2}", Helpers.GeneratedIdentifier("ptr"),
                    Helpers.SafeIdentifier(field.OriginalName), marshal.Context.Return);

                WriteLine(";");
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GeneratePropertyGetter<T>(T decl, Class @class)
            where T : Declaration, ITypedDecl
        {
            PushBlock(CSharpBlockKind.Method);
            WriteLine("get");
            WriteStartBraceIndent();

            if (decl is Function)
            {
                var function = decl as Function;
                GenerateInternalFunctionCall(function);
            }
            else if (decl is Field)
            {
                var field = decl as Field;

                WriteLine("var {0} = (Internal*){1}.ToPointer();",
                    Helpers.GeneratedIdentifier("ptr"), Helpers.InstanceIdentifier);

                var ctx = new CSharpMarshalContext(Driver)
                {
                    Kind = CSharpMarshalKind.NativeField,
                    ArgName = decl.Name,
                    ReturnVarName = string.Format("{0}->{1}", Helpers.GeneratedIdentifier("ptr"),
                        Helpers.SafeIdentifier(field.OriginalName)),
                    ReturnType = decl.QualifiedType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                decl.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }
            else if (decl is Variable)
            {
                var @var = decl as Variable;
                var libSymbol = GetDeclarationLibrarySymbol(@var);

                TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

                var location = string.Format("CppSharp.SymbolResolver.ResolveSymbol(\"{0}\", \"{1}\")",
                    libSymbol.Item1, libSymbol.Item2);

                WriteLine("var {0} = ({1}*){2};", Helpers.GeneratedIdentifier("ptr"),
                    @var.Type, location);

                TypePrinter.PopContext();

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ArgName = decl.Name,
                    ReturnVarName = "*" + Helpers.GeneratedIdentifier("ptr"),
                    ReturnType = new QualifiedType(var.Type)
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                decl.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClassMethods(Class @class)
        {
            var staticMethods = new List<Method>();
            foreach (var method in @class.Methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method))
                    continue;

                if (method.IsConstructor)
                    continue;

                if (method.IsStatic)
                {
                    staticMethods.Add(method);
                    continue;
                }

                GenerateMethod(method, @class);
            }

            foreach (var method in staticMethods)
            {
                GenerateMethod(method, @class);
            }
        }

        public void GenerateClassVariables(Class @class)
        {
            foreach (var variable in @class.Variables)
            {
                if (variable.Ignore) continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                var type = variable.Type;

                GenerateVariable(@class, type, variable);
            }
        }

        private void GenerateClassProperties(Class @class)
        {
            foreach (var prop in @class.Properties)
            {
                if (prop.Ignore) continue;

                PushBlock(CSharpBlockKind.Property);
                WriteLine("public {0} {1}", prop.Type, SafeIdentifier(prop.Name));
                WriteStartBraceIndent();

                if (prop.Field != null)
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.Field, @class);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.Field, @class);
                }
                else
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.GetMethod, @class);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.SetMethod, @class);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private void GenerateVariable(Class @class, Type type, Variable variable)
        {
            PushBlock(CSharpBlockKind.Variable);
            WriteLine("public static {0} {1}", type, SafeIdentifier(variable.Name));
            WriteStartBraceIndent();

            GeneratePropertyGetter(variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable, @class);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #region Virtual Tables

        public void GenerateVTable(Class @class)
        {
            var entries = VTables.GatherVTableMethodEntries(@class);
            entries = entries.Where(entry => !entry.Method.Ignore).ToList();

            if (entries.Count == 0)
                return;

            PushBlock(CSharpBlockKind.Region);
            WriteLine("#region Virtual table interop");
            NewLine();

            // Generate a delegate type for each method.
            foreach (var entry in entries)
            {
                var method = entry.Method;
                GenerateVTableMethodDelegates(@class, method);
            }

            const string Dictionary = "System.Collections.Generic.Dictionary";

            WriteLine("static IntPtr[] _OldVTables;");
            WriteLine("static IntPtr[] _NewVTables;");
            WriteLine("static IntPtr[] _Thunks;");
            WriteLine("static {0}<IntPtr, WeakReference> _References;", Dictionary);
            NewLine();

            GenerateVTableClassSetup(@class, Dictionary, entries);

            WriteLine("#endregion");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVTableClassSetup(Class @class, string Dictionary,
            List<VTableComponent> entries)
        {
            WriteLine("void SetupVTables(global::System.IntPtr instance)");
            WriteStartBraceIndent();

            WriteLine("var native = (Internal*)instance.ToPointer();");
            NewLine();

            WriteLine("if (_References == null)");
            WriteLineIndent("_References = new {0}<IntPtr, WeakReference>();", Dictionary);
            NewLine();

            WriteLine("if (_References.ContainsKey(instance))");
            WriteLineIndent("return;");

            NewLine();
            WriteLine("_References[instance] = new WeakReference(this);");
            NewLine();

            // Save the original vftable pointers.
            WriteLine("if (_OldVTables == null)");
            WriteStartBraceIndent();

            var vftables = @class.Layout.VFTables;
            WriteLine("_OldVTables = new IntPtr[{0}];", vftables.Count);

            var index = 0;
            foreach (var vfptr in vftables)
            {
                WriteLine("_OldVTables[{0}] = native->vfptr{0};", index++);
            }

            WriteCloseBraceIndent();
            NewLine();

            // Get the _Thunks
            WriteLine("if (_Thunks == null)");
            WriteStartBraceIndent();

            WriteLine("_Thunks = new IntPtr[{0}];", entries.Count);

            index = 0;
            foreach (var entry in entries)
            {
                var method = entry.Method;
                var delegateName = GetVTableMethodDelegateName(method);
                var delegateInstance = delegateName + "Instance";
                WriteLine("{0} += {1}Hook;", delegateInstance, delegateName);
                WriteLine("_Thunks[{0}] = Marshal.GetFunctionPointerForDelegate({1});",
                    index++, delegateInstance);
            }

            WriteCloseBraceIndent();
            NewLine();

            // Allocate new vtables if there are none yet.
            WriteLine("if (_NewVTables == null)");
            WriteStartBraceIndent();

            WriteLine("_NewVTables = new IntPtr[{0}];", vftables.Count);

            index = 0;
            foreach (var vfptr in vftables)
            {
                var size = vfptr.Layout.Components.Count;
                WriteLine("var vfptr = Marshal.AllocHGlobal({0} * IntPtr.Size);", size);
                WriteLine("_NewVTables[{0}] = vfptr;", index++);

                var entryIndex = 0;
                foreach (var entry in vfptr.Layout.Components)
                {
                    var offsetInBytes = VTables.GetVTableComponentIndex(@class, entry)*IntPtr.Size;
                    WriteLine("*(IntPtr*)(vfptr + {0}) = _Thunks[{1}];", offsetInBytes, entryIndex++);
                }
            }

            WriteCloseBraceIndent();
            NewLine();

            // Set the previous delegate instances pointers in the object
            // virtual table.
            index = 0;
            foreach (var vfptr in @class.Layout.VFTables)
                WriteLine("native->vfptr{0} = _NewVTables[{0}];", index++);

            WriteCloseBraceIndent();
            NewLine();
        }

        private void GenerateVTableManagedCall(Method method)
        {
            if (method.IsDestructor)
            {
                WriteLine("target.Dispose();");
                return;
            }

            var marshals = new List<string>();
            foreach (var param in method.Parameters)
            {
                if (param.Ignore)
                    continue;

                if (param.Kind == ParameterKind.HiddenStructureReturn)
                    continue;

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ReturnType = param.QualifiedType,
                    ReturnVarName = SafeIdentifier(param.Name)
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                marshals.Add(marshal.Context.Return);
            }

            var hasReturn = !method.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void)
                && !method.HasHiddenStructParameter;

            if (hasReturn)
                Write("var _ret = ");

            WriteLine("target.{0}({1});", method.Name, string.Join(", ", marshals));

            // TODO: Handle hidden structure return types.

            if (hasReturn)
            {
                var param = new Parameter
                {
                    Name = "_ret",
                    QualifiedType = method.ReturnType
                };

                // Marshal the managed result to native
                var ctx = new CSharpMarshalContext(Driver)
                {
                    ArgName = "_ret",
                    Parameter = param,
                    Function = method
                };

                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                method.ReturnType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }
        }

        private void GenerateVTableMethodDelegates(Class @class, Method method)
        {
            PushBlock(CSharpBlockKind.VTableDelegate);

            WriteLine("[SuppressUnmanagedCodeSecurity]");
            WriteLine("[UnmanagedFunctionPointerAttribute(CallingConvention.{0})]",
                Helpers.ToCSharpCallConv(method.CallingConvention));

            CSharpTypePrinterResult retType;
            var @params = GatherInternalParams(method, out retType);

            var delegateName = GetVTableMethodDelegateName(method);
            WriteLine("delegate {0} {1}({2});", retType, delegateName,
                string.Join(", ", @params));

            WriteLine("static {0} {0}Instance;", delegateName);
            NewLine();

            WriteLine("static {0} {1}Hook({2})", retType, delegateName,
                string.Join(", ", @params));
            WriteStartBraceIndent();

            WriteLine("if (!_References.ContainsKey(instance))");
            WriteLineIndent("throw new Exception(\"No managed instance was found\");");
            NewLine();

            WriteLine("var target = ({0}) _References[instance].Target;", @class.Name);
            GenerateVTableManagedCall(method);

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.Always);
        }

        public string GetVTableMethodDelegateName(Method method)
        {
            return string.Format("_{0}Delegate", GetFunctionIdentifier(method));
        }

        public void GenerateVTablePointers(Class @class)
        {
            var index = 0;
            foreach (var info in @class.Layout.VFTables)
            {
                PushBlock(CSharpBlockKind.InternalsClassField);

                WriteLine("[FieldOffset({0})]", info.VFPtrFullOffset);
                WriteLine("public global::System.IntPtr vfptr{0};",
                    info.VFPtrFullOffset, index++);

                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        #endregion

        #region Events

        private string delegateName;
        private string delegateInstance;
        private string delegateRaise;

        private void GenerateEvent(Event @event)
        {
            PushBlock(CSharpBlockKind.Event);
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            delegateInstance = Helpers.GeneratedIdentifier(@event.OriginalName);
            delegateName = delegateInstance + "Delegate";
            delegateRaise = delegateInstance + "RaiseInstance";

            WriteLine("[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]");
            WriteLine("delegate void {0}({1});", delegateName, args);
            WriteLine("{0} {1};", delegateName, delegateRaise);
            NewLine();

            WriteLine("{0} {1};", @event.Type, delegateInstance);
            WriteLine("public event {0} {1}", @event.Type, Helpers.SafeIdentifier(@event.Name));
            WriteStartBraceIndent();

            GenerateEventAdd(@event);
            NewLine();

            GenerateEventRemove(@event);

            WriteCloseBraceIndent();
            NewLine();

            GenerateEventRaiseWrapper(@event);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateEventAdd(Event @event)
        {
            WriteLine("add");
            WriteStartBraceIndent();

            WriteLine("if ({0} == null)", delegateRaise);
            WriteStartBraceIndent();

            WriteLine("{0} = new {1}(_{2}Raise);", delegateRaise, delegateName, @event.Name);

            WriteLine("var {0} = Marshal.GetFunctionPointerForDelegate({1}).ToPointer();",
                Helpers.GeneratedIdentifier("ptr"), delegateInstance);

            // Call type map here.

            //WriteLine("((::{0}*)NativePtr)->{1}.Connect(_fptr);", @class.QualifiedOriginalName,
            //    @event.OriginalName);

            WriteCloseBraceIndent();

            WriteLine("{0} = ({1})System.Delegate.Combine({0}, value);",
                delegateInstance, @event.Type);

            WriteCloseBraceIndent();
        }

        private void GenerateEventRemove(Event @event)
        {
            WriteLine("remove");
            WriteStartBraceIndent();

            WriteLine("{0} = ({1})System.Delegate.Remove({0}, value);",
                delegateInstance, @event.Type);

            WriteCloseBraceIndent();
        }

        private void GenerateEventRaiseWrapper(Event @event)
        {
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            WriteLine("void _{0}Raise({1})", @event.Name, args);
            WriteStartBraceIndent();

            var returns = new List<string>();
            foreach (var param in @event.Parameters)
            {
                var ctx = new CSharpMarshalContext(Driver)
                {
                    ReturnVarName = Helpers.SafeIdentifier(param.Name),
                    ReturnType = param.QualifiedType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);

                returns.Add(marshal.Context.Return);
            }

            WriteLine("if ({0} != null)", delegateInstance);
            WriteStartBraceIndent();
            WriteLine("{0}({1});", delegateInstance, string.Join(", ", returns));
            WriteCloseBraceIndent();

            WriteCloseBraceIndent();
        }

        #endregion

        #region Constructors

        public void GenerateClassConstructors(Class @class)
        {
            // Output a default constructor that takes the native pointer.
            GenerateNativeConstructor(@class);

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor))
                    continue;

                GenerateMethod(ctor, @class);
            }

            if (@class.IsRefType)
                GenerateDisposeMethods(@class);
        }

        private void GenerateDisposeMethods(Class @class)
        {
            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;

            // Generate the IDispose Dispose() method.
            if (!hasBaseClass)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("public void Dispose()");
                WriteStartBraceIndent();

                WriteLine("Dispose(disposing: true);");
                WriteLine("GC.SuppressFinalize(this);");

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate Dispose(bool) method
            PushBlock(CSharpBlockKind.Method);
            if (@class.IsValueType)
            {
                this.Write("private ");
            }
            else
            {
                this.Write("protected ");
                this.Write(hasBaseClass ? "override " : "virtual ");
            }

            WriteLine("void Dispose(bool disposing)");
            WriteStartBraceIndent();

            if (ShouldGenerateClassNativeField(@class))
                WriteLine("Marshal.FreeHGlobal({0});", Helpers.InstanceIdentifier);

            if (hasBaseClass)
                WriteLine("base.Dispose(disposing);");

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNativeConstructor(Class @class)
        {
            PushBlock(CSharpBlockKind.Method);
            WriteLine("internal {0}({1}.Internal* native)", SafeIdentifier(@class.Name),
                @class.Name);
            WriteLineIndent(": this(new global::System.IntPtr(native))");
            WriteStartBraceIndent();
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(CSharpBlockKind.Method);
            WriteLine("internal {0}({1}.Internal native)", SafeIdentifier(@class.Name),
                @class.Name);
            WriteLineIndent(": this(&native)");
            WriteStartBraceIndent();
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(CSharpBlockKind.Method);
            WriteLine("internal {0}(global::System.IntPtr native)", SafeIdentifier(@class.Name));

            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;
            if (hasBaseClass)
                WriteLineIndent(": base(native)");

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                if (ShouldGenerateClassNativeField(@class))
                {
                    WriteLine("{0} = native;", Helpers.InstanceIdentifier);
                    if (Options.GenerateVirtualTables && @class.IsDynamic)
                        WriteLine("SetupVTables(_Instance);");
                }
            }
            else
            {
                WriteLine("var {0} = (Internal*){1}.ToPointer();",
                    Helpers.GeneratedIdentifier("ptr"), "native");
                GenerateStructMarshalingFields(@class);
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            if (@class.IsValueType)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("internal Internal ToInternal()");
                WriteStartBraceIndent();
                GenerateStructInternalMarshaling(@class);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);

                PushBlock(CSharpBlockKind.Method);
                WriteLine("internal void FromInternal(Internal* native)");
                WriteStartBraceIndent();
                WriteLine("var {0} = {1};", Helpers.GeneratedIdentifier("ptr"), "native");
                GenerateStructMarshalingFields(@class);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private bool GenerateClassConstructorBase(Class @class, Method method)
        {
            var hasBase = @class.HasBaseClass && !@class.Bases[0].Class.Ignore;

            if (hasBase && !@class.IsValueType)
            {
                PushIndent();
                Write(": this(");

                if (method != null)
                    Write("IntPtr.Zero");
                else
                    Write("native");

                WriteLine(")");
                PopIndent();
            }

            if (@class.IsValueType)
                WriteLineIndent(": this()");

            return hasBase;
        }

        #endregion

        #region Methods / Functions

        public void GenerateFunction(Function function)
        {
            PushBlock(CSharpBlockKind.Function);
            GenerateDeclarationCommon(function);

            var functionName = GetFunctionIdentifier(function);
            Write("public static {0} {1}(", function.ReturnType, functionName);
            GenerateMethodParameters(function);
            WriteLine(")");
            WriteStartBraceIndent();

            GenerateInternalFunctionCall(function);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethod(Method method, Class @class)
        {
            PushBlock(CSharpBlockKind.Method);
            GenerateDeclarationCommon(method);

            Write("public ");

            var isBuiltinOperator = false;
            if (method.IsOperator)
                GetOperatorIdentifier(method.OperatorKind, out isBuiltinOperator);

            if (method.IsVirtual && !method.IsOverride)
                Write("virtual ");

            if (method.IsStatic || (method.IsOperator && isBuiltinOperator))
                Write("static ");

            if (method.IsOverride)
                Write("override ");

            var functionName = GetFunctionIdentifier(method);

            if (method.IsConstructor || method.IsDestructor)
                Write("{0}(", functionName);
            else
                Write("{0} {1}(", method.OriginalReturnType, functionName);

            GenerateMethodParameters(method);

            WriteLine(")");

            if (method.Kind == CXXMethodKind.Constructor)
                GenerateClassConstructorBase(@class, method);

            WriteStartBraceIndent();

            if (method.IsProxy)
                goto SkipImpl;

            if (@class.IsRefType)
            {
                if (method.IsConstructor)
                {
                    GenerateClassConstructor(method, @class);
                }
                else if (method.IsOperator)
                {
                    GenerateOperator(method, @class);
                }
                else
                {
                    GenerateInternalFunctionCall(method);
                }
            }
            else if (@class.IsValueType)
            {
                if (method.IsConstructor)
                {
                    GenerateInternalFunctionCall(method);
                }
                else if (method.IsOperator)
                {
                    GenerateOperator(method, @class);
                }
                else
                {
                    GenerateInternalFunctionCall(method);
                }
            }

            SkipImpl:

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private static string GetOperatorOverloadPair(CXXOperatorKind kind)
        {
            switch (kind)
            {
            case CXXOperatorKind.EqualEqual:
                return "!=";
            case CXXOperatorKind.ExclaimEqual:
                return "==";

            case CXXOperatorKind.Less:
                return ">";
            case CXXOperatorKind.Greater:
                return "<";

            case CXXOperatorKind.LessEqual:
                return ">=";
            case CXXOperatorKind.GreaterEqual:
                return "<=";

            default:
                throw new NotSupportedException();
            }
        }

        private void GenerateOperator(Method method, Class @class)
        {
            if (method.IsSynthetized)
            {
                var @operator = GetOperatorOverloadPair(method.OperatorKind);

                WriteLine("return !({0} {1} {2});", method.Parameters[0].Name,
                          @operator, method.Parameters[1].Name);
                return;
            }

            GenerateInternalFunctionCall(method);
        }

        private void GenerateClassConstructor(Method method, Class @class)
        {
            var @params = GenerateFunctionParamsMarshal(method.Parameters, method);

            WriteLine("{0} = Marshal.AllocHGlobal({1});", Helpers.InstanceIdentifier,
                @class.Layout.Size);
            Write("Internal.{0}({1}", GetFunctionNativeIdentifier(method),
                Helpers.InstanceIdentifier);
            if (@params.Any())
                Write(", ");
            GenerateFunctionParams(@params);
            WriteLine(");");
            if (Options.GenerateVirtualTables && @class.IsDynamic)
                WriteLine("SetupVTables(_Instance);");
        }

        public void GenerateInternalFunctionCall(Function function,
            List<Parameter> parameters = null)
        {
            if (parameters == null)
                parameters = function.Parameters;

            var functionName = string.Format("Internal.{0}",
                GetFunctionNativeIdentifier(function));
            GenerateFunctionCall(functionName, parameters, function);
        }

        public void GenerateFunctionCall(string functionName, List<Parameter> parameters,
            Function function)
        {
            if (function.IsPure)
            {
                WriteLine("throw new System.NotImplementedException();");
                return;
            }

            var retType = function.OriginalReturnType;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            var isValueType = false;
            var needsInstance = false;

            var method = function as Method;
            if (method != null)
            {
                var @class = (Class) method.Namespace;
                isValueType = @class.IsValueType;

                needsInstance = !method.IsStatic;

                if (method.IsOperator)
                {
                    bool isBuiltin;
                    GetOperatorIdentifier(method.OperatorKind, out isBuiltin);
                    needsInstance &= !isBuiltin;
                }
            }

            var needsFixedThis = needsInstance && isValueType;
            var @params = GenerateFunctionParamsMarshal(parameters, function);

            if (function.HasHiddenStructParameter)
            {
                var hiddenParam = function.Parameters[0];
                if (hiddenParam.Kind != ParameterKind.HiddenStructureReturn)
                    throw new NotSupportedException("Expected hidden structure parameter kind");

                Class retClass = null;
                hiddenParam.Type.Desugar().IsTagDecl(out retClass);
                WriteLine("var ret = new {0}.Internal();", retClass.OriginalName);
            }

            var names = new List<string>();
            foreach (var param in @params)
            {
                var name = string.Empty;
                if (param.Context != null
                    && !string.IsNullOrWhiteSpace(param.Context.ArgumentPrefix))
                    name += param.Context.ArgumentPrefix;

                name += Helpers.SafeIdentifier(param.Name);
                names.Add(name);
            }

            if (function.HasHiddenStructParameter)
            {
                var name = string.Format("new IntPtr(&ret)");
                names.Insert(0, name);
            }

            if (needsInstance)
            {
                names.Insert(0, needsFixedThis ? string.Format("new global::System.IntPtr(&{0})",
                    GeneratedIdentifier("instance")) : Helpers.InstanceIdentifier);
            }

            if (needsFixedThis)
            {
                //WriteLine("fixed({0}* {1} = &this)", @class.QualifiedName,
                //    GeneratedIdentifier("instance"));
                //WriteStartBraceIndent();
                WriteLine("var {0} = ToInternal();", Helpers.GeneratedIdentifier("instance"));
            }

            if (needsReturn && !function.HasHiddenStructParameter)
                Write("var ret = ");

            WriteLine("{0}({1});", functionName, string.Join(", ", names));

            var cleanups = new List<TextGenerator>();
            GenerateFunctionCallOutParams(@params, cleanups);

            foreach (var param in @params)
            {
                var context = param.Context;
                if (context == null) continue;

                if (!string.IsNullOrWhiteSpace(context.Cleanup))
                    cleanups.Add(context.Cleanup);
            }

            foreach (var cleanup in cleanups)
            {
                Write(cleanup);
            }

            if (needsFixedThis)
            {
                //    WriteCloseBraceIndent();
                WriteLine("FromInternal(&{0});", Helpers.GeneratedIdentifier("instance"));
            }

            if (needsReturn)
            {
                var ctx = new CSharpMarshalContext(Driver)
                {
                    ArgName = "ret",
                    ReturnVarName = "ret",
                    ReturnType = retType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                retType.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }
        }

        private void GenerateFunctionCallOutParams(IEnumerable<ParamMarshal> @params,
            ICollection<TextGenerator> cleanups)
        {
            foreach (var paramInfo in @params)
            {
                var param = paramInfo.Param;
                if (param.Usage != ParameterUsage.Out && param.Usage != ParameterUsage.InOut)
                    continue;

                var nativeVarName = paramInfo.Name;

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ArgName = nativeVarName,
                    ReturnVarName = nativeVarName,
                    ReturnType = param.QualifiedType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                param.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};", param.Name, marshal.Context.Return);

                if (!string.IsNullOrWhiteSpace(marshal.CSharpContext.Cleanup))
                    cleanups.Add(marshal.CSharpContext.Cleanup);
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
            public CSharpMarshalContext Context;
        }

        public void GenerateFunctionParams(List<ParamMarshal> @params)
        {
            var names = @params.Select(param => param.Name).ToList();
            Write(string.Join(", ", names));
        }

        public List<ParamMarshal> GenerateFunctionParamsMarshal(IEnumerable<Parameter> @params,
                                                                Function function = null)
        {
            var marshals = new List<ParamMarshal>();

            var paramIndex = 0;
            foreach (var param in @params)
            {
                if (param.Kind == ParameterKind.HiddenStructureReturn)
                    continue;

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
                return new ParamMarshal { Name = param.Name, Param = param };
            }

            var argName = "arg" + paramIndex.ToString(CultureInfo.InvariantCulture);
            var paramMarshal = new ParamMarshal { Name = argName, Param = param };

            if (param.Usage == ParameterUsage.Out)
            {
                var paramType = param.Type;

                Class @class;
                if (paramType.Desugar().IsTagDecl(out @class) && @class.IsRefType)
                {
                    WriteLine("{0} = new {1}();", param.Name, paramType);
                }
            }

            var ctx = new CSharpMarshalContext(Driver)
            {
                Parameter = param,
                ParameterIndex = paramIndex,
                ArgName = argName,
                Function = function
            };

            paramMarshal.Context = ctx;

            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            param.CSharpMarshalToNative(marshal);

            if (string.IsNullOrEmpty(marshal.Context.Return))
                throw new Exception("Cannot marshal argument of function");

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Write(marshal.Context.SupportBefore);

            WriteLine("var {0} = {1};", SafeIdentifier(argName), marshal.Context.Return);

            return paramMarshal;
        }

        static string GetParameterUsage(ParameterUsage usage)
        {
            switch (usage)
            {
                case ParameterUsage.Out:
                    return "out ";
                case ParameterUsage.InOut:
                    return "ref";
                default:
                    return string.Empty;
            }
        }

        private void GenerateMethodParameters(Function function)
        {
            var @params = new List<string>();

            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];

                if (param.Kind == ParameterKind.HiddenStructureReturn)
                    continue;

                @params.Add(string.Format("{0}{1} {2}", GetParameterUsage(param.Usage),
                    param.Type, SafeIdentifier(param.Name)));
            }

            Write(string.Join(", ", @params));
        }

        #endregion

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (typedef.Ignore)
                return false;

            GenerateDeclarationCommon(typedef);

            FunctionType function;
            TagType tag;

            if (typedef.Type.IsPointerToPrimitiveType(PrimitiveType.Void)
                || typedef.Type.IsPointerTo<TagType>(out tag))
            {
                PushBlock(CSharpBlockKind.Typedef);
                WriteLine("public class " + SafeIdentifier(typedef.Name) + @" { }");
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            else if (typedef.Type.IsPointerTo<FunctionType>(out function))
            {
                PushBlock(CSharpBlockKind.Typedef);
                WriteLine("public {0};",
                    string.Format(TypePrinter.VisitDelegate(function).Type,
                        SafeIdentifier(typedef.Name)));
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            else if (typedef.Type.IsEnumType())
            {
                // Already handled in the parser.
                return false;
            }
            else
            {
                Console.WriteLine("Unhandled typedef type: {0}", typedef);
                return false;
            }

            return true;
        }

        public void GenerateEnum(Enumeration @enum)
        {
            if (@enum.Ignore) return;

            PushBlock(CSharpBlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            if (@enum.IsFlags)
                WriteLine("[Flags]");

            Write("public enum {0}", SafeIdentifier(@enum.Name));

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int32)
                Write(" : {0}", typeName);

            NewLine();

            WriteStartBraceIndent();
            for (var i = 0; i < @enum.Items.Count; ++i)
            {
                var item = @enum.Items[i];
                GenerateInlineSummary(item.Comment);

                var value = @enum.GetItemValueAsString(item);
                Write(item.ExplicitValue
                          ? string.Format("{0} = {1}", SafeIdentifier(item.Name), value)
                          : string.Format("{0}", SafeIdentifier(item.Name)));

                if (i < @enum.Items.Count - 1)
                    Write(",");

                NewLine();
            }
            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public static string GetOperatorIdentifier(CXXOperatorKind kind,
            out bool isBuiltin)
        {
            isBuiltin = true;

            // These follow the order described in MSDN (Overloadable Operators).
            switch (kind)
            {
                // These unary operators can be overloaded
                case CXXOperatorKind.Plus: return "operator +";
                case CXXOperatorKind.Minus: return "operator -";
                case CXXOperatorKind.Exclaim: return "operator !";
                case CXXOperatorKind.Tilde: return "operator ~";
                case CXXOperatorKind.PlusPlus: return "operator ++";
                case CXXOperatorKind.MinusMinus: return "operator --";

                // These binary operators can be overloaded
                case CXXOperatorKind.Star: return "operator *";
                case CXXOperatorKind.Slash: return "operator /";
                case CXXOperatorKind.Percent: return "operator +";
                case CXXOperatorKind.Amp: return "operator &";
                case CXXOperatorKind.Pipe: return "operator |";
                case CXXOperatorKind.Caret: return "operator ^";
                case CXXOperatorKind.LessLess: return "operator <<";
                case CXXOperatorKind.GreaterGreater: return "operator >>";

                // The comparison operators can be overloaded
                case CXXOperatorKind.EqualEqual: return "operator ==";
                case CXXOperatorKind.ExclaimEqual: return "operator !=";
                case CXXOperatorKind.Less: return "operator <";
                case CXXOperatorKind.Greater: return "operator >";
                case CXXOperatorKind.LessEqual: return "operator <=";
                case CXXOperatorKind.GreaterEqual: return "operator >=";

                // Assignment operators cannot be overloaded
                case CXXOperatorKind.PlusEqual:
                case CXXOperatorKind.MinusEqual:
                case CXXOperatorKind.StarEqual:
                case CXXOperatorKind.SlashEqual:
                case CXXOperatorKind.PercentEqual:
                case CXXOperatorKind.AmpEqual:
                case CXXOperatorKind.PipeEqual:
                case CXXOperatorKind.CaretEqual:
                case CXXOperatorKind.LessLessEqual:
                case CXXOperatorKind.GreaterGreaterEqual:

                // The array indexing operator cannot be overloaded
                case CXXOperatorKind.Subscript:

                // The conditional logical operators cannot be overloaded
                case CXXOperatorKind.AmpAmp:
                case CXXOperatorKind.PipePipe:

                // These operators cannot be overloaded.
                case CXXOperatorKind.Equal:
                case CXXOperatorKind.Comma:
                case CXXOperatorKind.ArrowStar:
                case CXXOperatorKind.Arrow:
                case CXXOperatorKind.Call:
                case CXXOperatorKind.Conditional:
                case CXXOperatorKind.New:
                case CXXOperatorKind.Delete:
                case CXXOperatorKind.Array_New:
                case CXXOperatorKind.Array_Delete:
                    isBuiltin = false;
                    return "Operator" + kind.ToString();
            }

            throw new NotSupportedException();
        }

        public static string GetFunctionIdentifier(Function function)
        {
            var identifier = SafeIdentifier(function.Name);

            var method = function as Method;
            if (method == null || !method.IsOperator)
                return identifier;

            bool isBuiltin;
            identifier = GetOperatorIdentifier(method.OperatorKind, out isBuiltin);

            return identifier;
        }

        public static string GetFunctionNativeIdentifier(Function function)
        {
            var identifier = SafeIdentifier(function.Name);;

            var method = function as Method;
            if (method != null && method.IsOperator)
                identifier = "Operator" + method.OperatorKind.ToString();

            var overloads = function.Namespace.GetFunctionOverloads(function)
                .ToList();
            var index = overloads.IndexOf(function);

            if (index >= 0)
                identifier += index.ToString(CultureInfo.InvariantCulture);

            return identifier;
        }

        public void GenerateInternalFunction(Function function)
        {
            if (!function.IsProcessed || function.ExplicityIgnored)
                return;

            PushBlock(CSharpBlockKind.InternalsClassMethod);
            GenerateDeclarationCommon(function);
            WriteLine("[SuppressUnmanagedCodeSecurity]");

            string libName = Options.SharedLibraryName;

            if (Options.CheckSymbols)
            {
                NativeLibrary library;
                Driver.LibrarySymbols.FindLibraryBySymbol(function.Mangled, out library);

                libName = Path.GetFileNameWithoutExtension(library.FileName);
            }
            if (libName != null && libName.Length > 3 && libName.StartsWith("lib"))
            {
                libName = libName.Substring(3);
            }

            Write("[DllImport(\"{0}\", ", libName);

            var callConv = Helpers.ToCSharpCallConv(function.CallingConvention);
            WriteLine("CallingConvention = CallingConvention.{0},", callConv);

            WriteLineIndent("EntryPoint=\"{0}\")]", function.Mangled);

            if (function.ReturnType.Type.Desugar().IsPrimitiveType(PrimitiveType.Bool))
                WriteLine("[return: MarshalAsAttribute(UnmanagedType.I1)]");

            var @params = new List<string>();

            var typePrinter = TypePrinter as CSharpTypePrinter;
            typePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            var retParam = new Parameter { QualifiedType = function.ReturnType };
            var retType = retParam.CSharpType(typePrinter);

            var method = function as Method;
            if (method != null && !method.IsStatic)
            {
                @params.Add("global::System.IntPtr instance");

                if (method.IsConstructor && Options.IsMicrosoftAbi)
                    retType = "global::System.IntPtr";
            }

            for (var i = 0; i < function.Parameters.Count; ++i)
            {
                var param = function.Parameters[i];

                if (param.Kind == ParameterKind.OperatorParameter)
                    continue;

                var typeName = param.CSharpType(typePrinter);

                var paramName = param.IsSynthetized ?
                    GeneratedIdentifier(param.Name) : SafeIdentifier(param.Name);

                @params.Add(string.Format("{0} {1}", typeName, paramName));
            }

            if (method != null && method.IsConstructor)
            {
                var @class = method.Namespace as Class;
                if (Options.IsMicrosoftAbi && @class.Layout.HasVirtualBases)
                    @params.Add("int " + GeneratedIdentifier("forBases"));
            }

            WriteLine("public static extern {0} {1}({2});", retType,
                      GetFunctionNativeIdentifier(function),
                      string.Join(", ", @params));
            PopBlock(NewLineKind.BeforeNextBlock);

            typePrinter.PopContext();
        }
    }

    internal class SymbolNotFoundException : Exception
    {
        public SymbolNotFoundException(string msg) : base(msg)
        {}
    }
}