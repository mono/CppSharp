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

namespace CppSharp.Generators.CSharp
{
    public static class Helpers
    {
        // from https://github.com/mono/mono/blob/master/mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
        private static readonly string[] Keywords =
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

        public static readonly string InternalStruct = Generator.GeneratedIdentifier("Internal");
        public static readonly string InstanceField = Generator.GeneratedIdentifier("instance");
        public static readonly string InstanceIdentifier = Generator.GeneratedIdentifier("Instance");
        public static readonly string PointerAdjustmentIdentifier = Generator.GeneratedIdentifier("PointerAdjustment");
        public static readonly string ReturnIdentifier = Generator.GeneratedIdentifier("ret");
        public static readonly string DummyIdentifier = Generator.GeneratedIdentifier("dummy");
        public static readonly string TargetIdentifier = Generator.GeneratedIdentifier("target");
        public static readonly string SlotIdentifier = Generator.GeneratedIdentifier("slot");

        public static readonly string OwnsNativeInstanceIdentifier = Generator.GeneratedIdentifier("ownsNativeInstance");

        public static readonly string CreateInstanceIdentifier = Generator.GeneratedIdentifier("CreateInstance");

        public static string GetAccess(AccessSpecifier accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case AccessSpecifier.Private:
                case AccessSpecifier.Internal:
                    return "internal ";
                case AccessSpecifier.Protected:
                    return "protected ";
                default:
                    return "public ";
            }
        }

        public static string FormatTypesStringForIdentifier(StringBuilder types)
        {
            return types.Replace("global::System.", string.Empty).Replace("*", "Ptr")
                .Replace('.', '_').Replace(' ', '_').Replace("::", "_").ToString();
        }

