using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Util;
using CppSharp.AST;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;
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

        public static string SafeIdentifier(string id)
        {
            id = new string(id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
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
            get { return Generator.GeneratedIdentifier("Instance"); }
        }

        public static string GetAccess(AccessSpecifier accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case AccessSpecifier.Private:
                    return "internal ";
                case AccessSpecifier.Protected:
                    return "protected ";
                default:
                    return "public ";
            }
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
        public const int Interface = FIRST + 18;
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
            return Generator.GeneratedIdentifier(id);
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

                if (@class.IsInterface)
                    GenerateInterface(@class);
                else
                    GenerateClass(@class);
            }

            if (context.HasFunctions)
            {
                PushBlock(CSharpBlockKind.Functions);
                WriteLine("public unsafe partial class {0}{1}", SafeIdentifier(Options.OutputNamespace),
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
            if (decl.Comment != null)
            {
                GenerateComment(decl.Comment);
                GenerateDebug(decl);
            }
            foreach (Attribute attribute in decl.Attributes)
                WriteLine("[{0}({1})]", attribute.Type.FullName, attribute.Value);
        }

        public void GenerateDebug(Declaration decl)
        {
            if (Options.OutputDebug && !String.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public void GenerateComment(RawComment comment)
        {
            if (string.IsNullOrWhiteSpace(comment.BriefText))
                return;

            PushBlock(BlockKind.BlockComment);
            WriteLine("/// <summary>");
            foreach (string line in HtmlEncoder.HtmlEncode(comment.BriefText).Split(
                                        Environment.NewLine.ToCharArray()))
                WriteLine("/// <para>{0}</para>", line);
            WriteLine("/// </summary>");

            if (!string.IsNullOrWhiteSpace(comment.Text))
            {
                WriteLine("/// <remarks>");
                foreach (string line in HtmlEncoder.HtmlEncode(comment.Text).Split(
                                            Environment.NewLine.ToCharArray()))
                    WriteLine("/// <para>{0}</para>", line);
                WriteLine("/// </remarks>");
            }
            PopBlock();
        }

        public void GenerateInlineSummary(RawComment comment)
        {
            if (comment == null) return;

            if (string.IsNullOrWhiteSpace(comment.BriefText))
                return;

            PushBlock(BlockKind.InlineComment);
            WriteLine("/// <summary>{0}</summary>", comment.BriefText);
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
                    WriteLine("public global::System.IntPtr {0} {{ get; {1} set; }}",
                        Helpers.InstanceIdentifier, @class.IsValueType ? "private" : "protected");
                    PopBlock(NewLineKind.BeforeNextBlock);
                }

                GenerateClassMarshals(@class);
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

        private void GenerateClassMarshals(Class @class)
        {
            WriteLine("int CppSharp.Runtime.ICppMarshal.NativeDataSize");
            WriteStartBraceIndent();
            WriteLine("get {{ return {0}; }}", @class.Layout.DataSize);
            WriteCloseBraceIndent();
            NewLine();

            WriteLine("void CppSharp.Runtime.ICppMarshal.MarshalManagedToNative(global::System.IntPtr instance)");
            WriteStartBraceIndent();
            WriteCloseBraceIndent();
            NewLine();

            WriteLine("void CppSharp.Runtime.ICppMarshal.MarshalNativeToManaged(global::System.IntPtr instance)");
            WriteStartBraceIndent();
            WriteCloseBraceIndent();
            NewLine();
        }

        private void GenerateInterface(Class @class)
        {
            if (@class.Ignore || @class.IsIncomplete)
                return;

            PushBlock(CSharpBlockKind.Interface);
            GenerateDeclarationCommon(@class);

            GenerateClassProlog(@class);

            NewLine();
            WriteStartBraceIndent();

            GenerateDeclContext(@class);

            foreach (var method in @class.Methods.Where(m => !ASTUtils.CheckIgnoreMethod(m) &&
                                                             m.Access == AccessSpecifier.Public))
            {
                PushBlock(CSharpBlockKind.Method);
                GenerateDeclarationCommon(method);

                var functionName = GetFunctionIdentifier(method);

                Write("{0} {1}(", method.OriginalReturnType, functionName);

                Write(FormatMethodParameters(method.Parameters));

                WriteLine(");");

                PopBlock(NewLineKind.BeforeNextBlock);
            }
            foreach (var prop in @class.Properties.Where(p => !p.Ignore))
            {
                PushBlock(CSharpBlockKind.Property);
                var type = prop.Type;
                if (prop.Parameters.Count > 0 && prop.Type.IsPointerToPrimitiveType())
                    type = ((PointerType) prop.Type).Pointee;
                Write("{0} {1} {{ ", type, GetPropertyName(prop));
                if (prop.HasGetter)
                    Write("get; ");
                if (prop.HasSetter)
                    Write("set; ");

                WriteLine("}");
                PopBlock(NewLineKind.BeforeNextBlock);
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

                if (method.IsProxy)
                    return;

                functions.Add(method);
            };

            foreach (var ctor in @class.Constructors)
            {
                if (ctor.IsMoveConstructor)
                    continue;

                if (ctor.IsDefaultConstructor && !@class.HasNonTrivialDefaultConstructor)
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

            var retParam = new Parameter { QualifiedType = function.OriginalReturnType };
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

            for (int i = 0; i < @class.Fields.Count; i++)
            {
                var field = @class.Fields[i];
                if (ASTUtils.CheckIgnoreField(field)) continue;

                var nativeField = string.Format("{0}->{1}",
                    Generator.GeneratedIdentifier("ptr"), field.OriginalName);

                var ctx = new CSharpMarshalContext(Driver)
                {
                    Kind = CSharpMarshalKind.NativeField,
                    ArgName = field.Name,
                    ReturnVarName = nativeField,
                    ReturnType = field.QualifiedType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx) { VarSuffix = i };
                field.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0} = {1};", field.Name, marshal.Context.Return);
            }
        }

        private void GenerateStructInternalMarshaling(Class @class)
        {
            var marshalVar = Generator.GeneratedIdentifier("native");

            WriteLine("var {0} = new {1}.Internal();", marshalVar, QualifiedIdentifier(@class));
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

            Type type;
            Class @class;
            var isRef = field.Type.IsPointerTo(out type) &&
                !(type.IsTagDecl(out @class) && @class.IsValueType) &&
                !type.IsPrimitiveType();

            if (isRef)
            {
                WriteLine("if ({0} != null)", field.Name);
                WriteStartBraceIndent();
            }

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
               WriteLine(marshal.Context.SupportBefore);

           WriteLine("{0}.{1} = {2};", marshalVar, field.OriginalName, marshal.Context.Return);

            if (isRef)
                WriteCloseBraceIndent();
        }

        public bool ShouldGenerateClassNativeField(Class @class)
        {
            if (!@class.IsRefType)
                return false;

            Class baseClass = null;

            if (@class.HasBaseClass)
                baseClass = @class.Bases[0].Class;

            var hasRefBase = baseClass != null && baseClass.IsRefType && !baseClass.Ignore;

            var hasIgnoredBase = baseClass != null && baseClass.Ignore;

            return !@class.HasBase || !hasRefBase || hasIgnoredBase;
        }

        public void GenerateClassProlog(Class @class)
        {
            if (@class.IsUnion)
                WriteLine("[StructLayout(LayoutKind.Explicit)]");

            Write(Helpers.GetAccess(@class.Access));
            Write("unsafe ");

            if (Driver.Options.GenerateAbstractImpls && @class.IsAbstract)
                Write("abstract ");

            if (Options.GeneratePartialClasses)
                Write("partial ");

            Write(@class.IsInterface ? "interface " : (@class.IsValueType ? "struct " : "class "));
            Write("{0}", SafeIdentifier(@class.Name));

            var bases = new List<string>();

            var needsBase = @class.HasBaseClass && !@class.IsValueType
                && !@class.Bases[0].Class.IsValueType
                && !@class.Bases[0].Class.Ignore;

            if (needsBase)
            {
                bases.AddRange(
                    from @base in @class.Bases
                    where @base.IsClass
                    select QualifiedIdentifier(@base.Class));
            }

            if (@class.IsRefType)
                bases.Add("IDisposable");

            bases.Add("CppSharp.Runtime.ICppMarshal");

            if (bases.Count > 0)
                Write(" : {0}", string.Join(", ", bases));
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
            if (!Driver.Symbols.FindLibraryBySymbol(decl.Mangled, out nativeLib))
                goto Out;

            library = Path.GetFileNameWithoutExtension(nativeLib.FileName);

            Out:
            return Tuple.Create(library, decl.Mangled);
        }

        private void GeneratePropertySetter<T>(QualifiedType returnType, T decl, Class @class)
            where T : Declaration, ITypedDecl
        {
            if (!(decl is Function || decl is Field) )
            {
                return;
            }

            PushBlock(CSharpBlockKind.Method);
            WriteLine("set");

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
                if (function.IsPure && Driver.Options.GenerateAbstractImpls)
                {
                    Write("; ");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                WriteStartBraceIndent();
                if (function.Parameters.Count == 0)
                    throw new NotSupportedException("Expected at least one parameter in setter");

                param.QualifiedType = function.Parameters[0].QualifiedType;

                var method = function as Method;
                if (method != null && method.OperatorKind == CXXOperatorKind.Subscript)
                {
                    if (method.IsOverride && method.IsSynthetized)
                    {
                        GenerateVirtualTableFunctionCall(function, @class);
                    }
                    else
                    {
                        if (method.OperatorKind == CXXOperatorKind.Subscript)
                        {
                            GenerateIndexerSetter(returnType, method);                            
                        }
                        else
                        {
                            var parameters = new List<Parameter> { param };
                            GenerateInternalFunctionCall(function, parameters);
                        }
                    }
                }
                else
                {
                    var parameters = new List<Parameter> { param };
                    GenerateInternalFunctionCall(function, parameters); 
                }
                WriteCloseBraceIndent();
            }
            else if (decl is Field)
            {
                WriteStartBraceIndent();
                var field = decl as Field;

                WriteLine("var {0} = (Internal*){1}.ToPointer();",
                    Generator.GeneratedIdentifier("ptr"), Helpers.InstanceIdentifier);

                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                ctx.ReturnVarName = field.OriginalName;
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                Write("{0}->{1} = {2}", Generator.GeneratedIdentifier("ptr"),
                    Helpers.SafeIdentifier(field.OriginalName), marshal.Context.Return);

                WriteLine(";");
                WriteCloseBraceIndent();
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateIndexerSetter(QualifiedType returnType, Function function)
        {
            Type type;
            returnType.Type.IsPointerTo(out type);
            PrimitiveType primitiveType;
            string internalFunction = GetFunctionNativeIdentifier(function);
            if (type.IsPrimitiveType(out primitiveType))
            {
                WriteLine("*Internal.{0}({1}, {2}) = value;", internalFunction,
                    Helpers.InstanceIdentifier, function.Parameters[0].Name);
            }
            else
            {
                WriteLine("*({0}.Internal*) Internal.{1}({2}, {3}) = *({0}.Internal*) value.{2};",
                    type.ToString(), internalFunction,
                    Helpers.InstanceIdentifier, function.Parameters[0].Name);
            }
        }

        private void GeneratePropertyGetter<T>(T decl, Class @class)
            where T : Declaration, ITypedDecl
        {
            PushBlock(CSharpBlockKind.Method);
            Write("get");

            if (decl is Function)
            {
                var function = decl as Function;
                if (function.IsPure && Driver.Options.GenerateAbstractImpls)
                {
                    Write("; ");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }
                NewLine();
                WriteStartBraceIndent();
                var method = function as Method;
                if (method != null && method.IsOverride && method.IsSynthetized)
                {
                    GenerateVirtualTableFunctionCall(function, @class);
                }
                else
                {
                    bool isPrimitiveIndexer = function.OperatorKind == CXXOperatorKind.Subscript &&
                                              function.ReturnType.Type.IsPointerToPrimitiveType();
                    if (isPrimitiveIndexer)
                        TypePrinter.PushContext(CSharpTypePrinterContextKind.PrimitiveIndexer);
                    GenerateInternalFunctionCall(function);
                    if (isPrimitiveIndexer)
                        TypePrinter.PopContext();   
                }
            }
            else if (decl is Field)
            {
                NewLine();
                WriteStartBraceIndent();
                var field = decl as Field;

                WriteLine("var {0} = (Internal*){1}.ToPointer();",
                    Generator.GeneratedIdentifier("ptr"), Helpers.InstanceIdentifier);

                var ctx = new CSharpMarshalContext(Driver)
                {
                    Kind = CSharpMarshalKind.NativeField,
                    ArgName = decl.Name,
                    ReturnVarName = string.Format("{0}->{1}", Generator.GeneratedIdentifier("ptr"),
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
                NewLine();
                WriteStartBraceIndent();
                var @var = decl as Variable;
                var libSymbol = GetDeclarationLibrarySymbol(@var);

                TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

                var location = string.Format("CppSharp.SymbolResolver.ResolveSymbol(\"{0}\", \"{1}\")",
                    libSymbol.Item1, libSymbol.Item2);

                WriteLine("var {0} = ({1}*){2};", Generator.GeneratedIdentifier("ptr"),
                    @var.Type, location);

                TypePrinter.PopContext();

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ArgName = decl.Name,
                    ReturnVarName = "*" + Generator.GeneratedIdentifier("ptr"),
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
            foreach (var prop in @class.Properties.Where(p => !p.Ignore))
            {
                PushBlock(CSharpBlockKind.Property);
                var type = prop.Type;
                // if it's an indexer that returns an address use the real type because there is a setter anyway
                if (prop.Parameters.Count > 0 && prop.Type.IsPointerToPrimitiveType())
                    type = ((PointerType) prop.Type).Pointee;

                GenerateDeclarationCommon(prop);
                if (prop.ExplicitInterfaceImpl == null)
                {
                    Write(Helpers.GetAccess(prop.Access));
                    if (prop.IsStatic)
                        Write("static ");
                    if (prop.IsOverride)
                        Write("override ");
                    else if (prop.IsPure && Driver.Options.GenerateAbstractImpls)
                        Write("abstract ");
                    else if (prop.IsVirtual)
                        Write("virtual ");
                    WriteLine("{0} {1}", type, GetPropertyName(prop));
                }
                else
                {
                    WriteLine("{0} {1}.{2}", type, prop.ExplicitInterfaceImpl.Name, GetPropertyName(prop));
                }
                WriteStartBraceIndent();

                if (prop.Field != null)
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.Field, @class);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.Field.QualifiedType, prop.Field, @class);
                }
                else
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.GetMethod, @class);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.GetMethod.ReturnType, prop.SetMethod, @class);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private string GetPropertyName(Property prop)
        {
            return prop.Parameters.Count == 0 ? SafeIdentifier(prop.Name)
                : string.Format("this[{0}]", FormatMethodParameters(prop.Parameters));
        }

        private void GenerateVariable(Class @class, Type type, Variable variable)
        {
            PushBlock(CSharpBlockKind.Variable);
            WriteLine("public static {0} {1}", type, SafeIdentifier(variable.Name));
            WriteStartBraceIndent();

            GeneratePropertyGetter(variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable.QualifiedType, variable, @class);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #region Virtual Tables

        public List<VTableComponent> GetVTableMethodEntries(Class @class)
        {
            var entries = VTables.GatherVTableMethodEntries(@class);
            return entries.Where(e => !e.Method.Ignore ||
                @class.GetPropertyByConstituentMethod(e.Method) != null).ToList();
        }

        public List<VTableComponent> GetUniqueVTableMethodEntries(Class @class)
        {
            var uniqueEntries = new OrderedSet<VTableComponent>();
            foreach (var entry in GetVTableMethodEntries(@class))
                uniqueEntries.Add(entry);

            return uniqueEntries.ToList();
        }

        public void GenerateVTable(Class @class)
        {
            var entries = GetVTableMethodEntries(@class);
            if (entries.Count == 0)
                return;

            PushBlock(CSharpBlockKind.Region);
            WriteLine("#region Virtual table interop");
            NewLine();

            // Generate a delegate type for each method.
            foreach (var entry in GetUniqueVTableMethodEntries(@class))
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

            var uniqueEntries = new HashSet<VTableComponent>();

            index = 0;
            foreach (var entry in entries)
            {
                var method = entry.Method;
                var delegateName = GetVTableMethodDelegateName(method);
                var delegateInstance = delegateName + "Instance";
                if (uniqueEntries.Add(entry))
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
                WriteLine("var vfptr{0} = Marshal.AllocHGlobal({1} * IntPtr.Size);",
                    index, size);
                WriteLine("_NewVTables[{0}] = vfptr{0};", index);

                var entryIndex = 0;
                foreach (var entry in vfptr.Layout.Components)
                {
                    var offsetInBytes = VTables.GetVTableComponentIndex(@class, entry)
                        * IntPtr.Size;
                    WriteLine("*(IntPtr*)(vfptr{0} + {1}) = _Thunks[{2}];", index,
                        offsetInBytes, entryIndex++);
                }

                index++;
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

        private void GenerateVTableClassSetupCall(Class @class, bool addPointerGuard = false)
        {
            var entries = GetVTableMethodEntries(@class);
            if (Options.GenerateVirtualTables && @class.IsDynamic && entries.Count != 0)
            {
                // called from internal ctors which may have been passed an IntPtr.Zero
                if (addPointerGuard)
                {
                    WriteLine("if ({0} != global::System.IntPtr.Zero)", Helpers.InstanceIdentifier);
                    PushIndent();
                }
                WriteLine("SetupVTables({0});", Generator.GeneratedIdentifier("Instance"));
                if (addPointerGuard)
                    PopIndent();
            }
        }

        private void GenerateVTableManagedCall(Method method)
        {
            if (method.IsDestructor)
            {
                WriteLine("target.Dispose();");
                return;
            }

            var marshals = new List<string>();
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var param = method.Parameters[i];
                if (param.Ignore)
                    continue;

                if (param.Kind == ParameterKind.IndirectReturnType)
                    continue;

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ReturnType = param.QualifiedType,
                    ReturnVarName = SafeIdentifier(param.Name)
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx) { VarSuffix = i };
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                marshals.Add(marshal.Context.Return);
            }

            var hasReturn = !method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void);

            if (hasReturn)
                Write("var _ret = ");

            // HACK: because of the non-shared v-table entries bug we must look for the real method by name
            Method m = ((Class) method.Namespace).GetMethodByName(method.Name);
            if (m.IsGenerated)
            {
                WriteLine("target.{0}({1});", SafeIdentifier(method.Name), string.Join(", ", marshals));              
            }
            else
            {
                InvokeProperty(m, marshals);
            }

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
                method.OriginalReturnType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("return {0};", marshal.Context.Return);
            }
        }

        private void InvokeProperty(Declaration method, IEnumerable<string> marshals)
        {
            var property = ((Class) method.Namespace).Properties.FirstOrDefault(
                p => p.GetMethod == method);
            if (property == null)
            {
                property = ((Class) method.Namespace).Properties.First(
                    p => p.SetMethod == method);
                WriteLine("target.{0} = {1};", SafeIdentifier(property.Name),
                    string.Join(", ", marshals));
            }
            else
            {
                WriteLine("target.{0};", SafeIdentifier(property.Name));
            }
        }

        private void GenerateVTableMethodDelegates(Class @class, Method method)
        {
            PushBlock(CSharpBlockKind.VTableDelegate);

            var cleanSig = method.Signature.ReplaceLineBreaks("");
            cleanSig = Regex.Replace(cleanSig, @"\s+", " ");

            WriteLine("// {0}", cleanSig);
            WriteLine("[SuppressUnmanagedCodeSecurity]");
            WriteLine("[UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.{0})]",
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
            var nativeId = GetFunctionNativeIdentifier(method);

            // Trim '@' (if any) because '@' is valid only as the first symbol.
            nativeId = nativeId.Trim('@');

            return string.Format("_{0}Delegate", nativeId);
        }

        public void GenerateVTablePointers(Class @class)
        {
            var index = 0;
            foreach (var info in @class.Layout.VFTables)
            {
                PushBlock(CSharpBlockKind.InternalsClassField);

                WriteLine("[FieldOffset({0})]", info.VFPtrFullOffset);
                WriteLine("public global::System.IntPtr vfptr{0};", index++);

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
            PushBlock(CSharpBlockKind.Event, @event);
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            delegateInstance = Generator.GeneratedIdentifier(@event.OriginalName);
            delegateName = delegateInstance + "Delegate";
            delegateRaise = delegateInstance + "RaiseInstance";

            WriteLine("[UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]");
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
                Generator.GeneratedIdentifier("ptr"), delegateInstance);

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
            string className = @class.Name;
            string safeIdentifier = SafeIdentifier(className);
            if (@class.Access == AccessSpecifier.Private && className.EndsWith("Internal"))
            {
                className = className.Substring(0,
                    safeIdentifier.LastIndexOf("Internal", StringComparison.Ordinal));
            }
            WriteLine("internal {0}({1}.Internal* native)", safeIdentifier,
                className);
            WriteLineIndent(": this(new global::System.IntPtr(native))");
            WriteStartBraceIndent();
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(CSharpBlockKind.Method);
            WriteLine("internal {0}({1}.Internal native)", safeIdentifier, className);
            WriteLineIndent(": this(&native)");
            WriteStartBraceIndent();
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock(CSharpBlockKind.Method);
            WriteLine("internal {0}(global::System.IntPtr native){1}", safeIdentifier,
                @class.IsValueType ? " : this()" : string.Empty);

            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;
            if (hasBaseClass)
                WriteLineIndent(": base(native)");

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                if (ShouldGenerateClassNativeField(@class))
                {
                    WriteLine("{0} = native;", Helpers.InstanceIdentifier);
                    GenerateVTableClassSetupCall(@class, true);
                }
            }
            else
            {
                WriteLine("var {0} = (Internal*){1}.ToPointer();",
                    Generator.GeneratedIdentifier("ptr"), "native");
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
                WriteLine("var {0} = {1};", Generator.GeneratedIdentifier("ptr"), "native");
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
            Write("public static {0} {1}(", function.OriginalReturnType, functionName);
            Write(FormatMethodParameters(function.Parameters));
            WriteLine(")");
            WriteStartBraceIndent();

            GenerateInternalFunctionCall(function);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethod(Method method, Class @class)
        {
            PushBlock(CSharpBlockKind.Method, method);
            GenerateDeclarationCommon(method);

            if (method.ExplicitInterfaceImpl == null)
            {
                Write(Helpers.GetAccess(GetValidMethodAccess(method)));
            }

            if (method.IsVirtual && !method.IsOverride &&
                (!Driver.Options.GenerateAbstractImpls || !method.IsPure))
                Write("virtual ");

            var isBuiltinOperator = method.IsOperator &&
                Operators.IsBuiltinOperator(method.OperatorKind);

            if (method.IsStatic || isBuiltinOperator)
                Write("static ");

            if (method.IsOverride)
                Write("override ");

            if (Driver.Options.GenerateAbstractImpls && method.IsPure)
                Write("abstract ");

            var functionName = GetFunctionIdentifier(method);

            if (method.IsConstructor || method.IsDestructor)
                Write("{0}(", functionName);
            else if (method.ExplicitInterfaceImpl != null)
                Write("{0} {1}.{2}(", method.OriginalReturnType,
                    method.ExplicitInterfaceImpl.Name, functionName);
            else if (method.OperatorKind == CXXOperatorKind.Conversion)
                Write("{0} {1}(", functionName, method.OriginalReturnType);
            else
                Write("{0} {1}(", method.OriginalReturnType, functionName);


            Write(FormatMethodParameters(method.Parameters));

            Write(")");

            if (Driver.Options.GenerateAbstractImpls && method.IsPure)
            {
                Write(";");
                PopBlock(NewLineKind.BeforeNextBlock);
                return;
            }
            NewLine();

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
                else if (method.IsOverride && method.IsSynthetized)
                {
                    GenerateVirtualTableFunctionCall(method, @class);
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

        private static AccessSpecifier GetValidMethodAccess(Method method)
        {
            switch (method.Access)
            {
                case AccessSpecifier.Public:
                    return AccessSpecifier.Public;
                default:
                    return method.IsOverride ?
                        ((Class) method.Namespace).GetRootBaseMethod(method).Access : method.Access;
            }
        }

        private void GenerateVirtualTableFunctionCall(Function method, Class @class)
        {
            string delegateId;
            Write(GetVirtualCallDelegate(method, @class, Driver.Options.Is32Bit, out delegateId));
            GenerateFunctionCall(delegateId, method.Parameters, method);
        }

        public static string GetVirtualCallDelegate(Function method, Class @class,
            bool is32Bit, out string delegateId)
        {
            var virtualCallBuilder = new StringBuilder();
            virtualCallBuilder.AppendFormat("void* vtable = *((void**) {0}.ToPointer());",
                Helpers.InstanceIdentifier);
            virtualCallBuilder.AppendLine();

            var i = VTables.GetVTableIndex(method, @class);

            virtualCallBuilder.AppendFormat(
                "void* slot = *((void**) vtable + {0} * {1});", i, is32Bit ? 4 : 8);
            virtualCallBuilder.AppendLine();

            string @delegate = ASTHelpers.GetDelegateName(method);
            delegateId = Generator.GeneratedIdentifier(@delegate);

            virtualCallBuilder.AppendFormat(
                "var {1} = ({0}) Marshal.GetDelegateForFunctionPointer(new IntPtr(slot), typeof({0}));",
                @delegate, delegateId);
            virtualCallBuilder.AppendLine();
            return virtualCallBuilder.ToString();
        }

        private void GenerateOperator(Method method, Class @class)
        {
            if (method.IsSynthetized)
            {
                var @operator = Operators.GetOperatorOverloadPair(method.OperatorKind);

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

            if (!method.IsDefaultConstructor || @class.HasNonTrivialDefaultConstructor)
            {
                Write("Internal.{0}({1}", GetFunctionNativeIdentifier(method),
                      Helpers.InstanceIdentifier);
                if (@params.Any())
                    Write(", ");
                GenerateFunctionParams(@params);
                WriteLine(");");
            }

            GenerateVTableClassSetupCall(@class);
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
            Parameter operatorParam = null;
            if (method != null)
            {
                var @class = (Class) method.Namespace;
                isValueType = @class.IsValueType;

                operatorParam = method.Parameters.FirstOrDefault(
                    p => p.Kind == ParameterKind.OperatorParameter);
                needsInstance = !method.IsStatic || operatorParam != null;
            }

            var needsFixedThis = needsInstance && isValueType;
            var @params = GenerateFunctionParamsMarshal(parameters, function);

            var originalFunction = function.OriginalFunction ?? function;

            if (originalFunction.HasIndirectReturnTypeParameter)
            {
                var hiddenParam = originalFunction.Parameters[0];
                if (hiddenParam.Kind != ParameterKind.IndirectReturnType)
                    throw new NotSupportedException("Expected hidden structure parameter kind");

                Class retClass;
                hiddenParam.Type.Desugar().IsTagDecl(out retClass);
                WriteLine("var {0} = new {1}.Internal();", GeneratedIdentifier("ret"),
                    QualifiedIdentifier(retClass.OriginalClass ?? retClass));
            }

            var names = new List<string>();
            foreach (var param in @params)
            {
                if (param.Param == operatorParam && needsInstance)
                    continue;

                var name = string.Empty;
                if (param.Context != null
                    && !string.IsNullOrWhiteSpace(param.Context.ArgumentPrefix))
                    name += param.Context.ArgumentPrefix;

                name += Helpers.SafeIdentifier(param.Name);
                names.Add(name);
            }

            if (originalFunction.HasIndirectReturnTypeParameter)
            {
                var name = string.Format("new IntPtr(&{0})", GeneratedIdentifier("ret"));
                names.Insert(0, name);
            }

            if (needsInstance)
            {
                if (needsFixedThis)
                {
                    names.Insert(0, string.Format("new global::System.IntPtr(&{0})",
                        GeneratedIdentifier("fixedInstance")));
                }
                else
                {
                    if (operatorParam != null)
                    {
                        names.Insert(0, operatorParam.Name + "." + Helpers.InstanceIdentifier);
                    }
                    else
                    {
                        names.Insert(0, Helpers.InstanceIdentifier);                        
                    }
                }
            }

            if (needsFixedThis)
            {
                WriteLine("var {0} = {1};", Generator.GeneratedIdentifier("fixedInstance"),
                    method.IsOperator ? "__arg0" : "ToInternal()");
            }

            if (needsReturn && !originalFunction.HasIndirectReturnTypeParameter)
                Write("var {0} = ", GeneratedIdentifier("ret"));

            WriteLine("{0}({1});", functionName, string.Join(", ", names));

            var cleanups = new List<TextGenerator>();
            GenerateFunctionCallOutParams(@params, cleanups);

            cleanups.AddRange(
                from param in @params
                select param.Context into context
                where context != null && !string.IsNullOrWhiteSpace(context.Cleanup)
                select context.Cleanup);

            foreach (var cleanup in cleanups)
            {
                Write(cleanup);
            }

            if (needsFixedThis)
            {
                if (operatorParam != null)
                {
                    WriteLine("{0}.FromInternal(&{1});",
                        operatorParam.Name, Generator.GeneratedIdentifier("fixedInstance"));
                }
                else
                {
                    WriteLine("FromInternal(&{0});", Generator.GeneratedIdentifier("fixedInstance"));
                }
            }

            if (needsReturn)
            {
                TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                var retTypeName = retType.CSharpType(TypePrinter).Type;
                TypePrinter.PopContext();

                var isIntPtr = retTypeName.Contains("IntPtr");

                Type pointee;
                if (retType.Type.IsPointerTo(out pointee) && isIntPtr)
                {
                    pointee = pointee.Desugar();
                    string @null;
                    Class @class;
                    if (pointee.IsTagDecl(out @class) && @class.IsValueType)
                    {
                        @null = string.Format("new {0}()", pointee);
                    }
                    else
                    {
                        @null = (pointee.IsPrimitiveType() ||
                            pointee.IsPointer()) &&
                            !CSharpTypePrinter.IsConstCharString(retType) ?
                            "IntPtr.Zero" : "null";
                    }
                    WriteLine("if ({0} == global::System.IntPtr.Zero) return {1};",
                        Generator.GeneratedIdentifier("ret"), @null);
                }

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ArgName = GeneratedIdentifier("ret"),
                    ReturnVarName = GeneratedIdentifier("ret"),
                    ReturnType = retType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                retType.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine(
                    TypePrinter.ContextKind == CSharpTypePrinterContextKind.PrimitiveIndexer
                        ? "return *{0};"
                        : "return {0};", marshal.Context.Return);
            }
        }

        private void GenerateFunctionCallOutParams(IEnumerable<ParamMarshal> @params,
            ICollection<TextGenerator> cleanups)
        {
            foreach (var paramInfo in @params)
            {
                var param = paramInfo.Param;
                if (!(param.IsOut || param.IsInOut)) continue;

                var nativeVarName = paramInfo.Name;

                var ctx = new CSharpMarshalContext(Driver)
                {
                    Parameter = param,
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
                if (param.Kind == ParameterKind.IndirectReturnType)
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

            if (param.IsOut || param.IsInOut)
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

        private string FormatMethodParameters(IEnumerable<Parameter> @params)
        {
            return string.Join(", ",
                from param in @params
                where param.Kind != ParameterKind.IndirectReturnType
                let typeName = param.CSharpType(TypePrinter)
                select string.Format("{0}{1} {2}", GetParameterUsage(param.Usage),
                    typeName, SafeIdentifier(param.Name)));
        }

        #endregion

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (typedef.Ignore)
                return false;

            GenerateDeclarationCommon(typedef);

            FunctionType functionType;
            TagType tag;

            if (typedef.Type.IsPointerToPrimitiveType(PrimitiveType.Void)
                || typedef.Type.IsPointerTo(out tag))
            {
                PushBlock(CSharpBlockKind.Typedef);
                WriteLine("public class " + SafeIdentifier(typedef.Name) + @" { }");
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            else if (typedef.Type.IsPointerTo(out functionType))
            {
                PushBlock(CSharpBlockKind.Typedef);
                WriteLine("[UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.{0})]",
                    Helpers.ToCSharpCallConv(functionType.CallingConvention));
                TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                WriteLine("{0}unsafe {1};",
                    Helpers.GetAccess(typedef.Access),
                    string.Format(TypePrinter.VisitDelegate(functionType).Type,
                        SafeIdentifier(typedef.Name)));
                TypePrinter.PopContext();
                PopBlock(NewLineKind.BeforeNextBlock);
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

        public static string GetFunctionIdentifier(Function function)
        {
            var identifier = SafeIdentifier(function.Name);

            var method = function as Method;
            if (method == null || !method.IsOperator)
                return identifier;

            identifier = Operators.GetOperatorIdentifier(method.OperatorKind);

            return identifier;
        }

        public static string GetFunctionNativeIdentifier(Function function)
        {
            var identifier = SafeIdentifier(function.Name);

            if (function.IsOperator)
                identifier = "Operator" + function.OperatorKind;

            var overloads = function.Namespace.GetFunctionOverloads(function)
                .ToList();
            var index = overloads.IndexOf(function);

            if (index >= 0)
                identifier += "_" + index.ToString(CultureInfo.InvariantCulture);

            return identifier;
        }

        public void GenerateInternalFunction(Function function)
        {
            if (!function.IsProcessed || function.ExplicityIgnored || function.IsPure)
                return;

            if (function.OriginalFunction != null)
                function = function.OriginalFunction;

            PushBlock(CSharpBlockKind.InternalsClassMethod);
            WriteLine("[SuppressUnmanagedCodeSecurity]");

            string libName = Options.SharedLibraryName;

            if (Options.CheckSymbols)
            {
                NativeLibrary library;
                Driver.Symbols.FindLibraryBySymbol(function.Mangled, out library);

                libName = Path.GetFileNameWithoutExtension(library.FileName);
            }
            if (libName != null && libName.Length > 3 && libName.StartsWith("lib"))
            {
                libName = libName.Substring(3);
            }
            if (libName == null)
                libName = Options.SharedLibraryName;

            if (Options.GenerateInternalImports)
                libName = "__Internal";

            Write("[DllImport(\"{0}\", ", libName);

            var callConv = Helpers.ToCSharpCallConv(function.CallingConvention);
            WriteLine("CallingConvention = global::System.Runtime.InteropServices.CallingConvention.{0},",
                callConv);

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

            foreach (var param in function.Parameters)
            {
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

            WriteLine("internal static extern {0} {1}({2});", retType,
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