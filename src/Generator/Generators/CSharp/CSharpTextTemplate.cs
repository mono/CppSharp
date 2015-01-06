using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Util;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;
using Type = CppSharp.AST.Type;
using CppAbi = CppSharp.Parser.AST.CppAbi;

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
            if (id.All(char.IsLetterOrDigit))
                return Keywords.Contains(id) ? "@" + id : id;
            return new string(id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }

        public const string InstanceField = "__instance";
        public const string InstanceIdentifier = "__Instance";

        public const string OwnsNativeInstanceIdentifier = "__ownsNativeInstance";

        public const string CreateInstanceIdentifier = "__CreateInstance";

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
        public const int Finalizer = FIRST + 19;
    }

    public class CSharpTextTemplate : Template
    {
        public CSharpTypePrinter TypePrinter { get; private set; }
        public CSharpExpressionPrinter ExpressionPrinter { get; private set; }

        public override string FileExtension
        {
            get { return "cs"; }
        }

        public CSharpTextTemplate(Driver driver, IEnumerable<TranslationUnit> units, CSharpTypePrinter typePrinter, CSharpExpressionPrinter expressionPrinter)
            : base(driver, units)
        {
            TypePrinter = typePrinter;
            ExpressionPrinter = expressionPrinter;
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

            if (decl.GenerationKind == GenerationKind.Generate && Options.GenerateLibraryNamespace)
                names.Add(Options.OutputNamespace);

            names.Reverse();
            return string.Join(".", names);
        }

        public static string GeneratedIdentifier(string id)
        {
            return Generator.GeneratedIdentifier(id);
        }

        #endregion

        public override void Process()
        {
            GenerateHeader();

            PushBlock(CSharpBlockKind.Usings);
            WriteLine("using System;");
            WriteLine("using System.Runtime.InteropServices;");
            WriteLine("using System.Security;");
            foreach (var customUsingStatement in Options.DependentNameSpaces)
            {
                WriteLine(string.Format("using {0};", customUsingStatement));
            }
            PopBlock(NewLineKind.BeforeNextBlock);

            if (Options.GenerateLibraryNamespace)
            {
                PushBlock(CSharpBlockKind.Namespace);
                WriteLine("namespace {0}", Driver.Options.OutputNamespace);
                WriteStartBraceIndent();
            }

            foreach (var unit in TranslationUnits)
            {
                GenerateDeclContext(unit);
            }

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
                if (!@enum.IsGenerated || @enum.IsIncomplete)
                    continue;

                GenerateEnum(@enum);
            }

            // Generate all the typedef declarations.
            foreach (var typedef in context.Typedefs)
            {
                GenerateTypedef(typedef);
            }

            // Generate all the struct/class declarations.
            foreach (var @class in context.Classes)
            {
                if (@class.IsIncomplete)
                    continue;

                if (@class.IsInterface)
                    GenerateInterface(@class);
                else
                    GenerateClass(@class);
            }

            if (context.HasFunctions)
            {
                PushBlock(CSharpBlockKind.Functions);
                WriteLine("public unsafe partial class {0}",
                    context.TranslationUnit.FileNameWithoutExtension);
                WriteStartBraceIndent();

                PushBlock(CSharpBlockKind.InternalsClass);
                GenerateClassInternalHead();
                WriteStartBraceIndent();

                // Generate all the internal function declarations.
                foreach (var function in context.Functions)
                {
                    if (!function.IsGenerated && !function.IsInternal ) continue;

                    GenerateInternalFunction(function);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);

                foreach (var function in context.Functions)
                {
                    if (!function.IsGenerated) continue;

                    GenerateFunction(function);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            foreach (var @event in context.Events)
            {
                if (!@event.IsGenerated) continue;

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
            if (@class.IsIncomplete)
                return;

            PushBlock(CSharpBlockKind.Class);
            GenerateDeclarationCommon(@class);

            GenerateClassProlog(@class);

            NewLine();
            WriteStartBraceIndent();

            if (!@class.IsOpaque)
            {
                if (!@class.IsAbstractImpl)
                    GenerateClassInternals(@class);
                GenerateDeclContext(@class);

                if (@class.IsDependent || !@class.IsGenerated)
                    goto exit;

                if (ShouldGenerateClassNativeField(@class))
                {
                    PushBlock(CSharpBlockKind.Field);
                    if (@class.IsValueType)
                    {
                        WriteLine("private {0}.Internal {1};", @class.Name, Helpers.InstanceField);
                        WriteLine("public {0}.Internal {1} {{ get {{ return {2}; }} }}", @class.Name,
                            Helpers.InstanceIdentifier, Helpers.InstanceField);
                    }
                    else
                    {
                        WriteLine("public {0} {1} {{ get; protected set; }}",
                            "global::System.IntPtr", Helpers.InstanceIdentifier);
                    }
                    PopBlock(NewLineKind.BeforeNextBlock);
                }

                if (Options.GenerateClassMarshals)
                {
                    GenerateClassMarshals(@class);
                }

                GenerateClassConstructors(@class);

                GenerateClassMethods(@class.Methods);
                GenerateClassVariables(@class);
                GenerateClassProperties(@class);

                if (Options.GenerateVirtualTables && @class.IsDynamic)
                    GenerateVTable(@class);
            }
        exit:
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
            if (!@class.IsGenerated || @class.IsIncomplete)
                return;

            PushBlock(CSharpBlockKind.Interface);
            GenerateDeclarationCommon(@class);

            GenerateClassProlog(@class);

            NewLine();
            WriteStartBraceIndent();

            GenerateDeclContext(@class);

            foreach (var method in @class.Methods.Where(m =>
                !ASTUtils.CheckIgnoreMethod(m, Options) && m.Access == AccessSpecifier.Public))
            {
                PushBlock(CSharpBlockKind.Method);
                GenerateDeclarationCommon(method);

                var functionName = GetMethodIdentifier(method);

                Write("{0} {1}(", method.OriginalReturnType, functionName);

                Write(FormatMethodParameters(method.Parameters));

                WriteLine(");");

                PopBlock(NewLineKind.BeforeNextBlock);
            }
            foreach (var prop in @class.Properties.Where(p => p.IsGenerated))
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

        private void GenerateUnionFields(Class @class)
        {
            foreach (var field in @class.Fields)
            {
                GenerateClassField(field);                
            }
        }

        public void GenerateClassInternals(Class @class)
        {
            PushBlock(CSharpBlockKind.InternalsClass);
            WriteLine("[StructLayout(LayoutKind.Explicit, Size = {0})]",
                @class.Layout.Size);

            GenerateClassInternalHead(@class);
            WriteStartBraceIndent();

            var typePrinter = TypePrinter;
            typePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            GenerateClassFields(@class, GenerateClassInternalsField, true);
            if (@class.IsGenerated)
            {
                if (Options.GenerateVirtualTables && @class.IsDynamic)
                    GenerateVTablePointers(@class);

                var functions = GatherClassInternalFunctions(@class);

                foreach (var function in functions)
                {
                    GenerateInternalFunction(function);
                }
            }

            typePrinter.PopContext();

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private IEnumerable<Function> GatherClassInternalFunctions(Class @class, bool includeCtors = true)
        {
            var functions = new List<Function>();
            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    functions.AddRange(GatherClassInternalFunctions(@base.Class, false));

            Action<Method> tryAddOverload = method =>
            {
                if (method.IsSynthetized)
                    return;

                if (method.IsProxy)
                    return;

                functions.Add(method);
            };

            if (includeCtors)
            {
                foreach (var ctor in @class.Constructors)
                {
                    if (@class.IsStatic || ctor.IsMoveConstructor)
                        continue;

                    if (!ctor.IsGenerated && !(Options.GenerateCopyConstructors && ctor.IsCopyConstructor))
                        continue;

                    if (ctor.IsDefaultConstructor && !@class.HasNonTrivialDefaultConstructor)
                        continue;

                    tryAddOverload(ctor);
                }
            }

            if (@class.HasNonTrivialDestructor && !@class.IsStatic)
                foreach (var dtor in @class.Destructors)
                    tryAddOverload(dtor);

            foreach (var method in @class.Methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method, Options))
                    continue;

                if (method.IsConstructor)
                    continue;

                tryAddOverload(method);
            }

            foreach (var prop in @class.Properties)
            {
                if (prop.GetMethod != null)
                    tryAddOverload(prop.GetMethod);

                if (prop.SetMethod != null && prop.SetMethod != prop.GetMethod)
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
                @params.Add(string.Format("{0} {1}", typeName, param.Name));
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

            WriteLine("partial struct Internal");
        }

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;
            return @class.IsValueType || !@class.HasBase || !@class.HasRefBase();
        }

        public void GenerateClassProlog(Class @class)
        {
            Write(@class.IsInternal ? "internal " : Helpers.GetAccess(@class.Access));
            Write("unsafe ");

            if (Driver.Options.GenerateAbstractImpls && @class.IsAbstract)
                Write("abstract ");

            if (@class.IsStatic)
                Write("static ");

            // This token needs to directly precede the "class" token.
            if (Options.GeneratePartialClasses)
                Write("partial ");

            Write(@class.IsInterface ? "interface " : (@class.IsValueType ? "struct " : "class "));
            Write("{0}", @class.Name);

            var bases = new List<string>();

            var needsBase =  @class.HasNonIgnoredBase && @class.IsGenerated;

            if (needsBase)
            {
                bases.AddRange(
                    from @base in @class.Bases
                    where @base.IsClass
                    select QualifiedIdentifier(@base.Class));
            }

            if (@class.IsGenerated)
            {
                if (@class.IsRefType)
                    bases.Add("IDisposable");

                if (Options.GenerateClassMarshals)
                {
                    bases.Add("CppSharp.Runtime.ICppMarshal");
                }
            }

            if (bases.Count > 0 && !@class.IsStatic)
                Write(" : {0}", string.Join(", ", bases));
        }

        public void GenerateClassFields(Class @class, Action<Field> action, bool nativeFields = false)
        {
            foreach (var @base in @class.Bases.Where(b => !(b.Type is DependentNameType)))
            {
                TypeMap typeMap;
                if ((!Driver.TypeDatabase.FindTypeMap(@base.Type, out typeMap) && !@base.Class.IsDeclared) ||
                    @base.Class.OriginalClass == @class)
                    continue;

                GenerateClassFields(@base.Class, action, nativeFields);
            }

            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field, nativeFields)) continue;
                action(field);
            }
        }

        private void GenerateClassInternalsField(Field field)
        {
            // we do not support dependent fields yet, see https://github.com/mono/CppSharp/issues/197
            Class @class;
            field.Type.TryGetClass(out @class);
            if ((field.Type.IsDependent && !field.Type.IsPointer() &&
                !(@class != null && @class.IsUnion)) || (@class != null && @class.TranslationUnit.IsSystemHeader))
                return;

            var safeIdentifier = Helpers.SafeIdentifier(field.OriginalName);

            PushBlock(CSharpBlockKind.Field);

            WriteLine("[FieldOffset({0})]", field.OffsetInBytes);

            var fieldTypePrinted = field.QualifiedType.CSharpType(TypePrinter);

            if (!string.IsNullOrWhiteSpace(fieldTypePrinted.NameSuffix))
                safeIdentifier += fieldTypePrinted.NameSuffix;

            var access = @class != null && !@class.IsGenerated ? "internal" : "public";
            if (field.Expression != null)
            {
                var fieldValuePrinted = field.Expression.CSharpValue(ExpressionPrinter);
                Write("{0} {1} {2} = {3};", access, fieldTypePrinted.Type, safeIdentifier, fieldValuePrinted);
            }
            else
            {
                Write("{0} {1} {2};", access, fieldTypePrinted.Type, safeIdentifier);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassField(Field field, bool @public = false)
        {
            PushBlock(CSharpBlockKind.Field);

            GenerateDeclarationCommon(field);

            var @class = (Class) field.Namespace;

            WriteLine("{0} {1} {2};", @public ? "public" : "private",
                field.Type, field.Name);

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

        private void GeneratePropertySetter<T>(QualifiedType returnType, T decl, Class @class, bool isAbstract = false)
            where T : Declaration, ITypedDecl
        {
            if (!(decl is Function || decl is Field) )
            {
                return;
            }

            PushBlock(CSharpBlockKind.Method);
            Write("set");

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
                if (isAbstract && Driver.Options.GenerateAbstractImpls)
                {
                    Write(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteStartBraceIndent();
                if (function.Parameters.Count == 0)
                    throw new NotSupportedException("Expected at least one parameter in setter");

                param.QualifiedType = function.Parameters[0].QualifiedType;

                if (function.SynthKind == FunctionSynthKind.AbstractImplCall)
                {
                    GenerateAbstractImplCall(function, @class);
                }
                else
                {
                    var method = function as Method;
                    if (method != null && method.OperatorKind == CXXOperatorKind.Subscript)
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
                    else
                    {
                        GenerateInternalFunctionCall(function, new List<Parameter> { param });
                    }
                }
                WriteCloseBraceIndent();
            }
            else
            {
                var field = decl as Field;
                if (WrapSetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteStartBraceIndent();

                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                ctx.ReturnVarName = string.Format("{0}{1}{2}",
                    @class.IsValueType
                        ? Helpers.InstanceField
                        : string.Format("((Internal*) {0})", Helpers.InstanceIdentifier),
                    @class.IsValueType ? "." : "->",
                    Helpers.SafeIdentifier(field.OriginalName));
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (marshal.Context.Return.StringBuilder.Length > 0)
                {
                    WriteLine("{0} = {1};", ctx.ReturnVarName, marshal.Context.Return);                    
                }

                WriteCloseBraceIndent();
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private bool WrapSetterArrayOfPointers(string name, Type fieldType)
        {
            var arrayType = fieldType as ArrayType;
            if (arrayType != null && arrayType.Type.IsPointerToPrimitiveType())
            {
                NewLine();
                WriteStartBraceIndent();
                WriteLine("{0} = value;", name);
                WriteLine("if (!{0}{1})", name, "Initialised");
                WriteStartBraceIndent();
                WriteLine("{0}{1} = true;", name, "Initialised");
                WriteCloseBraceIndent();
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
                return true;
            }
            return false;
        }

        private void GenerateIndexerSetter(QualifiedType returnType, Function function)
        {
            Type type;
            function.Type.IsPointerTo(out type);
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

        private void GeneratePropertyGetter<T>(QualifiedType returnType, T decl, Class @class, bool isAbstract = false)
            where T : Declaration, ITypedDecl
        {
            PushBlock(CSharpBlockKind.Method);
            Write("get");

            if (decl is Function)
            {
                var function = decl as Function;
                if (isAbstract && Driver.Options.GenerateAbstractImpls)
                {
                    Write(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteStartBraceIndent();
                var method = function as Method;
                if (method != null && method.SynthKind == FunctionSynthKind.AbstractImplCall)
                    GenerateAbstractImplCall(method, @class);
                else
                    GenerateInternalFunctionCall(function, function.Parameters, returnType.Type);
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapGetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteStartBraceIndent();

                var ctx = new CSharpMarshalContext(Driver)
                {
                    Kind = CSharpMarshalKind.NativeField,
                    ArgName = decl.Name,
                    ReturnVarName = string.Format("{0}{1}{2}",
                        @class.IsValueType
                            ? Helpers.InstanceField
                            : string.Format("((Internal*) {0})", Helpers.InstanceIdentifier),
                        @class.IsValueType ? "." : "->",
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

        private bool WrapGetterArrayOfPointers(string name, Type fieldType)
        {
            var arrayType = fieldType as ArrayType;
            if (arrayType != null && arrayType.Type.IsPointerToPrimitiveType())
            {
                NewLine();
                WriteStartBraceIndent();
                WriteLine("if (!{0}{1})", name, "Initialised");
                WriteStartBraceIndent();
                WriteLine("{0} = null;", name);
                WriteLine("{0}{1} = true;", name, "Initialised");
                WriteCloseBraceIndent();
                WriteLine("return {0};", name);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
                return true;
            }
            return false;
        }

        public void GenerateClassMethods(IList<Method> methods)
        {
            if (methods.Count == 0)
                return;

            var @class = (Class) methods[0].Namespace;

            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    GenerateClassMethods(@base.Class.Methods.Where(m => !m.IsOperator).ToList());

            var staticMethods = new List<Method>();
            foreach (var method in methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method, Options))
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
            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    GenerateClassVariables(@base.Class);

            foreach (var variable in @class.Variables)
            {
                if (!variable.IsGenerated) continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                var type = variable.Type;

                GenerateVariable(@class, type, variable);
            }
        }

        private void GenerateClassProperties(Class @class)
        {
            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore && b.Class.IsDeclared))
                    GenerateClassProperties(@base.Class);

            GenerateProperties(@class);
        }

        private void GenerateProperties(Class @class)
        {
            foreach (var prop in @class.Properties.Where(p => p.IsGenerated))
            {
                if (prop.IsInRefTypeAndBackedByValueClassField())
                {
                    GenerateClassField(prop.Field, true);
                    continue;
                }

                PushBlock(CSharpBlockKind.Property);

                ArrayType arrayType = prop.Type as ArrayType;
                if (arrayType != null && arrayType.Type.IsPointerToPrimitiveType() && prop.Field != null)
                {
                    GenerateClassField(prop.Field);
                    WriteLine("private bool {0};",
                        GeneratedIdentifier(string.Format("{0}Initialised", prop.Field.OriginalName)));
                }

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

                    WriteLine("{0} {1}", prop.Type, GetPropertyName(prop));
                }
                else
                {
                    WriteLine("{0} {1}.{2}", prop.Type, prop.ExplicitInterfaceImpl.Name,
                        GetPropertyName(prop));
                }
                WriteStartBraceIndent();

                if (prop.Field != null)
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.QualifiedType, prop.Field, @class);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.Field.QualifiedType, prop.Field, @class);
                }
                else
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.QualifiedType, prop.GetMethod, @class, prop.IsPure);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.QualifiedType, prop.SetMethod, @class, prop.IsPure);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private string GetPropertyName(Property prop)
        {
            return prop.Parameters.Count == 0 ? prop.Name
                : string.Format("this[{0}]", FormatMethodParameters(prop.Parameters));
        }

        private void GenerateVariable(Class @class, Type type, Variable variable)
        {
            PushBlock(CSharpBlockKind.Variable);
            WriteLine("public static {0} {1}", type, variable.Name);
            WriteStartBraceIndent();

            GeneratePropertyGetter(variable.QualifiedType, variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable.QualifiedType, variable, @class);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #region Virtual Tables

        public List<VTableComponent> GetValidVTableMethodEntries(Class @class)
        {
            var entries = VTables.GatherVTableMethodEntries(@class);
            return entries.Where(e => !e.Ignore && !e.Method.IsOperator).ToList();
        }

        public List<VTableComponent> GetUniqueVTableMethodEntries(Class @class)
        {
            var uniqueEntries = new OrderedSet<VTableComponent>();
            foreach (var entry in GetValidVTableMethodEntries(@class))
                uniqueEntries.Add(entry);

            return uniqueEntries.ToList();
        }

        public void GenerateVTable(Class @class)
        {
            var entries = VTables.GatherVTableMethodEntries(@class);
            var wrappedEntries = GetUniqueVTableMethodEntries(@class);
            if (wrappedEntries.Count == 0)
                return;

            PushBlock(CSharpBlockKind.Region);
            WriteLine("#region Virtual table interop");
            NewLine();

            // Generate a delegate type for each method.
            foreach (var method in wrappedEntries.Select(e => e.Method))
            {
                GenerateVTableMethodDelegates(@class, method);
            }

            const string dictionary = "System.Collections.Generic.Dictionary";

            WriteLine("private static void*[] _OldVTables;");
            WriteLine("private static void*[] _NewVTables;");
            WriteLine("private static void*[] _Thunks;");
            WriteLine("private static {0}<IntPtr, WeakReference> _References;", dictionary);
            NewLine();

            GenerateVTableClassSetup(@class, dictionary, entries, wrappedEntries);

            WriteLine("#endregion");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVTableClassSetup(Class @class, string dictionary,
            IList<VTableComponent> entries, IList<VTableComponent> wrappedEntries)
        {
            WriteLine("void SetupVTables(global::System.IntPtr instance)");
            WriteStartBraceIndent();

            WriteLine("var native = (Internal*)instance.ToPointer();");
            NewLine();

            WriteLine("if (_References == null)");
            WriteLineIndent("_References = new {0}<IntPtr, WeakReference>();", dictionary);
            NewLine();

            WriteLine("if (_References.ContainsKey(instance))");
            WriteLineIndent("return;");

            NewLine();
            WriteLine("_References[instance] = new WeakReference(this);");
            NewLine();

            // Save the original vftable pointers.
            WriteLine("if (_OldVTables == null)");
            WriteStartBraceIndent();

            switch (Driver.Options.Abi)
            {
                case CppAbi.Microsoft:
                    SaveOriginalVTablePointersMS(@class);
                    break;
                case CppAbi.Itanium:
                case CppAbi.ARM:
                    SaveOriginalVTablePointersItanium();
                    break;
            }
            WriteCloseBraceIndent();
            NewLine();

            // Get the _Thunks
            WriteLine("if (_Thunks == null)");
            WriteStartBraceIndent();
            WriteLine("_Thunks = new void*[{0}];", wrappedEntries.Count);

            var uniqueEntries = new HashSet<VTableComponent>();

            for (int i = 0; i < wrappedEntries.Count; i++)
            {
                var entry = wrappedEntries[i];
                var method = entry.Method;
                var name = GetVTableMethodDelegateName(method);
                var instance = name + "Instance";
                if (uniqueEntries.Add(entry))
                    WriteLine("{0} += {1}Hook;", instance, name);
                WriteLine("_Thunks[{0}] = Marshal.GetFunctionPointerForDelegate({1}).ToPointer();",
                    i, instance);
            }
            WriteCloseBraceIndent();

            NewLine();

            WriteLine("if (_NewVTables == null)");
            WriteStartBraceIndent();

            switch (Driver.Options.Abi)
            {
                case CppAbi.Microsoft:
                    AllocateNewVTablesMS(@class, entries, wrappedEntries);
                    break;
                case CppAbi.Itanium:
                case CppAbi.ARM:
                    AllocateNewVTablesItanium(@class, entries, wrappedEntries);
                    break;
            }

            WriteCloseBraceIndent();
            NewLine();
        }

        private void SaveOriginalVTablePointersMS(Class @class)
        {
            WriteLine("_OldVTables = new void*[{0}];", @class.Layout.VFTables.Count);

            for (int i = 0; i < @class.Layout.VFTables.Count; i++)
            {
                WriteLine("_OldVTables[{0}] = native->vfptr{0}.ToPointer();", i);
            }
        }

        private void SaveOriginalVTablePointersItanium()
        {
            WriteLine("_OldVTables = new void*[1];");
            WriteLine("_OldVTables[0] = native->vfptr0.ToPointer();");
        }

        private void AllocateNewVTablesMS(Class @class, IList<VTableComponent> entries,
            IList<VTableComponent> wrappedEntries)
        {
            WriteLine("_NewVTables = new void*[{0}];", @class.Layout.VFTables.Count);

            for (int tableIndex = 0; tableIndex < @class.Layout.VFTables.Count; tableIndex++)
            {
                var vfptr = @class.Layout.VFTables[tableIndex];
                var size = vfptr.Layout.Components.Count;
                WriteLine("var vfptr{0} = Marshal.AllocHGlobal({1} * {2});",
                    tableIndex, size, Driver.TargetInfo.PointerWidth / 8);
                WriteLine("_NewVTables[{0}] = vfptr{0}.ToPointer();", tableIndex);

                AllocateNewVTableEntries(@class, entries, wrappedEntries, tableIndex);
            }

            WriteCloseBraceIndent();
            NewLine();

            for (var i = 0; i < @class.Layout.VFTables.Count; i++)
                WriteLine("native->vfptr{0} = new IntPtr(_NewVTables[{0}]);", i);
        }

        private void AllocateNewVTablesItanium(Class @class, IList<VTableComponent> entries,
            IList<VTableComponent> wrappedEntries)
        {
            WriteLine("_NewVTables = new void*[1];");

            // reserve space for the offset-to-top and RTTI pointers as well
            var size = entries.Count;
            WriteLine("var vfptr{0} = Marshal.AllocHGlobal({1} * {2});", 0, size, Driver.TargetInfo.PointerWidth / 8);
            WriteLine("_NewVTables[0] = vfptr0.ToPointer();");

            AllocateNewVTableEntries(@class, entries, wrappedEntries, tableIndex: 0);

            WriteCloseBraceIndent();
            NewLine();

            WriteLine("native->vfptr0 = new IntPtr(_NewVTables[0]);");
        }

        private void AllocateNewVTableEntries(Class @class, IList<VTableComponent> entries,
            IList<VTableComponent> wrappedEntries, int tableIndex)
        {
            foreach (var entry in entries)
            {
                var offsetInBytes = VTables.GetVTableComponentIndex(@class, entry)
                                    * (Driver.TargetInfo.PointerWidth / 8);

                if (entry.Ignore)
                    WriteLine("*(void**)(vfptr{0} + {1}) = *(void**)(native->vfptr{0} + {1});",
                        tableIndex, offsetInBytes);
                else
                    WriteLine("*(void**)(vfptr{0} + {1}) = _Thunks[{2}];", tableIndex,
                        offsetInBytes, wrappedEntries.IndexOf(entry));
            }
        }

        private void GenerateVTableClassSetupCall(Class @class, bool addPointerGuard = false)
        {
            var entries = GetUniqueVTableMethodEntries(@class);
            if (Options.GenerateVirtualTables && @class.IsDynamic && entries.Count != 0)
            {
                // called from internal ctors which may have been passed an IntPtr.Zero
                if (addPointerGuard)
                {
                    WriteLine("if ({0} != global::System.IntPtr.Zero && !isInternalImpl)",
                        Helpers.InstanceIdentifier);
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
                if (!param.IsGenerated)
                    continue;

                if (param.Kind == ParameterKind.IndirectReturnType)
                    continue;

                var ctx = new CSharpMarshalContext(Driver)
                {
                    ReturnType = param.QualifiedType,
                    ReturnVarName = param.Name
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
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
                WriteLine("target.{0}({1});", method.Name, string.Join(", ", marshals));              
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
                WriteLine("target.{0} = {1};", property.Name,
                    string.Join(", ", marshals));
            }
            else
            {
                WriteLine("target.{0};", property.Name);
            }
        }

        private void GenerateVTableMethodDelegates(Class @class, Method method)
        {
            PushBlock(CSharpBlockKind.VTableDelegate);

            // This works around a parser bug, see https://github.com/mono/CppSharp/issues/202
            if (method.Signature != null)
            {
                var cleanSig = method.Signature.ReplaceLineBreaks("");
                cleanSig = Regex.Replace(cleanSig, @"\s+", " ");

                WriteLine("// {0}", cleanSig);
            }
            WriteLine("[SuppressUnmanagedCodeSecurity]");
            WriteLine("[UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.{0})]",
                method.CallingConvention.ToInteropCallConv());

            CSharpTypePrinterResult retType;
            var @params = GatherInternalParams(method, out retType);

            var vTableMethodDelegateName = GetVTableMethodDelegateName(method);
            WriteLine("internal delegate {0} {1}({2});", retType, vTableMethodDelegateName,
                string.Join(", ", @params));

            WriteLine("private static {0} {0}Instance;", vTableMethodDelegateName);
            NewLine();

            WriteLine("private static {0} {1}Hook({2})", retType, vTableMethodDelegateName,
                string.Join(", ", @params));
            WriteStartBraceIndent();

            WriteLine("if (!_References.ContainsKey(instance))");
            WriteLineIndent("throw new global::System.Exception(\"No managed instance was found\");");
            NewLine();

            WriteLine("var target = ({0}) _References[instance].Target;", @class.Name);
            GenerateVTableManagedCall(method);

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.Always);
        }

        public string GetVTableMethodDelegateName(Function function)
        {
            var nativeId = GetFunctionNativeIdentifier(function);

            // Trim '@' (if any) because '@' is valid only as the first symbol.
            nativeId = nativeId.Trim('@');

            return string.Format("_{0}Delegate", nativeId);
        }

        public void GenerateVTablePointers(Class @class)
        {
            switch (Driver.Options.Abi)
            {
                case CppAbi.Microsoft:
                    var index = 0;
                    foreach (var info in @class.Layout.VFTables)
                    {
                        PushBlock(CSharpBlockKind.InternalsClassField);

                        WriteLine("[FieldOffset({0})]", info.VFPtrFullOffset);
                        WriteLine("public global::System.IntPtr vfptr{0};", index++);

                        PopBlock(NewLineKind.BeforeNextBlock);
                    }
                    break;
                case CppAbi.Itanium:
                case CppAbi.ARM:
                    PushBlock(CSharpBlockKind.InternalsClassField);

                    WriteLine("[FieldOffset(0)]");
                    WriteLine("public global::System.IntPtr vfptr0;");

                    PopBlock(NewLineKind.BeforeNextBlock);
                    break;
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
            WriteLine("public event {0} {1}", @event.Type, @event.Name);
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
                    ReturnVarName = param.Name,
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
            if (@class.IsStatic)
                return;

            // Output a default constructor that takes the native pointer.
            GenerateNativeConstructor(@class);

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor, Options))
                    continue;

                GenerateMethod(ctor, @class);
            }

            if (@class.IsRefType)
            {
                GenerateClassFinalizer(@class);
                GenerateDisposeMethods(@class);
            }
        }

        private void GenerateClassFinalizer(Class @class)
        {
            if (!Options.GenerateFinalizers)
                return;

            PushBlock(CSharpBlockKind.Finalizer);

            WriteLine("~{0}()", @class.Name);
            WriteStartBraceIndent();
            WriteLine("Dispose(false);");
            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);
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
                Write("private ");
            }
            else
            {
                Write("protected ");
                Write(hasBaseClass ? "override " : "virtual ");
            }

            WriteLine("void Dispose(bool disposing)");
            WriteStartBraceIndent();

            if (ShouldGenerateClassNativeField(@class))
            {
                var dtor = @class.Methods.FirstOrDefault(method => method.IsDestructor);
                if (dtor != null)
                {
                    if (dtor.Access != AccessSpecifier.Private && @class.HasNonTrivialDestructor && !dtor.IsPure)
                    {
                        NativeLibrary library;
                        if (!Options.CheckSymbols ||
                            Driver.Symbols.FindLibraryBySymbol(dtor.Mangled, out library))
                        {
                            WriteLine("Internal.{0}({1});", GetFunctionNativeIdentifier(dtor),
                                Helpers.InstanceIdentifier);
                        }
                    }
                }
            }

            if (@class.IsRefType)
            {
                WriteLine("if ({0})", Helpers.OwnsNativeInstanceIdentifier);
                WriteStartBraceIndent();
                WriteLine("Marshal.FreeHGlobal({0});", Helpers.InstanceIdentifier);
                WriteCloseBraceIndent();
            }

            if (hasBaseClass)
                WriteLine("base.Dispose(disposing);");

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNativeConstructor(Class @class)
        {
            if (@class.IsRefType)
            {
                PushBlock(CSharpBlockKind.Field);
                WriteLine("private readonly bool {0};", Helpers.OwnsNativeInstanceIdentifier);
                PopBlock(NewLineKind.BeforeNextBlock);   
            }

            string className = @class.Name;
            string safeIdentifier = className;
            if (@class.IsAbstractImpl)
            {
                className = className.Substring(0,
                    safeIdentifier.LastIndexOf("Internal", StringComparison.Ordinal));
            }

            if (!@class.IsAbstract)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("public static {0}{1} {2}(global::System.IntPtr native)",
                    @class.HasNonIgnoredBase && !@class.BaseClass.IsAbstract ? "new " : string.Empty,
                    safeIdentifier, Helpers.CreateInstanceIdentifier);
                WriteStartBraceIndent();
                WriteLine("return new {0}(({1}.Internal*) native);", safeIdentifier, className);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);

                GenerateNativeConstructorByValue(@class, className, safeIdentifier);
            }

            PushBlock(CSharpBlockKind.Method);
            WriteLine("{0} {1}({2}.Internal* native, bool isInternalImpl = false){3}",
                @class.IsRefType ? "protected" : "private",
                safeIdentifier, className, @class.IsValueType ? " : this()" : string.Empty);

            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;
            if (hasBaseClass)
                WriteLineIndent(": base(({0}.Internal*) native{1})",
                    @class.BaseClass.Name, @class.IsAbstractImpl ? ", true" : string.Empty);

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                if (ShouldGenerateClassNativeField(@class))
                {
                    WriteLine("{0} = new global::System.IntPtr(native);", Helpers.InstanceIdentifier);
                    GenerateVTableClassSetupCall(@class, true);
                }
            }
            else
            {
                WriteLine("{0} = *native;", Helpers.InstanceField);
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNativeConstructorByValue(Class @class, string className, string safeIdentifier)
        {
            PushBlock(CSharpBlockKind.Method);
            WriteLine("public static {0} {1}({0}.Internal native)", className, Helpers.CreateInstanceIdentifier);
            WriteStartBraceIndent();
            WriteLine("return new {0}(native);", safeIdentifier);
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            if (@class.IsRefType)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("private static {0}.Internal* __CopyValue({0}.Internal native)", className);
                WriteStartBraceIndent();
                if (@class.HasNonTrivialCopyConstructor)
                {
                    // Find a valid copy constructor overload.
                    var copyCtorMethod = @class.Methods.FirstOrDefault(method =>
                        method.IsCopyConstructor);

                    if (copyCtorMethod == null)
                        throw new NotSupportedException("Expected a valid copy constructor");

                    // Allocate memory for a new native object and call the ctor.
                    WriteLine("var ret = Marshal.AllocHGlobal({0});", @class.Layout.Size);
                    WriteLine("{0}.Internal.{1}(ret, new global::System.IntPtr(&native));",
                        QualifiedIdentifier(@class), GetFunctionNativeIdentifier(copyCtorMethod));
                    WriteLine("return ({0}.Internal*) ret;", className);
                }
                else
                {
                    WriteLine("var ret = ({0}.Internal*) Marshal.AllocHGlobal({1});",
                        className, @class.Layout.Size);
                    WriteLine("*ret = native;", className);
                    WriteLine("return ret;");
                }
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            PushBlock(CSharpBlockKind.Method);
            WriteLine("private {0}({1}.Internal native)", safeIdentifier, className);
            WriteLineIndent(@class.IsRefType ? ": this(__CopyValue(native))" : ": this()");
            WriteStartBraceIndent();
            if (@class.IsRefType)
            {
                WriteLine("{0} = true;", Helpers.OwnsNativeInstanceIdentifier);
            }
            else
            {
                WriteLine("{0} = native;", Helpers.InstanceField);
            }
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private bool GenerateClassConstructorBase(Class @class, Method method)
        {
            var hasBase = @class.HasBaseClass;

            if (hasBase && !@class.IsValueType)
            {
                PushIndent();
                Write(": this(");

                if (method != null)
                    Write("(Internal*) null");
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

            if (method.IsVirtual && !method.IsOverride && !method.IsOperator &&
                (!Driver.Options.GenerateAbstractImpls || !method.IsPure))
                Write("virtual ");

            var isBuiltinOperator = method.IsOperator &&
                Operators.IsBuiltinOperator(method.OperatorKind);

            if (method.IsStatic || isBuiltinOperator)
                Write("static ");

            if (method.IsOverride)
            {
                if (method.Access == AccessSpecifier.Private)
                    Write("sealed ");
                Write("override ");
            }

            if (Driver.Options.GenerateAbstractImpls && method.IsPure)
                Write("abstract ");

            var functionName = GetMethodIdentifier(method);

            if (method.IsConstructor || method.IsDestructor)
                Write("{0}(", functionName);
            else if (method.ExplicitInterfaceImpl != null)
                Write("{0} {1}.{2}(", method.OriginalReturnType,
                    method.ExplicitInterfaceImpl.Name, functionName);
            else if (method.OperatorKind == CXXOperatorKind.Conversion || 
                     method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                Write("{0} {1}(", functionName, method.OriginalReturnType);
            else
                Write("{0} {1}(", method.OriginalReturnType, functionName);


            Write(FormatMethodParameters(method.Parameters));

            Write(")");

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload && method.IsConstructor &&
                !(Driver.Options.GenerateAbstractImpls && method.IsPure))
            {
                Write(" : this({0})",
                    string.Join(", ",
                        method.Parameters.Where(
                            p => p.Kind == ParameterKind.Regular).Select(
                                p => p.Ignore ? p.DefaultArgument.String : p.Name)));
            }

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

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload)
            {
                if (!method.IsConstructor)
                {
                    Type type = method.OriginalReturnType.Type;
                    WriteLine("{0}{1}({2});",
                        type.IsPrimitiveType(PrimitiveType.Void) ? string.Empty : "return ",
                        method.Name,
                        string.Join(", ",
                            method.Parameters.Where(
                                p => p.Kind == ParameterKind.Regular).Select(
                                    p => p.Ignore ? p.DefaultArgument.String : p.Name)));
                }
                goto SkipImpl;
            }

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
                else if (method.SynthKind == FunctionSynthKind.AbstractImplCall)
                {
                    GenerateAbstractImplCall(method, @class);
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

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                GenerateEquals(method, @class);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateEquals(Function method, Class @class)
        {
            Class leftHandSide;
            Class rightHandSide;
            if (method.Parameters[0].Type.SkipPointerRefs().TryGetClass(out leftHandSide) &&
                leftHandSide.OriginalPtr == @class.OriginalPtr &&
                method.Parameters[1].Type.SkipPointerRefs().TryGetClass(out rightHandSide) &&
                rightHandSide.OriginalPtr == @class.OriginalPtr)
            {
                NewLine();
                WriteLine("public override bool Equals(object obj)");
                WriteStartBraceIndent();
                if (@class.IsRefType)
                {
                    WriteLine("return this == obj as {0};", @class.Name);
                }
                else
                {
                    WriteLine("if (!(obj is {0})) return false;", @class.Name);
                    WriteLine("return this == ({0}) obj;", @class.Name);
                }
                WriteCloseBraceIndent();
            }
        }

        private void CheckArgumentRange(Function method)
        {
            if (Driver.Options.MarshalCharAsManagedChar)
            {
                foreach (var param in method.Parameters.Where(
                    p => p.Type.IsPrimitiveType(PrimitiveType.Char)))
                {
                    WriteLine("if ({0} < char.MinValue || {0} > sbyte.MaxValue)", param.Name);
                    WriteLineIndent(
                        "throw new global::System.ArgumentException(\"{0} must be in the range {1} - {2}.\");",
                        param.Name, (int) char.MinValue, sbyte.MaxValue);
                }
            }
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

        private void GenerateAbstractImplCall(Function function, Class @class)
        {
            string delegateId;
            Write(GetAbstractCallDelegate(function, @class, out delegateId));
            GenerateFunctionCall(delegateId, function.Parameters, function);
        }

        public string GetAbstractCallDelegate(Function function, Class @class,
            out string delegateId)
        {
            var virtualCallBuilder = new StringBuilder();
            var i = VTables.GetVTableIndex(function, @class);
            virtualCallBuilder.AppendFormat("void* slot = *(void**) ((({0}.Internal*) {1})->vfptr0 + {2} * {3});",
                @class.BaseClass.Name, Helpers.InstanceIdentifier, i, Driver.TargetInfo.PointerWidth / 8);
            virtualCallBuilder.AppendLine();

            string @delegate = GetVTableMethodDelegateName(function.OriginalFunction);
            delegateId = Generator.GeneratedIdentifier(@delegate);

            virtualCallBuilder.AppendFormat(
                "var {1} = ({0}) Marshal.GetDelegateForFunctionPointer(new IntPtr(slot), typeof({0}));",
                @delegate, delegateId);
            virtualCallBuilder.AppendLine();
            return virtualCallBuilder.ToString();
        }

        private void GenerateOperator(Method method, Class @class)
        {
            if (method.SynthKind == FunctionSynthKind.ComplementOperator)
            {
                if (method.Kind == CXXMethodKind.Conversion)
                {
                    // To avoid ambiguity when having the multiple inheritance pass enabled
                    var paramType = method.Parameters[0].Type.SkipPointerRefs().Desugar();
                    Class paramClass;
                    Class @interface = null;
                    if (paramType.TryGetClass(out paramClass))
                        @interface = paramClass.Namespace.Classes.Find(c => c.OriginalClass == paramClass);
                    if (@interface != null)
                        WriteLine("return new {0}(({2}) {1});", method.ConversionType,
                                  method.Parameters[0].Name, @interface.Name);
                    else
                        WriteLine("return new {0}({1});", method.ConversionType,
                                  method.Parameters[0].Name);
                }
                else
                {
                    var @operator = Operators.GetOperatorOverloadPair(method.OperatorKind);

                    WriteLine("return !({0} {1} {2});", method.Parameters[0].Name,
                              @operator, method.Parameters[1].Name);
                }
                return;
            }

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                WriteLine("bool {0}Null = ReferenceEquals({0}, null);", method.Parameters[0].Name);
                WriteLine("bool {0}Null = ReferenceEquals({0}, null);", method.Parameters[1].Name);
                WriteLine("if ({0}Null || {1}Null)", method.Parameters[0].Name, method.Parameters[1].Name);
                WriteLineIndent("return {0}Null && {1}Null;", method.Parameters[0].Name, method.Parameters[1].Name);
            }

            GenerateInternalFunctionCall(method);
        }

        private void GenerateClassConstructor(Method method, Class @class)
        {
            WriteLine("{0} = Marshal.AllocHGlobal({1});", Helpers.InstanceIdentifier,
                @class.Layout.Size);
            WriteLine("{0} = true;", Helpers.OwnsNativeInstanceIdentifier);

            if (method.IsCopyConstructor)
            {
                if (@class.HasNonTrivialCopyConstructor)
                    GenerateInternalFunctionCall(method);
                else
                    WriteLine("*(({0}.Internal*) {2}) = *(({0}.Internal*) {1}.{2});",
                        @class.Name, method.Parameters[0].Name, Helpers.InstanceIdentifier);
            }
            else
            {
                if (!method.IsDefaultConstructor || @class.HasNonTrivialDefaultConstructor)
                    GenerateInternalFunctionCall(method);
            }

            GenerateVTableClassSetupCall(@class);
        }

        public void GenerateInternalFunctionCall(Function function,
            List<Parameter> parameters = null, Type returnType = null)
        {
            if (parameters == null)
                parameters = function.Parameters;

            CheckArgumentRange(function);
            var functionName = string.Format("Internal.{0}",
                GetFunctionNativeIdentifier(function));
            GenerateFunctionCall(functionName, parameters, function, returnType);
        }

        public void GenerateFunctionCall(string functionName, List<Parameter> parameters,
            Function function, Type returnType = null)
        {
            if (function.IsPure)
            {
                WriteLine("throw new System.NotImplementedException();");
                return;
            }

            var retType = function.OriginalReturnType;
            if (returnType == null)
                returnType = retType.Type;

            var method = function as Method;
            var hasThisReturnStructor = method != null && (method.IsConstructor || method.IsDestructor);
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void) && !hasThisReturnStructor;

            var isValueType = false;
            var needsInstance = false;

            Parameter operatorParam = null;
            if (method != null)
            {
                var @class = (Class) method.Namespace;
                isValueType = @class.IsValueType;

                operatorParam = method.Parameters.FirstOrDefault(
                    p => p.Kind == ParameterKind.OperatorParameter);
                needsInstance = !method.IsStatic || operatorParam != null;
            }

            var @params = GenerateFunctionParamsMarshal(parameters, function);

            var originalFunction = function.OriginalFunction ?? function;

            if (originalFunction.HasIndirectReturnTypeParameter)
            {
                var indirectRetType = originalFunction.Parameters.First(
                    parameter => parameter.Kind == ParameterKind.IndirectReturnType);

                Class retClass;
                indirectRetType.Type.Desugar().TryGetClass(out retClass);

                TypeMap typeMap;
                string construct = null;
                if (Driver.TypeDatabase.FindTypeMap(retClass, out typeMap))
                    construct = typeMap.CSharpConstruct();

                if (construct == null)
                {
                    WriteLine("var {0} = new {1}.Internal();", GeneratedIdentifier("ret"),
                        QualifiedIdentifier(retClass.OriginalClass ?? retClass));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(construct))
                        WriteLine("{0} {1};",
                            typeMap.CSharpSignature(new CSharpTypePrinterContext
                            {
                                Type = indirectRetType.Type.Desugar()
                            }),
                            GeneratedIdentifier("ret"));
                    else
                        WriteLine("var {0} = {1};", construct);
                }
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

                name += param.Name;
                names.Add(name);
            }

            var needsFixedThis = needsInstance && isValueType;

            if (originalFunction.HasIndirectReturnTypeParameter)
            {
                var name = string.Format("new IntPtr(&{0})", GeneratedIdentifier("ret"));
                names.Insert(0, name);
            }

            if (needsInstance)
            {
                var instanceIndex = GetInstanceParamIndex(function);

                if (needsFixedThis)
                {
                    names.Insert(instanceIndex, string.Format("new global::System.IntPtr(__instancePtr)"));
                }
                else
                {
                    if (operatorParam != null)
                    {
                        names.Insert(instanceIndex, @params[0].Name);
                    }
                    else
                    {
                        names.Insert(instanceIndex, Helpers.InstanceIdentifier);
                    }
                }
            }

            if (needsFixedThis)
            {
                if (operatorParam == null)
                {
                    WriteLine("fixed (Internal* __instancePtr = &{0})", Helpers.InstanceField);
                    WriteStartBraceIndent();
                }
                else
                {
                    WriteLine("var __instancePtr = &{0}.{1};", operatorParam.Name, Helpers.InstanceField);
                }
            }

            if (needsReturn && !originalFunction.HasIndirectReturnTypeParameter)
                Write("var {0} = ", GeneratedIdentifier("ret"));

            WriteLine("{0}({1});", functionName, string.Join(", ", names));

            var cleanups = new List<TextGenerator>();
            GenerateFunctionCallOutParams(@params, cleanups);

            cleanups.AddRange(
                from param in @params
                select param.Context
                into context
                where context != null && !string.IsNullOrWhiteSpace(context.Cleanup)
                select context.Cleanup);

            foreach (var cleanup in cleanups)
            {
                Write(cleanup);
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
                    if (pointee.TryGetClass(out @class) && @class.IsValueType)
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

                // Special case for indexer - needs to dereference if the internal
                // function is a pointer type and the property is not.
                if (retType.Type.IsAddress() &&
                    retType.Type.GetPointee().Equals(returnType) &&
                    returnType.IsPrimitiveType())
                    WriteLine("return *{0};", marshal.Context.Return);
                else
                    WriteLine("return {0};", marshal.Context.Return);
            }

            if (needsFixedThis && operatorParam == null)
                WriteCloseBraceIndent();
        }

        private int GetInstanceParamIndex(Function function)
        {
            var method = function as Method;

            if (Options.IsMicrosoftAbi)
                return 0;

            var indirectReturnType = method.Parameters.FirstOrDefault(
                parameter => parameter.Kind == ParameterKind.IndirectReturnType);
            var indirectReturnTypeIndex = method.Parameters.IndexOf(indirectReturnType);

            return indirectReturnTypeIndex >= 0 ? ++indirectReturnTypeIndex : 0;
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

        public struct ParamMarshal
        {
            public string Name;
            public Parameter Param;
            public CSharpMarshalContext Context;
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
                if (paramType.Desugar().TryGetClass(out @class) && @class.IsRefType)
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

            WriteLine("var {0} = {1};", argName, marshal.Context.Return);

            return paramMarshal;
        }

        static string GetParameterUsage(ParameterUsage usage)
        {
            switch (usage)
            {
                case ParameterUsage.Out:
                    return "out ";
                case ParameterUsage.InOut:
                    return "ref ";
                default:
                    return string.Empty;
            }
        }

        private string FormatMethodParameters(IEnumerable<Parameter> @params)
        {
            return string.Join(", ",
                from param in @params
                where param.Kind != ParameterKind.IndirectReturnType && !param.Ignore
                let typeName = param.CSharpType(TypePrinter)
                select string.Format("{0}{1} {2}", GetParameterUsage(param.Usage),
                    typeName, param.Name +
                        (param.DefaultArgument == null || !Options.GenerateDefaultValuesForArguments ?
                            string.Empty : " = " + param.DefaultArgument.String)));
        }

        #endregion

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (!typedef.IsGenerated)
                return false;

            GenerateDeclarationCommon(typedef);

            FunctionType functionType;
            TagType tag;

            if (typedef.Type.IsPointerToPrimitiveType(PrimitiveType.Void)
                || typedef.Type.IsPointerTo(out tag))
            {
                PushBlock(CSharpBlockKind.Typedef);
                WriteLine("public class " + typedef.Name + @" { }");
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            else if (typedef.Type.IsPointerTo(out functionType))
            {
                PushBlock(CSharpBlockKind.Typedef);
                var attributedType = typedef.Type.GetPointee() as AttributedType;
                var callingConvention = attributedType == null
                    ? functionType.CallingConvention
                    : ((FunctionType) attributedType.Equivalent.Type).CallingConvention;
                TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                var interopCallConv = callingConvention.ToInteropCallConv();
                if (interopCallConv != System.Runtime.InteropServices.CallingConvention.Winapi)
                    WriteLine(
                        "[UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.{0})]",
                        interopCallConv);
                WriteLine("{0}unsafe {1};",
                    Helpers.GetAccess(typedef.Access),
                    string.Format(TypePrinter.VisitDelegate(functionType).Type,
                        typedef.Name));
                TypePrinter.PopContext();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            return true;
        }

        public void GenerateEnum(Enumeration @enum)
        {
            if (!@enum.IsGenerated) return;

            PushBlock(CSharpBlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            if (@enum.IsFlags)
                WriteLine("[Flags]");

            Write("public enum {0}", @enum.Name);

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int)
                Write(" : {0}", typeName);

            NewLine();

            WriteStartBraceIndent();
            for (var i = 0; i < @enum.Items.Count; ++i)
            {
                var item = @enum.Items[i];
                GenerateInlineSummary(item.Comment);

                var value = @enum.GetItemValueAsString(item);
                Write(item.ExplicitValue
                          ? string.Format("{0} = {1}", item.Name, value)
                          : string.Format("{0}", item.Name));

                if (i < @enum.Items.Count - 1)
                    Write(",");

                NewLine();
            }
            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public static string GetMethodIdentifier(Method method)
        {
            if (method.IsConstructor || method.IsDestructor)
                return method.Namespace.Name;

            return GetFunctionIdentifier(method);
        }

        public static string GetFunctionIdentifier(Function function)
        {
            if (function.IsOperator)
                return Operators.GetOperatorIdentifier(function.OperatorKind);

            return function.Name;
        }

        public static string GetFunctionNativeIdentifier(Function function)
        {
            var functionName = function.Name;

            var method = function as Method;
            if (method != null)
            {
                if (method.IsConstructor && !method.IsCopyConstructor)
                    functionName = "ctor";
                else if (method.IsCopyConstructor)
                    functionName = "cctor";
                else if (method.IsDestructor)
                    functionName = "dtor";
                else
                    functionName = GetMethodIdentifier(method);
            }

            var identifier = functionName;

            if (function.IsOperator)
                identifier = "Operator" + function.OperatorKind;

            var overloads = function.Namespace.GetOverloads(function)
                .ToList();
            var index = overloads.IndexOf(function);

            if (index >= 0)
                identifier += "_" + index.ToString(CultureInfo.InvariantCulture);
            else if (function.Index.HasValue)
                identifier += "_" + function.Index.Value;

            return identifier;
        }

        public void GenerateInternalFunction(Function function)
        {
            if (function.IsPure)
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

                if (library != null)
                    libName = Path.GetFileNameWithoutExtension(library.FileName);
            }
            if (Options.StripLibPrefix && libName != null && libName.Length > 3 && libName.StartsWith("lib"))
            {
                libName = libName.Substring(3);
            }
            if (libName == null)
                libName = Options.SharedLibraryName;

            if (Options.GenerateInternalImports)
                libName = "__Internal";

            Write("[DllImport(\"{0}\", ", libName);

            var callConv = function.CallingConvention.ToInteropCallConv();
            WriteLine("CallingConvention = global::System.Runtime.InteropServices.CallingConvention.{0},",
                callConv);

            WriteLineIndent("EntryPoint=\"{0}\")]", function.Mangled);

            if (function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Bool))
                WriteLine("[return: MarshalAsAttribute(UnmanagedType.I1)]");

            var @params = new List<string>();

            var typePrinter = TypePrinter;
            typePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            var retParam = new Parameter { QualifiedType = function.ReturnType };
            var retType = retParam.CSharpType(typePrinter);

            var method = function as Method;
            var isInstanceMethod = method != null && !method.IsStatic;

            if (isInstanceMethod && Options.IsMicrosoftAbi)
            {
                @params.Add("global::System.IntPtr instance");

                if (method.IsConstructor)
                    retType = "global::System.IntPtr";
            }

            if (!function.HasIndirectReturnTypeParameter &&
                isInstanceMethod && Options.IsItaniumLikeAbi)
                @params.Add("global::System.IntPtr instance");

            foreach (var param in function.Parameters)
            {
                if (param.Kind == ParameterKind.OperatorParameter)
                    continue;

                var typeName = param.CSharpType(typePrinter);

                @params.Add(string.Format("{0} {1}", typeName, param.Name));

                if (param.Kind == ParameterKind.IndirectReturnType &&
                    isInstanceMethod && Options.IsItaniumLikeAbi)
                    @params.Add("global::System.IntPtr instance");
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