        public static string GetSuffixForInternal(Declaration context)
        {
            var specialization = context as ClassTemplateSpecialization;

            if (specialization == null ||
                specialization.TemplatedDecl.TemplatedClass.Fields.All(
                f => !(f.Type.Desugar() is TemplateParameterType)))
                return string.Empty;

            if (specialization.Arguments.All(
                a => a.Type.Type != null && a.Type.Type.IsAddress()))
                return "_Ptr";

            var suffixBuilder = new StringBuilder(specialization.USR);
            for (int i = 0; i < suffixBuilder.Length; i++)
                if (!char.IsLetterOrDigit(suffixBuilder[i]))
                    suffixBuilder[i] = '_';
            const int maxCSharpIdentifierLength = 480;
            if (suffixBuilder.Length > maxCSharpIdentifierLength)
                return suffixBuilder.Remove(maxCSharpIdentifierLength,
                    suffixBuilder.Length - maxCSharpIdentifierLength).ToString();
            return suffixBuilder.ToString();
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

    public class CSharpSources : Template
    {
        public CSharpTypePrinter TypePrinter { get; private set; }
        public CSharpExpressionPrinter ExpressionPrinter { get; private set; }

        public override string FileExtension
        {
            get { return "cs"; }
        }

        public CSharpSources(BindingContext context, IEnumerable<TranslationUnit> units,
            CSharpTypePrinter typePrinter, CSharpExpressionPrinter expressionPrinter)
            : base(context, units)
        {
            TypePrinter = typePrinter;
            ExpressionPrinter = expressionPrinter;
        }

        #region Identifiers

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

            var module = TranslationUnits.Count == 0 ?
                Context.Options.SystemModule : TranslationUnit.Module;
            if (!string.IsNullOrEmpty(module.OutputNamespace))
            {
                PushBlock(CSharpBlockKind.Namespace);
                WriteLine("namespace {0}", module.OutputNamespace);
                WriteStartBraceIndent();
            }

            foreach (var unit in TranslationUnits)
            {
                GenerateDeclContext(unit);
            }

            if (!string.IsNullOrEmpty(module.OutputNamespace))
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateHeader()
        {
            PushBlock(BlockKind.Header);
            WriteLine("//----------------------------------------------------------------------------");
            WriteLine("// <auto-generated>");
            WriteLine("// This is autogenerated code by CppSharp.");
            WriteLine("// Do not edit this file or all your changes will be lost after re-generation.");
            WriteLine("// </auto-generated>");
            WriteLine("//----------------------------------------------------------------------------");
            PopBlock();
        }

        private void GenerateDeclContext(DeclarationContext context)
        {
            var isNamespace = context is Namespace;
            var isTranslationUnit = context is TranslationUnit;

            var shouldGenerateNamespace = isNamespace && !isTranslationUnit &&
                context.Declarations.Any(d => d.IsGenerated || (d is Class && !d.IsIncomplete));

            if (shouldGenerateNamespace)
            {
                PushBlock(CSharpBlockKind.Namespace);
                WriteLine("namespace {0}", context.Name);
                WriteStartBraceIndent();
            }

            // Generate all the enum declarations.
            foreach (var @enum in context.Enums)
            {
                if (@enum.IsIncomplete)
                    continue;

                GenerateEnum(@enum);
            }

            // Generate all the typedef declarations.
            foreach (var typedef in context.Typedefs)
            {
                GenerateTypedef(typedef);
            }

            // Generate all the struct/class declarations.
            foreach (var @class in context.Classes.Where(c => !c.IsIncomplete))
            {
                if (@class.IsInterface)
                {
                    GenerateInterface(@class);
                    continue;
                }
                var specialization = @class as ClassTemplateSpecialization;
                if (specialization != null &&
                    specialization.SpecializationKind ==
                        TemplateSpecializationKind.ExplicitInstantiationDeclaration)
                    continue;
                if (!@class.IsDependent)
                {
                    GenerateClass(@class);
                    continue;
                }
                if (!(@class.Namespace is Class))
                    GenerateClassTemplateSpecializationInternal(@class);
            }

            if (context.HasFunctions || (!(context is Class) && context.Variables.Any(
                v => v.IsGenerated && v.Access == AccessSpecifier.Public)))
            {
                PushBlock(CSharpBlockKind.Functions);
                var parentName = Helpers.SafeIdentifier(context.TranslationUnit.FileNameWithoutExtension);
                WriteLine("public unsafe partial class {0}", parentName);
                WriteStartBraceIndent();

                PushBlock(CSharpBlockKind.InternalsClass);
                GenerateClassInternalHead();
                WriteStartBraceIndent();

                // Generate all the internal function declarations.
                foreach (var function in context.Functions)
                {
                    if ((!function.IsGenerated && !function.IsInternal) || function.IsSynthetized) continue;

                    GenerateInternalFunction(function);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);

                foreach (var function in context.Functions)
                {
                    if (!function.IsGenerated) continue;

                    GenerateFunction(function, parentName);
                }

                foreach (var variable in context.Variables.Where(
                    v => v.IsGenerated && v.Access == AccessSpecifier.Public))
                    GenerateVariable(null, variable);

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

        private void GenerateClassTemplateSpecializationInternal(Class classTemplate)
        {
            if (classTemplate.Specializations.Count == 0)
                return;

            List<ClassTemplateSpecialization> specializations;
            if (classTemplate.Fields.Any(
                f => f.Type.Desugar() is TemplateParameterType))
                specializations = classTemplate.Specializations;
            else
            {
                specializations = new List<ClassTemplateSpecialization>();
                var specialization = classTemplate.Specializations.FirstOrDefault(s => !s.Ignore);
                if (specialization == null)
                    specializations.Add(classTemplate.Specializations[0]);
                else
                    specializations.Add(specialization);
            }

            bool generateClass = specializations.Any(s => s.IsGenerated);
            if (!generateClass)
            {
                PushBlock(CSharpBlockKind.Namespace);
                WriteLine("namespace {0}{1}",
                    classTemplate.OriginalNamespace is Class ?
                        classTemplate.OriginalNamespace.Name + '_' : string.Empty,
                    classTemplate.Name);
                WriteStartBraceIndent();
            }

            foreach (var specialization in specializations)
            {
                if (specialization.Ignore)
                    GenerateClassInternals(specialization);
                else
                    GenerateClass(specialization);
            }

            foreach (var nestedClass in classTemplate.Classes)
            {
                NewLine();
                GenerateClassProlog(nestedClass);
                NewLine();
                WriteStartBraceIndent();
                GenerateClassInternals(nestedClass);
                WriteCloseBraceIndent();
            }

            if (!generateClass)
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
            if (Options.OutputDebug && !string.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public void GenerateComment(RawComment comment)
        {
            if (comment.FullComment != null)
            {
                PushBlock(BlockKind.BlockComment);
                WriteLine(comment.FullComment.CommentToString(Options.CommentPrefix));
                PopBlock();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(comment.BriefText))
                    return;

                PushBlock(BlockKind.BlockComment);
                WriteLine("{0} <summary>", Options.CommentPrefix);
                foreach (string line in HtmlEncoder.HtmlEncode(comment.BriefText).Split(
                                            Environment.NewLine.ToCharArray()))
                    WriteLine("{0} <para>{1}</para>", Options.CommentPrefix, line);
                WriteLine("{0} </summary>", Options.CommentPrefix);
                PopBlock();
            }
        }

        public void GenerateInlineSummary(RawComment comment)
        {
            if (comment == null) return;

            if (string.IsNullOrWhiteSpace(comment.BriefText))
                return;

            PushBlock(BlockKind.InlineComment);
            if (comment.BriefText.Contains("\n"))
            {
                WriteLine("{0} <summary>", Options.CommentPrefix);
                foreach (string line in HtmlEncoder.HtmlEncode(comment.BriefText).Split(
                                            Environment.NewLine.ToCharArray()))
                    WriteLine("{0} <para>{1}</para>", Options.CommentPrefix, line);
                WriteLine("{0} </summary>", Options.CommentPrefix);
            }
            else
            {
                WriteLine("{0} <summary>{1}</summary>", Options.CommentPrefix, comment.BriefText);
            }
            PopBlock();
        }

        #region Classes

        public void GenerateClass(Class @class)
        {
            if (@class.IsIncomplete)
                return;

            foreach (var nestedTemplate in @class.Classes.Where(c => !c.IsIncomplete && c.IsDependent))
                GenerateClassTemplateSpecializationInternal(nestedTemplate);

            System.Type typeMap = null;
            if (Context.TypeMaps.TypeMaps.ContainsKey(@class.Name))
            {
                typeMap = Context.TypeMaps.TypeMaps[@class.Name];
                // disable the type map for the mapped class itself so that operator params are not mapped
                Context.TypeMaps.TypeMaps.Remove(@class.Name);
            }

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
                        WriteLine("private {0}.{1} {2};", @class.Name, Helpers.InternalStruct,
                            Helpers.InstanceField);
                        WriteLine("public {0}.{1} {2} {{ get {{ return {3}; }} }}", @class.Name,
                            Helpers.InternalStruct, Helpers.InstanceIdentifier, Helpers.InstanceField);
                    }
                    else
                    {
                        WriteLine("public {0} {1} {{ get; protected set; }}",
                            CSharpTypePrinter.IntPtrType, Helpers.InstanceIdentifier);

                        PopBlock(NewLineKind.BeforeNextBlock);

                        PushBlock(CSharpBlockKind.Field);

                        WriteLine("protected int {0};", Helpers.PointerAdjustmentIdentifier);

                        // use interfaces if any - derived types with a secondary base this class must be compatible with the map
                        var @interface = @class.Namespace.Classes.Find(c => c.OriginalClass == @class);
                        WriteLine(
                            "public static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, {0}> NativeToManagedMap = new System.Collections.Concurrent.ConcurrentDictionary<IntPtr, {0}>();",
                            @interface != null ? @interface.Name : @class.Name);
                        WriteLine("protected void*[] __OriginalVTables;");
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

                if (@class.IsDynamic)
                    GenerateVTable(@class);
            }
        exit:
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            if (typeMap != null)
                Context.TypeMaps.TypeMaps.Add(@class.Name, typeMap);
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
            foreach (var prop in @class.Properties.Where(p => p.IsGenerated && p.Access == AccessSpecifier.Public))
            {
                PushBlock(CSharpBlockKind.Property);
                var type = prop.Type;
                if (prop.Parameters.Count > 0 && prop.Type.IsPointerToPrimitiveType())
                    type = ((PointerType) prop.Type).Pointee;
                GenerateDeclarationCommon(prop);
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

        public void GenerateClassInternals(Class @class)
        {
            PushBlock(CSharpBlockKind.InternalsClass);
            WriteLine("[StructLayout(LayoutKind.Explicit, Size = {0})]",
                @class.Layout.Size);

            GenerateClassInternalHead(@class);
            WriteStartBraceIndent();

            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            foreach (var field in @class.Layout.Fields)
                GenerateClassInternalsField(field);
            if (@class.IsGenerated)
            {
                var functions = GatherClassInternalFunctions(@class);

                foreach (var function in functions)
                    GenerateInternalFunction(function);
            }

            TypePrinter.PopContext();

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private IEnumerable<Function> GatherClassInternalFunctions(Class @class,
            bool includeCtors = true)
        {
            var functions = new List<Function>();
            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    functions.AddRange(GatherClassInternalFunctions(@base.Class, false));

            Action<Method> tryAddOverload = method =>
            {
                if (method.IsSynthetized &&
                    method.SynthKind != FunctionSynthKind.AdjustedMethod)
                    return;

                if (method.IsProxy ||
                    (method.IsVirtual && !method.IsOperator &&
                    // virtual destructors in abstract classes may lack a pointer in the v-table
                    // so they have to be called by symbol and therefore not ignored
                    !(method.IsDestructor && @class.IsAbstract)))
                    return;

                functions.Add(method);
            };

            if (includeCtors)
            {
                foreach (var ctor in @class.Constructors)
                {
                    if (@class.IsStatic || ctor.IsMoveConstructor)
                        continue;

                    if (!ctor.IsGenerated)
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

        private IEnumerable<string> GatherInternalParams(Function function, out CSharpTypePrinterResult retType)
        {
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            retType = function.ReturnType.CSharpType(TypePrinter);

            var @params = function.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi).Select(p =>
                string.Format("{0} {1}", p.CSharpType(TypePrinter), p.Name)).ToList();

            TypePrinter.PopContext();

            return @params;
        }

        private void GenerateClassInternalHead(Class @class = null)
        {
            Write("public ");

            var isSpecialization = @class is ClassTemplateSpecialization;
            if (@class != null && @class.NeedsBase && !@class.BaseClass.IsInterface & !isSpecialization)
                Write("new ");

            var suffix = Helpers.GetSuffixForInternal(@class);
            WriteLine("{0}partial struct {1}{2}",
                isSpecialization ? "unsafe " : string.Empty, Helpers.InternalStruct, suffix);
        }

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;
            return @class.IsValueType || !@class.HasBase || !@class.HasRefBase();
        }

        public void GenerateClassProlog(Class @class)
        {
            // private classes must be visible to because the internal structs can be used in dependencies
            // the proper fix is InternalsVisibleTo
            Write(@class.Access == AccessSpecifier.Protected ? "protected " : "public ");
            if (@class.Access == AccessSpecifier.Protected)
                Write("internal ");
            Write("unsafe ");

            if (@class.IsAbstract)
                Write("abstract ");

            if (@class.IsStatic)
                Write("static ");

            // This token needs to directly precede the "class" token.
            Write("partial ");

            Write(@class.IsInterface ? "interface " : (@class.IsValueType ? "struct " : "class "));
            Write("{0}", Helpers.SafeIdentifier(@class.Name));

            var bases = new List<string>();

            if (@class.NeedsBase)
            {
                bases.AddRange(
                    from @base in @class.Bases
                    where @base.IsClass
                    select @base.Class.Visit(TypePrinter).Type);
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

        public void GenerateClassFields(Class owner, Class @class,
            Action<Class, Field> action, bool nativeFields = false)
        {
            foreach (var @base in @class.Bases.Where(b => b.Class != null))
            {
                TypeMap typeMap;
                if ((!Context.TypeMaps.FindTypeMap(@base.Type, out typeMap) && !@base.Class.IsDeclared) ||
                    @base.Class.OriginalClass == @class)
                    continue;

                GenerateClassFields(owner, @base.Class, action, nativeFields);
            }

            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field, nativeFields)) continue;
                action(owner, field);
            }
        }

        private void GenerateClassInternalsField(LayoutField field)
        {
            Declaration decl;
            field.QualifiedType.Type.TryGetDeclaration(out decl);

            var arrayType = field.QualifiedType.Type.Desugar() as ArrayType;
            var coreType = field.QualifiedType.Type.Desugar();
            if (arrayType != null && arrayType.SizeType == ArrayType.ArraySize.Constant)
                coreType = arrayType.Type.Desugar();
            // we do not support the primitives below yet because their representation in C# is problematic
            if (coreType.IsPrimitiveType(PrimitiveType.LongDouble) ||
                coreType.IsPrimitiveType(PrimitiveType.Int128) ||
                coreType.IsPrimitiveType(PrimitiveType.UInt128) ||
                coreType.IsPrimitiveType(PrimitiveType.Half))
                return;

            var safeIdentifier = Helpers.SafeIdentifier(field.Name);

            if(safeIdentifier.All(c => c.Equals('_')))
            {
                safeIdentifier = Helpers.SafeIdentifier(field.Name);
            }

            PushBlock(CSharpBlockKind.Field);

            WriteLine("[FieldOffset({0})]", field.Offset);

            TypePrinter.PushMarshalKind(CSharpMarshalKind.NativeField);
            var fieldTypePrinted = field.QualifiedType.CSharpType(TypePrinter);
            TypePrinter.PopMarshalKind();

            var fieldType = field.QualifiedType.Type.Desugar().IsAddress() ?
                CSharpTypePrinter.IntPtrType : fieldTypePrinted.Type;

            var fieldName = safeIdentifier;
            if (!string.IsNullOrWhiteSpace(fieldTypePrinted.NameSuffix))
                fieldName += fieldTypePrinted.NameSuffix;

            var access = decl != null && !decl.IsGenerated ? "internal" : "public";
            if (field.Expression != null)
            {
                var fieldValuePrinted = field.Expression.CSharpValue(ExpressionPrinter);
                Write("{0} {1} {2} = {3};", access, fieldType, fieldName, fieldValuePrinted);
            }
            else
            {
                Write("{0} {1} {2};", access, fieldType, fieldName);
            }

            PopBlock(NewLineKind.BeforeNextBlock);

            // Workaround a bug in Mono when handling fixed arrays in P/Invoke declarations.
            // https://bugzilla.xamarin.com/show_bug.cgi?id=33571
            if (arrayType != null && arrayType.SizeType == ArrayType.ArraySize.Constant &&
                arrayType.Size > 0)
            {
                for (var i = 1; i < arrayType.Size; ++i)
                {
                    var dummy = new LayoutField
                    {
                        Offset = (uint) (field.Offset + i * arrayType.ElementSize / 8),
                        QualifiedType = new QualifiedType(arrayType.Type),
                        Name = string.Format("{0}_{1}_{2}", Helpers.DummyIdentifier,
                            safeIdentifier, i),
                        FieldPtr = field.FieldPtr
                    };

                    GenerateClassInternalsField(dummy);
                }
            }
        }

        private void GenerateClassField(Field field, bool @public = false)
        {
            PushBlock(CSharpBlockKind.Field);

            GenerateDeclarationCommon(field);

            WriteLine("{0} {1} {2};", @public ? "public" : "private",
                field.Type, field.Name);

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #endregion

        private void GeneratePropertySetter<T>(T decl,
            Class @class, bool isAbstract = false, Property property = null)
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

            var ctx = new CSharpMarshalContext(Context)
            {
                Parameter = param,
                ArgName = param.Name,
            };

            if (decl is Function)
            {
                var function = decl as Function;
                if (isAbstract)
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

                if (!property.Type.Equals(param.Type) && property.Type.IsEnumType())
                    param.Name = ctx.ArgName = "&" + param.Name;

                var method = function as Method;
                if (function.SynthKind == FunctionSynthKind.AbstractImplCall)
                    GenerateVirtualPropertyCall(method, @class.BaseClass, property,
                        new List<Parameter> { param });
                else if (method != null && method.IsVirtual)
                    GenerateVirtualPropertyCall(method, @class, property, new List<Parameter> { param });
                else
                {
                    if (method != null && method.OperatorKind == CXXOperatorKind.Subscript)
                    {
                        if (method.OperatorKind == CXXOperatorKind.Subscript)
                        {
                            GenerateIndexerSetter(method);
                        }
                        else
                        {
                            GenerateInternalFunctionCall(function, new List<Parameter> { param });
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

                ctx.Kind = CSharpMarshalKind.NativeField;
                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                ctx.Declaration = field;

                var arrayType = field.Type as ArrayType;

                if (arrayType != null && @class.IsValueType)
                {
                    ctx.ReturnVarName = HandleValueArray(arrayType, field);
                }
                else
                {
                    var name = @class.Layout.Fields.First(f => f.FieldPtr == field.OriginalPtr).Name;
                    ctx.ReturnVarName = string.Format("{0}{1}{2}",
                        @class.IsValueType
                            ? Helpers.InstanceField
                            : string.Format("(({0}{1}*) {2})", Helpers.InternalStruct,
                                Helpers.GetSuffixForInternal(@class), Helpers.InstanceIdentifier),
                        @class.IsValueType ? "." : "->",
                        Helpers.SafeIdentifier(name));
                }
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (ctx.HasCodeBlock)
                    PushIndent();

                if (marshal.Context.Return.StringBuilder.Length > 0)
                {
                    WriteLine("{0} = {1}{2};", ctx.ReturnVarName,
                        field.Type.IsPointer() && field.Type.GetFinalPointee().IsPrimitiveType() &&
                        !CSharpTypePrinter.IsConstCharString(field.Type) ?
                            string.Format("({0}) ", CSharpTypePrinter.IntPtrType) :
                            string.Empty,
                        marshal.Context.Return);
                }

                if ((arrayType != null && @class.IsValueType) || ctx.HasCodeBlock)
                    WriteCloseBraceIndent();

                WriteCloseBraceIndent();
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private string HandleValueArray(ArrayType arrayType, Field field)
        {
            var originalType = new PointerType(new QualifiedType(arrayType.Type));
            var arrPtr = Generator.GeneratedIdentifier("arrPtr");
            var finalElementType = (arrayType.Type.GetFinalPointee() ?? arrayType.Type);
            var isChar = finalElementType.IsPrimitiveType(PrimitiveType.Char);

            string type;
            if (Context.Options.MarshalCharAsManagedChar && isChar)
            {
                var typePrinter = new CSharpTypePrinter(Context);
                typePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                type = originalType.Visit(typePrinter).Type;
            }
            else
            {
                type = originalType.ToString();
            }

            var name = ((Class) field.Namespace).Layout.Fields.First(
                f => f.FieldPtr == field.OriginalPtr).Name;
            WriteLine(string.Format("fixed ({0} {1} = {2}.{3})",
                type, arrPtr, Helpers.InstanceField, Helpers.SafeIdentifier(name)));
            WriteStartBraceIndent();
            return arrPtr;
        }

        private bool WrapSetterArrayOfPointers(string name, Type fieldType)
        {
            var arrayType = fieldType as ArrayType;
            if (arrayType == null || !arrayType.Type.IsPointerToPrimitiveType())
                return false;

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

        private void GenerateIndexerSetter(Function function)
        {
            Type type;
            function.Type.IsPointerTo(out type);
            PrimitiveType primitiveType;
            var internalFunction = GetFunctionNativeIdentifier(function);
            if (type.IsPrimitiveType(out primitiveType))
            {
                WriteLine("*{0}{1}.{2}({3}, {4}) = value;",
                    Helpers.InternalStruct, Helpers.GetSuffixForInternal(function.Namespace),
                    internalFunction, GetInstanceParam(function), function.Parameters[0].Name);
            }
            else
            {
                var typeString = type.ToString();
                Class @class;
                var isValueType = (type.GetFinalPointee() ?? type).TryGetClass(out @class) &&
                    @class.IsValueType;
                var paramMarshal = GenerateFunctionParamMarshal(function.Parameters[0], 0, function);
                var suffix = Helpers.GetSuffixForInternal(function.Namespace);
                WriteLine("*({0}.{1}{2}*) {1}{2}.{3}({4}, {5}) = {6}value.{7};",
                    typeString, Helpers.InternalStruct, suffix, internalFunction, GetInstanceParam(function),
                    paramMarshal.Context == null ? paramMarshal.Name : paramMarshal.Context.Return,
                    isValueType ? string.Empty :
                        string.Format("*({0}.{1}{2}*) ", typeString, Helpers.InternalStruct, suffix),
                    Helpers.InstanceIdentifier);
            }
        }

        private void GeneratePropertyGetter<T>(QualifiedType returnType, T decl,
            Class @class, bool isAbstract = false, Property property = null)
            where T : Declaration, ITypedDecl
        {
            PushBlock(CSharpBlockKind.Method);
            Write("get");

            if (property != null && property.GetMethod != null &&
                property.GetMethod.SynthKind == FunctionSynthKind.InterfaceInstance)
            {
                NewLine();
                WriteStartBraceIndent();
                var to = ((Class) property.OriginalNamespace).OriginalClass;
                var baseOffset = GetOffsetToBase(@class, to);
                WriteLine("return {0} + {1};", Helpers.InstanceIdentifier, baseOffset);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
                return;
            }

            if (decl is Function)
            {
                var function = decl as Function;
                if (isAbstract)
                {
                    Write(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteStartBraceIndent();

                var method = function as Method;
                if (function.SynthKind == FunctionSynthKind.AbstractImplCall)
                    GenerateVirtualPropertyCall(method, @class.BaseClass, property);
                else if (method != null && method.IsVirtual)
                    GenerateVirtualPropertyCall(method, @class, property);
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

                var name = @class.Layout.Fields.First(f => f.FieldPtr == field.OriginalPtr).Name;
                var ctx = new CSharpMarshalContext(Context)
                {
                    Kind = CSharpMarshalKind.NativeField,
                    ArgName = decl.Name,
                    Declaration = decl,
                    ReturnVarName = string.Format("{0}{1}{2}",
                        @class.IsValueType
                            ? Helpers.InstanceField
                            : string.Format("(({0}{1}*) {2})", Helpers.InternalStruct,
                                Helpers.GetSuffixForInternal(@class), Helpers.InstanceIdentifier),
                        @class.IsValueType ? "." : "->",
                        Helpers.SafeIdentifier(name)),
                    ReturnType = decl.QualifiedType
                };

                var arrayType = field.Type as ArrayType;

                if (arrayType != null && @class.IsValueType)
                    ctx.ReturnVarName = HandleValueArray(arrayType, field);

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                decl.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (ctx.HasCodeBlock)
                    PushIndent();

                var @return = marshal.Context.Return.ToString();
                if (field.Type.IsPointer())
                {
                    var final = field.Type.GetFinalPointee().Desugar();
                    if (final.IsPrimitiveType() && !final.IsPrimitiveType(PrimitiveType.Void) &&
                        (!final.IsPrimitiveType(PrimitiveType.Char) &&
                         !final.IsPrimitiveType(PrimitiveType.WideChar) ||
                         (!Context.Options.MarshalCharAsManagedChar &&
                          !((PointerType) field.Type).QualifiedPointee.Qualifiers.IsConst)))
                        @return = string.Format("({0}*) {1}", field.Type.GetPointee().Desugar(), @return);
                    if (!((PointerType) field.Type).QualifiedPointee.Qualifiers.IsConst &&
                        final.IsPrimitiveType(PrimitiveType.WideChar))
                        @return = string.Format("({0}*) {1}", field.Type.GetPointee().Desugar(), @return);
                }
                WriteLine("return {0};", @return);

                if ((arrayType != null && @class.IsValueType) || ctx.HasCodeBlock)
                    WriteCloseBraceIndent();
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteStartBraceIndent();
                var var = decl as Variable;

                TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

                var location = string.Format("CppSharp.SymbolResolver.ResolveSymbol(\"{0}\", \"{1}\")",
                    GetLibraryOf(decl), var.Mangled);

                var arrayType = decl.Type as ArrayType;
                var isRefTypeArray = arrayType != null && @class != null && @class.IsRefType;
                if (isRefTypeArray)
                    WriteLine("var {0} = {1}{2};", Generator.GeneratedIdentifier("ptr"),
                        arrayType.Type.IsPrimitiveType(PrimitiveType.Char) &&
                        arrayType.QualifiedType.Qualifiers.IsConst
                            ? string.Empty : "(byte*)",
                        location);
                else
                    WriteLine("var {0} = ({1}*){2};", Generator.GeneratedIdentifier("ptr"),
                        @var.Type, location);

                TypePrinter.PopContext();

                var ctx = new CSharpMarshalContext(Context)
                {
                    ArgName = decl.Name,
                    ReturnVarName = (isRefTypeArray ||
                        (arrayType != null && arrayType.Type.Desugar().IsPrimitiveType()) ? string.Empty : "*")
                        + Generator.GeneratedIdentifier("ptr"),
                    ReturnType = new QualifiedType(var.Type)
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                decl.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (ctx.HasCodeBlock)
                    PushIndent();

                WriteLine("return {0};", marshal.Context.Return);

                if (ctx.HasCodeBlock)
                    WriteCloseBraceIndent();
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

                GenerateVariable(@class, variable);
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
                    var name = @class.Layout.Fields.First(f => f.FieldPtr == prop.Field.OriginalPtr).Name;
                    GenerateClassField(prop.Field);
                    WriteLine("private bool {0};",
                        GeneratedIdentifier(string.Format("{0}Initialised", name)));
                }

                GenerateDeclarationCommon(prop);
                if (prop.ExplicitInterfaceImpl == null)
                {
                    Write(Helpers.GetAccess(GetValidPropertyAccess(prop)));

                    if (prop.IsStatic)
                        Write("static ");

                    // check if overriding a property from a secondary base
                    Property rootBaseProperty;
                    var isOverride = prop.IsOverride &&
                        (rootBaseProperty = @class.GetBaseProperty(prop, true, true)) != null &&
                        (rootBaseProperty.IsVirtual || rootBaseProperty.IsPure);

                    if (isOverride)
                        Write("override ");
                    else if (prop.IsPure)
                        Write("abstract ");

                    if (prop.IsVirtual && !isOverride && !prop.IsPure)
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
                        GeneratePropertySetter(prop.Field, @class);
                }
                else
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.QualifiedType, prop.GetMethod, @class, prop.IsPure, prop);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.SetMethod, @class, prop.IsPure, prop);
                }

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private string GetPropertyName(Property prop)
        {
            var isIndexer = prop.Parameters.Count != 0;
            if (!isIndexer)
                return prop.Name;

            var @params = prop.Parameters.Select(param => {
                var p = new Parameter(param);
                p.Usage = ParameterUsage.In;
                return p;
            });

            return string.Format("this[{0}]", FormatMethodParameters(@params));
        }

        private void GenerateVariable(Class @class, Variable variable)
        {
            PushBlock(CSharpBlockKind.Variable);
            
            GenerateDeclarationCommon(variable);
            WriteLine("public static {0} {1}", variable.Type, variable.Name);
            WriteStartBraceIndent();

            GeneratePropertyGetter(variable.QualifiedType, variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst)
                GeneratePropertySetter(variable, @class);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #region Virtual Tables

        public List<VTableComponent> GetUniqueVTableMethodEntries(Class @class)
        {
            var uniqueEntries = new OrderedSet<VTableComponent>();
            var vTableMethodEntries = VTables.GatherVTableMethodEntries(@class);
            foreach (var entry in vTableMethodEntries.Where(e => !e.IsIgnored() && !e.Method.IsOperator))
                uniqueEntries.Add(entry);

            return uniqueEntries.ToList();
        }

        public void GenerateVTable(Class @class)
        {
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

            WriteLine("private static void*[] __ManagedVTables;");
            if (wrappedEntries.Any(e => e.Method.IsDestructor))
                WriteLine("private static void*[] __ManagedVTablesDtorOnly;");
            WriteLine("private static void*[] _Thunks;");
            NewLine();

            GenerateVTableClassSetup(@class, wrappedEntries);

            WriteLine("#endregion");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVTableClassSetup(Class @class, IList<VTableComponent> wrappedEntries)
        {
            const string destructorOnly = "destructorOnly";
            WriteLine("private void SetupVTables(bool {0} = false)", destructorOnly);
            WriteStartBraceIndent();

            WriteLine("if (__OriginalVTables != null)");
            WriteLineIndent("return;");

            WriteLine("var native = ({0}{1}*) {2}.ToPointer();", Helpers.InternalStruct,
                Helpers.GetSuffixForInternal(@class), Helpers.InstanceIdentifier);
            NewLine();

            SaveOriginalVTablePointers(@class);

            NewLine();

            var hasVirtualDtor = wrappedEntries.Any(e => e.Method.IsDestructor);
            if (!hasVirtualDtor)
            {
                WriteLine("if ({0})", destructorOnly);
                WriteLineIndent("return;");
            }

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

            if (hasVirtualDtor)
            {
                WriteLine("if ({0})", destructorOnly);
                WriteStartBraceIndent();
                WriteLine("if (__ManagedVTablesDtorOnly == null)");
                WriteStartBraceIndent();

                AllocateNewVTables(@class, wrappedEntries, true);

                WriteCloseBraceIndent();
                WriteLine("else");
                WriteStartBraceIndent();
            }
            WriteLine("if (__ManagedVTables == null)");
            WriteStartBraceIndent();

            AllocateNewVTables(@class, wrappedEntries, false);

            if (hasVirtualDtor)
                WriteCloseBraceIndent();

            WriteCloseBraceIndent();
            NewLine();
        }

        private void AllocateNewVTables(Class @class, IList<VTableComponent> wrappedEntries,
            bool destructorOnly)
        {
            if (Context.ParserOptions.IsMicrosoftAbi)
                AllocateNewVTablesMS(@class, wrappedEntries, destructorOnly);
            else
                AllocateNewVTablesItanium(@class, wrappedEntries, destructorOnly);
        }

        private void SaveOriginalVTablePointers(Class @class)
        {
            var suffix = Helpers.GetSuffixForInternal(@class);
            if (Context.ParserOptions.IsMicrosoftAbi)
                WriteLine("__OriginalVTables = new void*[] {{ {0} }};",
                    string.Join(", ",
                        @class.Layout.VTablePointers.Select(v => string.Format(
                            "(({0}{1}*) native)->{2}.ToPointer()",
                                Helpers.InternalStruct, suffix, v.Name))));
            else
                WriteLine(
                    "__OriginalVTables = new void*[] {{ (({0}{1}*) native)->{2}.ToPointer() }};",
                    Helpers.InternalStruct, suffix, @class.Layout.VTablePointers[0].Name);
        }

        private void AllocateNewVTablesMS(Class @class, IList<VTableComponent> wrappedEntries,
            bool destructorOnly)
        {
            var managedVTables = destructorOnly ? "__ManagedVTablesDtorOnly" : "__ManagedVTables";
            WriteLine("{0} = new void*[{1}];", managedVTables, @class.Layout.VFTables.Count);

            for (int i = 0; i < @class.Layout.VFTables.Count; i++)
            {
                var vfptr = @class.Layout.VFTables[i];
                var size = vfptr.Layout.Components.Count;
                WriteLine("var vfptr{0} = Marshal.AllocHGlobal({1} * {2});",
                    i, size, Context.TargetInfo.PointerWidth / 8);
                WriteLine("{0}[{1}] = vfptr{1}.ToPointer();", managedVTables, i);

                AllocateNewVTableEntries(vfptr.Layout.Components, wrappedEntries,
                    @class.Layout.VTablePointers[i].Name, i, destructorOnly);
            }

            WriteCloseBraceIndent();
            NewLine();

            for (int i = 0; i < @class.Layout.VTablePointers.Count; i++)
                WriteLine("native->{0} = new IntPtr({1}[{2}]);",
                    @class.Layout.VTablePointers[i].Name, managedVTables, i);
        }

        private void AllocateNewVTablesItanium(Class @class, IList<VTableComponent> wrappedEntries,
            bool destructorOnly)
        {
            var managedVTables = destructorOnly ? "__ManagedVTablesDtorOnly" : "__ManagedVTables";
            WriteLine("{0} = new void*[1];", managedVTables);

            var size = @class.Layout.Layout.Components.Count;
            var pointerSize = Context.TargetInfo.PointerWidth / 8;
            WriteLine("var vtptr = Marshal.AllocHGlobal({0} * {1});", size, pointerSize);

            WriteLine("var vfptr0 = vtptr + {0} * {1};", VTables.ItaniumOffsetToTopAndRTTI, pointerSize);
            WriteLine("{0}[0] = vfptr0.ToPointer();", managedVTables);

            AllocateNewVTableEntries(@class.Layout.Layout.Components,
                wrappedEntries, @class.Layout.VTablePointers[0].Name, 0, destructorOnly);

            WriteCloseBraceIndent();
            NewLine();

            WriteLine("native->{0} = new IntPtr({1}[0]);",
                @class.Layout.VTablePointers[0].Name, managedVTables);
        }

        private void AllocateNewVTableEntries(IList<VTableComponent> entries,
            IList<VTableComponent> wrappedEntries, string vptr, int tableIndex, bool destructorOnly)
        {
            var pointerSize = Context.TargetInfo.PointerWidth / 8;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var offset = pointerSize
                    * (i - (Context.ParserOptions.IsMicrosoftAbi ? 0 : VTables.ItaniumOffsetToTopAndRTTI));

                var nativeVftableEntry = string.Format("*(void**)(native->{0} + {1})", vptr, offset);
                var managedVftableEntry = string.Format("*(void**)(vfptr{0} + {1})", tableIndex, offset);

                if ((entry.Kind == VTableComponentKind.FunctionPointer ||
                     entry.Kind == VTableComponentKind.DeletingDtorPointer) &&
                    !entry.IsIgnored() &&
                    (!destructorOnly || entry.Method.IsDestructor ||
                     Context.Options.ExplicitlyPatchedVirtualFunctions.Contains(entry.Method.QualifiedOriginalName)))
                    WriteLine("{0} = _Thunks[{1}];", managedVftableEntry, wrappedEntries.IndexOf(entry));
                else
                    WriteLine("{0} = {1};", managedVftableEntry, nativeVftableEntry);
            }
        }

        private void GenerateVTableClassSetupCall(Class @class, bool destructorOnly = false)
        {
            if (@class.IsDynamic && GetUniqueVTableMethodEntries(@class).Count > 0)
            {
                if (destructorOnly)
                {
                    WriteLine("SetupVTables(true);");
                    return;
                }
                var typeFullName = TypePrinter.VisitClassDecl(@class);
                if (!string.IsNullOrEmpty(@class.TranslationUnit.Module.OutputNamespace))
                    typeFullName = string.Format("{0}.{1}",
                        @class.TranslationUnit.Module.OutputNamespace, typeFullName);
                WriteLine("SetupVTables(GetType().FullName == \"{0}\");", typeFullName);
            }
        }

        private void GenerateVTableManagedCall(Method method)
        {
            if (method.IsDestructor)
            {
                WriteLine("{0}.Dispose(true);", Helpers.TargetIdentifier);
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

                var ctx = new CSharpMarshalContext(Context)
                {
                    ReturnType = param.QualifiedType,
                    ReturnVarName = param.Name,
                    ParameterIndex = i
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx) { MarshalsParameter = true };
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                marshals.Add(marshal.Context.Return);
            }

            var hasReturn = !method.OriginalReturnType.Type.IsPrimitiveType(PrimitiveType.Void);

            if (hasReturn)
                Write("var {0} = ", Helpers.ReturnIdentifier);

            if (method.IsGenerated)
            {
                WriteLine("{0}.{1}({2});", Helpers.TargetIdentifier,
                    method.Name, string.Join(", ", marshals));              
            }
            else
            {
                InvokeProperty(method, marshals);
            }

            if (hasReturn)
            {
                var param = new Parameter
                {
                    Name = Helpers.ReturnIdentifier,
                    QualifiedType = method.OriginalReturnType
                };

                // Marshal the managed result to native
                var ctx = new CSharpMarshalContext(Context)
                {
                    ArgName = Helpers.ReturnIdentifier,
                    Parameter = param,
                    Function = method,
                    Kind = CSharpMarshalKind.VTableReturnValue
                };

                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                method.OriginalReturnType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (method.HasIndirectReturnTypeParameter)
                {
                    var retParam = method.Parameters.First(p => p.Kind == ParameterKind.IndirectReturnType);
                    TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                    WriteLine("*({0}*) {1} = {2};",
                        method.OriginalReturnType.Visit(TypePrinter), retParam.Name, marshal.Context.Return);
                    TypePrinter.PopContext();
                }
                else
                {
                    WriteLine("return {0};", marshal.Context.Return);                    
                }
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
                WriteLine("{0}.{1} = {2};", Helpers.TargetIdentifier, property.Name,
                    string.Join(", ", marshals));
            }
            else
            {
                WriteLine("{0}.{1};", Helpers.TargetIdentifier, property.Name);
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

            CSharpTypePrinterResult retType;
            var @params = GatherInternalParams(method, out retType);

            var vTableMethodDelegateName = GetVTableMethodDelegateName(method);

            WriteLine("private static {0} {1}Instance;",
                GetDelegateName(method, @class.TranslationUnit.Module.OutputNamespace),
                vTableMethodDelegateName);
            NewLine();

            WriteLine("private static {0} {1}Hook({2})", retType, vTableMethodDelegateName,
                string.Join(", ", @params));
            WriteStartBraceIndent();

            WriteLine("if (!NativeToManagedMap.ContainsKey(instance))");
            WriteLineIndent("throw new global::System.Exception(\"No managed instance was found\");");
            NewLine();

            WriteLine("var {0} = ({1}) NativeToManagedMap[instance];", Helpers.TargetIdentifier, @class.Name);
            WriteLine("if ({0}.{1})", Helpers.TargetIdentifier, Helpers.OwnsNativeInstanceIdentifier);
            WriteLineIndent("{0}.SetupVTables();", Helpers.TargetIdentifier);
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

        #endregion

        #region Events

        private void GenerateEvent(Event @event)
        {
            PushBlock(CSharpBlockKind.Event, @event);
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            var delegateInstance = Generator.GeneratedIdentifier(@event.OriginalName);
            var delegateName = delegateInstance + "Delegate";
            var delegateRaise = delegateInstance + "RaiseInstance";

            WriteLine("[UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]");
            WriteLine("delegate void {0}({1});", delegateName, args);
            WriteLine("{0} {1};", delegateName, delegateRaise);
            NewLine();

            WriteLine("{0} {1};", @event.Type, delegateInstance);
            WriteLine("public event {0} {1}", @event.Type, @event.Name);
            WriteStartBraceIndent();

            GenerateEventAdd(@event, delegateRaise, delegateName, delegateInstance);
            NewLine();

            GenerateEventRemove(@event, delegateInstance);

            WriteCloseBraceIndent();
            NewLine();

            GenerateEventRaiseWrapper(@event, delegateInstance);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateEventAdd(Event @event, string delegateRaise, string delegateName, string delegateInstance)
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

        private void GenerateEventRemove(ITypedDecl @event, string delegateInstance)
        {
            WriteLine("remove");
            WriteStartBraceIndent();

            WriteLine("{0} = ({1})System.Delegate.Remove({0}, value);",
                delegateInstance, @event.Type);

            WriteCloseBraceIndent();
        }

        private void GenerateEventRaiseWrapper(Event @event, string delegateInstance)
        {
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            WriteLine("void _{0}Raise({1})", @event.Name, args);
            WriteStartBraceIndent();

            var returns = new List<string>();
            foreach (var param in @event.Parameters)
            {
                var ctx = new CSharpMarshalContext(Context)
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

                // ensure any virtual dtor in the chain is called
                var dtor = @class.Destructors.FirstOrDefault(d => d.Access != AccessSpecifier.Private);
                var baseDtor = @class.BaseClass == null ? null :
                    @class.BaseClass.Destructors.FirstOrDefault(d => !d.IsVirtual);
                if (ShouldGenerateClassNativeField(@class) ||
                    ((dtor != null && (dtor.IsVirtual || @class.HasNonTrivialDestructor)) && baseDtor != null) ||
                    // virtual destructors in abstract classes may lack a pointer in the v-table
                    // so they have to be called by symbol; thus we need an explicit Dispose override
                    @class.IsAbstract)
                    GenerateDisposeMethods(@class);
            }
        }

        private void GenerateClassFinalizer(INamedDecl @class)
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
                if (Options.GenerateFinalizers)
                    WriteLine("GC.SuppressFinalize(this);");

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate Dispose(bool) method
            PushBlock(CSharpBlockKind.Method);
            Write("public ");
            if (!@class.IsValueType)
                Write(hasBaseClass ? "override " : "virtual ");

            WriteLine("void Dispose(bool disposing)");
            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                var @base = @class.GetNonIgnoredRootBase();

                // Use interfaces if any - derived types with a this class as a seconary base, must be compatible with the map
                var @interface = @base.Namespace.Classes.Find(c => c.OriginalClass == @base);

                // The local var must be of the exact type in the object map because of TryRemove
                WriteLine("{0} {1};",
                    (@interface ?? (@base.IsAbstractImpl ? @base.BaseClass : @base)).Visit(TypePrinter),
                    Helpers.DummyIdentifier);
                WriteLine("NativeToManagedMap.TryRemove({0}, out {1});",
                    Helpers.InstanceIdentifier, Helpers.DummyIdentifier);
                var suffix = Helpers.GetSuffixForInternal(@class);
                if (@class.IsDynamic && GetUniqueVTableMethodEntries(@class).Count != 0)
                {
                    if (Context.ParserOptions.IsMicrosoftAbi)
                        for (var i = 0; i < @class.Layout.VTablePointers.Count; i++)
                            WriteLine("(({0}{1}*) {2})->{3} = new global::System.IntPtr(__OriginalVTables[{4}]);",
                                Helpers.InternalStruct, suffix, Helpers.InstanceIdentifier,
                                @class.Layout.VTablePointers[i].Name, i);
                    else
                        WriteLine("(({0}{1}*) {2})->{3} = new global::System.IntPtr(__OriginalVTables[0]);",
                            Helpers.InternalStruct, suffix, Helpers.InstanceIdentifier,
                            @class.Layout.VTablePointers[0].Name);
                }
            }

            var dtor = @class.Destructors.FirstOrDefault();
            if (dtor != null && dtor.Access != AccessSpecifier.Private &&
                @class.HasNonTrivialDestructor && !dtor.IsPure)
            {
                NativeLibrary library;
                if (!Options.CheckSymbols ||
                    Context.Symbols.FindLibraryBySymbol(dtor.Mangled, out library))
                {
                    WriteLine("if (disposing)");
                    if (dtor.IsVirtual)
                    {
                        WriteStartBraceIndent();
                        GenerateVirtualFunctionCall(dtor, @class, true);
                        if (@class.IsAbstract)
                        {
                            WriteCloseBraceIndent();
                            WriteLine("else");
                            PushIndent();
                            GenerateInternalFunctionCall(dtor);
                            PopIndent();
                        }
                        WriteCloseBraceIndent();
                    }
                    else
                    {
                        PushIndent();
                        GenerateInternalFunctionCall(dtor);
                        PopIndent();
                    }
                }
            }

            WriteLine("if ({0})", Helpers.OwnsNativeInstanceIdentifier);
            WriteLineIndent("Marshal.FreeHGlobal({0});", Helpers.InstanceIdentifier);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNativeConstructor(Class @class)
        {
            var shouldGenerateClassNativeField = ShouldGenerateClassNativeField(@class);
            if (@class.IsRefType && shouldGenerateClassNativeField)
            {
                PushBlock(CSharpBlockKind.Field);
                WriteLine("protected bool {0};", Helpers.OwnsNativeInstanceIdentifier);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            var className = @class.IsAbstractImpl ? @class.BaseClass.Name : @class.Name;

            var ctorCall = string.Format("{0}{1}", @class.Name, @class.IsAbstract ? "Internal" : "");
            if (!@class.IsAbstractImpl)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("public static {0}{1} {2}(global::System.IntPtr native, bool skipVTables = false)",
                    @class.NeedsBase && !@class.BaseClass.IsInterface ? "new " : string.Empty,
                    @class.Name, Helpers.CreateInstanceIdentifier);
                WriteStartBraceIndent();
                WriteLine("return new {0}(native.ToPointer(), skipVTables);", ctorCall);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            GenerateNativeConstructorByValue(@class, className, ctorCall);

            PushBlock(CSharpBlockKind.Method);
            WriteLine("{0} {1}(void* native, bool skipVTables = false){2}",
                @class.IsAbstractImpl ? "internal" : (@class.IsRefType ? "protected" : "private"),
                @class.Name, @class.IsValueType ? " : this()" : string.Empty);

            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;
            if (hasBaseClass)
                WriteLineIndent(": base((void*) null)", @class.BaseClass.Visit(TypePrinter));

            WriteStartBraceIndent();

            if (@class.IsRefType)
            {
                if (@class.HasBaseClass)
                    WriteLine("{0} = {1};", Helpers.PointerAdjustmentIdentifier,
                        GetOffsetToBase(@class, @class.BaseClass));
                if (!@class.IsAbstractImpl)
                {
                    WriteLine("if (native == null)");
                    WriteLineIndent("return;");
                }

                WriteLine("{0} = new global::System.IntPtr(native);", Helpers.InstanceIdentifier);
                var dtor = @class.Destructors.FirstOrDefault();
                var hasVTables = @class.IsDynamic && GetUniqueVTableMethodEntries(@class).Count > 0;
                var setupVTables = !@class.IsAbstractImpl && hasVTables && dtor != null && dtor.IsVirtual;
                if (setupVTables)
                {
                    WriteLine("if (skipVTables)");
                    PushIndent();
                }

                if (@class.IsAbstractImpl || hasVTables)
                    SaveOriginalVTablePointers(@class);

                if (setupVTables)
                {
                    PopIndent();
                    WriteLine("else");
                    PushIndent();
                    GenerateVTableClassSetupCall(@class, destructorOnly: true);
                    PopIndent();
                }
            }
            else
            {
                WriteLine("{0} = *({1}{2}*) native;", Helpers.InstanceField,
                    Helpers.InternalStruct, Helpers.GetSuffixForInternal(@class));
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNativeConstructorByValue(Class @class, string className, string ctorCall)
        {
            var internalSuffix = Helpers.GetSuffixForInternal(@class);

            if (!@class.IsAbstractImpl)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("public static {0} {1}({0}.{2}{3} native, bool skipVTables = false)",
                    className, Helpers.CreateInstanceIdentifier,
                    Helpers.InternalStruct, internalSuffix);
                WriteStartBraceIndent();
                WriteLine("return new {0}(native, skipVTables);", ctorCall);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);   
            }

            if (@class.IsRefType && !@class.IsAbstract)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("private static void* __CopyValue({0}.{1}{2} native)",
                    className, Helpers.InternalStruct, internalSuffix);
                WriteStartBraceIndent();
                var copyCtorMethod = @class.Methods.FirstOrDefault(method =>
                    method.IsCopyConstructor);
                if (@class.HasNonTrivialCopyConstructor && copyCtorMethod != null &&
                    copyCtorMethod.IsGenerated)
                {
                    // Allocate memory for a new native object and call the ctor.
                    WriteLine("var ret = Marshal.AllocHGlobal({0});", @class.Layout.Size);
                    WriteLine("{0}.{1}{2}.{3}(ret, new global::System.IntPtr(&native));",
                        @class.Visit(TypePrinter), Helpers.InternalStruct,
                        Helpers.GetSuffixForInternal(@class),
                        GetFunctionNativeIdentifier(copyCtorMethod));
                    WriteLine("return ret.ToPointer();", className);
                }
                else
                {
                    WriteLine("var ret = Marshal.AllocHGlobal({1});",
                        className, @class.Layout.Size);
                    WriteLine("*({0}.{1}{2}*) ret = native;", className,
                        Helpers.InternalStruct, Helpers.GetSuffixForInternal(@class));
                    WriteLine("return ret.ToPointer();");
                }
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            if (!@class.IsAbstract)
            {
                PushBlock(CSharpBlockKind.Method);
                WriteLine("{0} {1}({2}.{3}{4} native, bool skipVTables = false)",
                    @class.IsAbstractImpl ? "internal" : "private",
                    @class.Name, className, Helpers.InternalStruct, internalSuffix);
                WriteLineIndent(@class.IsRefType ? ": this(__CopyValue(native), skipVTables)" : ": this()");
                WriteStartBraceIndent();
                if (@class.IsRefType)
                {
                    WriteLine("{0} = true;", Helpers.OwnsNativeInstanceIdentifier);
                    WriteLine("NativeToManagedMap[{0}] = this;", Helpers.InstanceIdentifier);
                }
                else
                {
                    WriteLine("{0} = native;", Helpers.InstanceField);
                }
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private void GenerateClassConstructorBase(Class @class, Method method)
        {
            var hasBase = @class.HasBaseClass;

            if (hasBase && !@class.IsValueType)
            {
                PushIndent();
                Write(": this(");

                Write(method != null ? "(void*) null" : "native");

                WriteLine(")");
                PopIndent();
            }

            if (@class.IsValueType)
                WriteLineIndent(": this()");
        }

        #endregion

        #region Methods / Functions

        public void GenerateFunction(Function function, string parentName)
        {
            PushBlock(CSharpBlockKind.Function);
            GenerateDeclarationCommon(function);

            var functionName = GetFunctionIdentifier(function);
            if (functionName == parentName)
                functionName += '_';
            Write("public static {0} {1}(", function.OriginalReturnType, functionName);
            Write(FormatMethodParameters(function.Parameters));
            WriteLine(")");
            WriteStartBraceIndent();

            if (function.SynthKind == FunctionSynthKind.DefaultValueOverload)
                GenerateOverloadCall(function);
            else
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

            // check if overriding a function from a secondary base
            Method rootBaseMethod;
            var isOverride = method.IsOverride &&
                (rootBaseMethod = @class.GetBaseMethod(method, true)) != null &&
                rootBaseMethod.IsGenerated && rootBaseMethod.IsVirtual;

            if (method.IsVirtual && !isOverride && !method.IsOperator && !method.IsPure)
                Write("virtual ");

            var isBuiltinOperator = method.IsOperator &&
                Operators.IsBuiltinOperator(method.OperatorKind);

            if (method.IsStatic || isBuiltinOperator)
                Write("static ");

            if (isOverride)
            {
                Write("override ");
            }

            if (method.IsPure)
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

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload && method.IsConstructor && !method.IsPure)
            {
                Write(" : this({0})",
                    string.Join(", ",
                        method.Parameters.Where(
                            p => p.Kind == ParameterKind.Regular).Select(
                                p => p.Ignore ? ExpressionPrinter.VisitExpression(p.DefaultArgument) : p.Name)));
            }

            if (method.IsPure)
            {
                Write(";");
                PopBlock(NewLineKind.BeforeNextBlock);
                return;
            }
            NewLine();

            if (method.Kind == CXXMethodKind.Constructor &&
                method.SynthKind != FunctionSynthKind.DefaultValueOverload)
                GenerateClassConstructorBase(@class, method);

            WriteStartBraceIndent();

            if (method.IsProxy)
                goto SkipImpl;

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload)
            {
                if (!method.IsConstructor)
                {
                    GenerateOverloadCall(method);
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
                    GenerateOperator(method);
                }
                else if (method.SynthKind == FunctionSynthKind.AbstractImplCall)
                {
                    GenerateVirtualFunctionCall(method, @class.BaseClass);
                }
                else if (method.IsVirtual)
                {
                    GenerateVirtualFunctionCall(method, @class);
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
                    GenerateOperator(method);
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
                GenerateEqualsAndGetHashCode(method, @class);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private string OverloadParamNameWithDefValue(Parameter p, ref int index)
        {
            Class @class;
            return p.Type.IsPointerToPrimitiveType() && p.Usage == ParameterUsage.InOut && p.HasDefaultValue
                ? "ref param" + index++
                : (( p.Type.TryGetClass(out @class) && @class.IsInterface) ? "param" + index++ 
                     : ExpressionPrinter.VisitExpression(p.DefaultArgument));
        }

        private void GenerateOverloadCall(Function function)
        {
            for (int i = 0, j = 0; i < function.Parameters.Count; i++)
            {
                var parameter = function.Parameters[i];
                PrimitiveType primitiveType;
                if (parameter.Kind == ParameterKind.Regular && parameter.Ignore &&
                    parameter.Type.IsPointerToPrimitiveType(out primitiveType) &&
                    parameter.Usage == ParameterUsage.InOut && parameter.HasDefaultValue)
                {
                    var pointeeType = ((PointerType) parameter.Type).Pointee.ToString();
                    WriteLine("{0} param{1} = {2};", pointeeType, j++,
                        primitiveType == PrimitiveType.Bool ? "false" : "0");
                }
                Class @class;
                if (parameter.Kind == ParameterKind.Regular && parameter.Ignore &&
                    parameter.Type.Desugar().TryGetClass(out @class) && @class.IsInterface &&
                    parameter.HasDefaultValue)
                {
                    WriteLine("var param{0} = ({1}) {2};",  j++, @class.OriginalClass.OriginalName,
                        ExpressionPrinter.VisitExpression(parameter.DefaultArgument));
                }
            }

            GenerateManagedCall(function, prependThis: function.Parameters.Any(p => !p.Ignore && p.Name == function.Name));
        }

        private void GenerateManagedCall(Function function, bool prependBase = false, bool prependThis = false)
        {
            var type = function.OriginalReturnType.Type;
            var index = 0;
            WriteLine("{0}{1}{2}({3});",
                type.IsPrimitiveType(PrimitiveType.Void) ? string.Empty : "return ",
                prependBase ? "base." : prependThis ? "this." : string.Empty,
                function.Name,
                string.Join(", ",
                    function.Parameters.Where(
                        p => p.Kind != ParameterKind.IndirectReturnType).Select(
                            p => p.Ignore ? OverloadParamNameWithDefValue(p, ref index) :
                                (p.Usage == ParameterUsage.InOut ? "ref " : string.Empty) + p.Name)));
        }

        private void GenerateEqualsAndGetHashCode(Function method, Class @class)
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

                NewLine();

                WriteLine("public override int GetHashCode()");
                WriteStartBraceIndent();
                if (@class.IsRefType)
                {
                    WriteLine("if ({0} == global::System.IntPtr.Zero)", Helpers.InstanceIdentifier);
                    WriteLineIndent("return global::System.IntPtr.Zero.GetHashCode();");
                    WriteLine("return (*({0}{1}*) {2}).GetHashCode();", Helpers.InternalStruct,
                        Helpers.GetSuffixForInternal(@class), Helpers.InstanceIdentifier);   
                }
                else
                {
                    WriteLine("return {0}.GetHashCode();", Helpers.InstanceIdentifier);                       
                }
                WriteCloseBraceIndent();
            }
        }

        private static AccessSpecifier GetValidMethodAccess(Method method)
        {
            if (!method.IsOverride)
                return method.Access;
            var baseMethod = ((Class) method.Namespace).GetBaseMethod(method);
            return baseMethod.IsGenerated ? baseMethod.Access : method.Access;
        }

        private static AccessSpecifier GetValidPropertyAccess(Property property)
        {
            if (property.Access == AccessSpecifier.Public)
                return AccessSpecifier.Public;
            if (!property.IsOverride)
                return property.Access;
            var baseProperty = ((Class) property.Namespace).GetBaseProperty(property);
            // access can be changed from private to other while overriding in C++
            return baseProperty != null ? baseProperty.Access : property.Access;
        }

        private void GenerateVirtualPropertyCall(Method method, Class @class,
            Property property, List<Parameter> parameters = null)
        {
            if (property.IsOverride && !property.IsPure &&
                method.SynthKind != FunctionSynthKind.AbstractImplCall &&
                @class.HasNonAbstractBasePropertyInPrimaryBase(property))
            {
                WriteLine(parameters == null ?
                    "return base.{0};" : "base.{0} = value;", property.Name);
            }
            else
            {
                string delegateId;
                GetVirtualCallDelegate(method, @class, out delegateId);
                GenerateFunctionCall(delegateId, parameters ?? method.Parameters, method);
            }
        }

        private void GenerateVirtualFunctionCall(Method method, Class @class,
            bool forceVirtualCall = false)
        {
            if (!forceVirtualCall && method.IsOverride && !method.IsPure &&
                method.SynthKind != FunctionSynthKind.AbstractImplCall &&
                @class.HasCallableBaseMethodInPrimaryBase(method))
            {
                GenerateManagedCall(method, true);
            }
            else
            {
                string delegateId;
                GetVirtualCallDelegate(method, @class, out delegateId);
                GenerateFunctionCall(delegateId, method.Parameters, method);
            }
        }

        private void GetVirtualCallDelegate(Method method, Class @class,
            out string delegateId)
        {
            var i = VTables.GetVTableIndex(method.OriginalFunction ?? method, @class);
            int vtableIndex = 0;
            if (Context.ParserOptions.IsMicrosoftAbi)
                vtableIndex = @class.Layout.VFTables.IndexOf(@class.Layout.VFTables.Where(
                    v => v.Layout.Components.Any(c => c.Method.OriginalPtr == method.OriginalPtr)).First());
            WriteLine("var {0} = *(void**) ((IntPtr) __OriginalVTables[{1}] + {2} * {3});",
                Helpers.SlotIdentifier, vtableIndex, i, Context.TargetInfo.PointerWidth / 8);
            if (method.IsDestructor && @class.IsAbstract)
            {
                WriteLine("if ({0} != null)", Helpers.SlotIdentifier);
                WriteStartBraceIndent();
            }

            var @delegate = GetVTableMethodDelegateName(method.OriginalFunction ?? method);
            delegateId = Generator.GeneratedIdentifier(@delegate);

            WriteLine("var {0} = ({1}) Marshal.GetDelegateForFunctionPointer(new IntPtr({2}), typeof({1}));",
                delegateId, GetDelegateName(method, method.TranslationUnit.Module.OutputNamespace),
                Helpers.SlotIdentifier);
        }

        private string GetDelegateName(Function function, string outputNamespace)
        {
            var @delegate = Context.Delegates[function];
            if (string.IsNullOrWhiteSpace(@delegate.Namespace) ||
                outputNamespace == @delegate.Namespace)
            {
                return @delegate.Signature;
            }
            return string.Format("global::{0}.{1}", @delegate.Namespace, @delegate.Signature);
        }

        private void GenerateOperator(Method method)
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

                    var paramName = string.Format("{0}{1}",
                        method.Parameters[0].Type.IsPrimitiveTypeConvertibleToRef() ?
                        "ref *" : string.Empty,
                        method.Parameters[0].Name);
                    if (@interface != null)
                        WriteLine("return new {0}(({2}) {1});",
                            method.ConversionType, paramName, @interface.Name);
                    else
                        WriteLine("return new {0}({1});", method.ConversionType, paramName);
                }
                else
                {
                    var @operator = Operators.GetOperatorOverloadPair(method.OperatorKind);

                    WriteLine("return !({0} {1} {2});", method.Parameters[0].Name,
                              @operator, method.Parameters[1].Name);
                }
                return;
            }

            if (method.OperatorKind == CXXOperatorKind.EqualEqual ||
                method.OperatorKind == CXXOperatorKind.ExclaimEqual)
            {
                WriteLine("bool {0}Null = ReferenceEquals({0}, null);",
                    method.Parameters[0].Name);
                WriteLine("bool {0}Null = ReferenceEquals({0}, null);",
                    method.Parameters[1].Name);
                WriteLine("if ({0}Null || {1}Null)",
                    method.Parameters[0].Name, method.Parameters[1].Name);
                WriteLineIndent("return {0}{1}Null && {2}Null{3};",
                    method.OperatorKind == CXXOperatorKind.EqualEqual ? string.Empty : "!(",
                    method.Parameters[0].Name, method.Parameters[1].Name,
                    method.OperatorKind == CXXOperatorKind.EqualEqual ? string.Empty : ")");
            }

            GenerateInternalFunctionCall(method);
        }

        private void GenerateClassConstructor(Method method, Class @class)
        {
            WriteLine("{0} = Marshal.AllocHGlobal({1});", Helpers.InstanceIdentifier,
                @class.Layout.Size);
            WriteLine("{0} = true;", Helpers.OwnsNativeInstanceIdentifier);
            WriteLine("NativeToManagedMap[{0}] = this;", Helpers.InstanceIdentifier);

            if (method.IsCopyConstructor)
            {
                if (@class.HasNonTrivialCopyConstructor)
                    GenerateInternalFunctionCall(method);
                else
                    WriteLine("*(({0}.{1}{2}*) {3}) = *(({0}.{1}{2}*) {4}.{3});",
                        @class.Name, Helpers.InternalStruct,
                        Helpers.GetSuffixForInternal(@class), Helpers.InstanceIdentifier,
                        method.Parameters[0].Name);
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

            var templateSpecialization = function.Namespace as ClassTemplateSpecialization;

            string @namespace = templateSpecialization != null &&
                templateSpecialization.Ignore ?
                (templateSpecialization.Namespace.OriginalName + '.') : string.Empty;

            var functionName = string.Format("{0}{1}{2}.{3}", @namespace,
                Helpers.InternalStruct, Helpers.GetSuffixForInternal(function.Namespace),
                GetFunctionNativeIdentifier(function.OriginalFunction ?? function));
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
                if (Context.TypeMaps.FindTypeMap(retClass, out typeMap))
                    construct = typeMap.CSharpConstruct();

                if (construct == null)
                {
                    var @class = retClass.OriginalClass ?? retClass;
                    var specialization = @class as ClassTemplateSpecialization;
                    WriteLine("var {0} = new {1}.{2}{3}();", Helpers.ReturnIdentifier,
                        @class.Visit(TypePrinter), Helpers.InternalStruct,
                        Helpers.GetSuffixForInternal(@class));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(construct))
                        WriteLine("{0} {1};",
                            typeMap.CSharpSignature(new CSharpTypePrinterContext
                            {
                                Type = indirectRetType.Type.Desugar()
                            }),
                            Helpers.ReturnIdentifier);
                    else
                        WriteLine("var {0} = {1};", construct);
                }
            }

            var names = new List<string>();
            foreach (var param in @params)
            {
                if (param.Param == operatorParam && needsInstance)
                    continue;

                var name = new StringBuilder();
                if (param.Context != null
                    && !string.IsNullOrWhiteSpace(param.Context.ArgumentPrefix))
                    name.Append(param.Context.ArgumentPrefix);

                name.Append(param.Name);
                names.Add(name.ToString());
            }

            var needsFixedThis = needsInstance && isValueType;

            if (originalFunction.HasIndirectReturnTypeParameter)
            {
                var name = string.Format("new IntPtr(&{0})", Helpers.ReturnIdentifier);
                names.Insert(0, name);
            }

            if (needsInstance)
            {
                var instanceIndex = GetInstanceParamIndex(method);

                if (needsFixedThis)
                {
                    names.Insert(instanceIndex, "new global::System.IntPtr(__instancePtr)");
                }
                else
                {
                    names.Insert(instanceIndex,
                        operatorParam != null ? @params[0].Name : GetInstanceParam(function));
                }
            }

            if (needsFixedThis)
            {
                if (operatorParam == null)
                {
                    WriteLine("fixed ({0}{1}* __instancePtr = &{2})",
                        Helpers.InternalStruct, Helpers.GetSuffixForInternal(originalFunction.Namespace),
                        Helpers.InstanceField);
                    WriteStartBraceIndent();
                }
                else
                {
                    WriteLine("var __instancePtr = &{0}.{1};", operatorParam.Name, Helpers.InstanceField);
                }
            }

            if (needsReturn && !originalFunction.HasIndirectReturnTypeParameter)
                Write("var {0} = ", Helpers.ReturnIdentifier);

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
                var ctx = new CSharpMarshalContext(Context)
                {
                    ArgName = Helpers.ReturnIdentifier,
                    ReturnVarName = Helpers.ReturnIdentifier,
                    ReturnType = retType,
                    Parameter = operatorParam
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                retType.CSharpMarshalToManaged(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                if (ctx.HasCodeBlock)
                    PushIndent();

                // Special case for indexer - needs to dereference if the internal
                // function is a pointer type and the property is not.
                if (retType.Type.IsAddress() &&
                    retType.Type.GetPointee().Equals(returnType) &&
                    returnType.IsPrimitiveType())
                    WriteLine("return *{0};", marshal.Context.Return);
                else
                    WriteLine("return {0};", marshal.Context.Return);

                if (ctx.HasCodeBlock)
                    WriteCloseBraceIndent();
            }

            if (needsFixedThis && operatorParam == null)
                WriteCloseBraceIndent();
            
            var numFixedBlocks = @params.Count(param => param.HasUsingBlock);
            for(var i = 0; i < numFixedBlocks; ++i)
                WriteCloseBraceIndent();
        }

        private static string GetInstanceParam(Function function)
        {
            var from = (Class) function.Namespace;
            var to = function.OriginalFunction == null ? @from.BaseClass :
                (Class) function.OriginalFunction.Namespace;

            var baseOffset = 0u;
            if (to != null)
            {
                to = to.OriginalClass ?? to;
                baseOffset = GetOffsetToBase(from, to);
            }
            var isPrimaryBase = from.BaseClass == to;
            if (isPrimaryBase)
            {
                return string.Format("({0} + {1}{2})",
                    Helpers.InstanceIdentifier,
                    Helpers.PointerAdjustmentIdentifier,
                    baseOffset == 0 ? string.Empty : (" - " + baseOffset));
            }
            return string.Format("({0}{1})",
                Helpers.InstanceIdentifier,
                baseOffset == 0 ? string.Empty : " + " + baseOffset);
        }

        private static uint GetOffsetToBase(Class from, Class to)
        {
            return from.Layout.Bases.Where(
                b => b.Class == to).Select(b => b.Offset).FirstOrDefault();
        }

        private int GetInstanceParamIndex(Function method)
        {
            if (Context.ParserOptions.IsMicrosoftAbi)
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
                if (param.Type.IsPrimitiveTypeConvertibleToRef())
                    continue;

                var nativeVarName = paramInfo.Name;

                var ctx = new CSharpMarshalContext(Context)
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

                if (!string.IsNullOrWhiteSpace(marshal.Context.Cleanup))
                    cleanups.Add(marshal.Context.Cleanup);
            }
        }

        public struct ParamMarshal
        {
            public string Name;
            public Parameter Param;
            public CSharpMarshalContext Context;
            public bool HasUsingBlock;
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
            PrimitiveType primitive;
            // Do not delete instance in MS ABI.
            var name = param.Kind == ParameterKind.ImplicitDestructorParameter ? "0" : param.Name;
            if (param.Type.IsPrimitiveType(out primitive) && primitive != PrimitiveType.Char)
            {
                return new ParamMarshal { Name = name, Param = param };
            }

            var argName = Generator.GeneratedIdentifier("arg") + paramIndex.ToString(CultureInfo.InvariantCulture);
            var paramMarshal = new ParamMarshal { Name = argName, Param = param };

            if (param.IsOut || param.IsInOut)
            {
                var paramType = param.Type;

                Class @class;
                if ((paramType.GetFinalPointee() ?? paramType).Desugar().TryGetClass(out @class))
                {
                    var qualifiedIdentifier = (@class.OriginalClass ?? @class).Visit(TypePrinter);
                    WriteLine("{0} = new {1}();", name, qualifiedIdentifier);
                }
            }

            var ctx = new CSharpMarshalContext(Context)
            {
                Parameter = param,
                ParameterIndex = paramIndex,
                ArgName = argName,
                Function = function
            };

            paramMarshal.Context = ctx;
            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            param.CSharpMarshalToNative(marshal);
            paramMarshal.HasUsingBlock = ctx.HasCodeBlock;

            if (string.IsNullOrEmpty(marshal.Context.Return))
                throw new Exception("Cannot marshal argument of function");

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Write(marshal.Context.SupportBefore);

            if (paramMarshal.HasUsingBlock)
                PushIndent();

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
                            string.Empty : " = " + ExpressionPrinter.VisitExpression(param.DefaultArgument))));
        }

        #endregion

        public bool GenerateTypedef(TypedefNameDecl typedef)
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
                if (interopCallConv == System.Runtime.InteropServices.CallingConvention.Winapi)
                    WriteLine("[SuppressUnmanagedCodeSecurity]");
                else
                    WriteLine(
                        "[SuppressUnmanagedCodeSecurity, " +
                        "UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.{0})]",
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
            PushBlock(CSharpBlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            if (@enum.IsFlags)
                WriteLine("[Flags]");

            Write(Helpers.GetAccess(@enum.Access)); 
            // internal P/Invoke declarations must see protected enums
            if (@enum.Access == AccessSpecifier.Protected)
                Write("internal ");
            Write("enum {0}", Helpers.SafeIdentifier(@enum.Name));

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
            Write("[DllImport(\"{0}\", ", GetLibraryOf(function));

            var callConv = function.CallingConvention.ToInteropCallConv();
            WriteLine("CallingConvention = global::System.Runtime.InteropServices.CallingConvention.{0},",
                callConv);

            WriteLineIndent("EntryPoint=\"{0}\")]", function.Mangled);

            if (function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Bool))
                WriteLine("[return: MarshalAsAttribute(UnmanagedType.I1)]");

            CSharpTypePrinterResult retType;
            var @params = GatherInternalParams(function, out retType);

            WriteLine("internal static extern {0} {1}({2});", retType,
                      GetFunctionNativeIdentifier(function),
                      string.Join(", ", @params));
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private string GetLibraryOf(Declaration declaration)
        {
            if (declaration.TranslationUnit.IsSystemHeader)
                return Context.Options.SystemModule.TemplatesLibraryName;

            string libName = declaration.TranslationUnit.Module.SharedLibraryName;

            if (Options.CheckSymbols)
            {
                NativeLibrary library;
                Context.Symbols.FindLibraryBySymbol(((IMangledDecl) declaration).Mangled, out library);

                if (library != null)
                    libName = Path.GetFileNameWithoutExtension(library.FileName);
            }
            if (Options.StripLibPrefix && libName != null && libName.Length > 3 &&
                libName.StartsWith("lib", StringComparison.Ordinal))
            {
                libName = libName.Substring(3);
            }
            if (libName == null)
                libName = declaration.TranslationUnit.Module.SharedLibraryName;

            if (Options.GenerateInternalImports)
                libName = "__Internal";

            if (Platform.IsMacOS)
            {
                var framework = libName + ".framework";
                for (uint i = 0; i < Context.ParserOptions.LibraryDirsCount; i++)
                {
                    var libDir = Context.ParserOptions.getLibraryDirs(i);
                    if (Path.GetFileName(libDir) == framework && File.Exists(Path.Combine(libDir, libName)))
                    {
                        libName = string.Format("@executable_path/../Frameworks/{0}/{1}", framework, libName);
                        break;
                    }
                }
            }

            return libName;
        }
    }

    internal class SymbolNotFoundException : Exception
    {
        public SymbolNotFoundException(string msg) : base(msg)
        {}
    }
}