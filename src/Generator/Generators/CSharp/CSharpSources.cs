using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Parser;
using CppSharp.Types;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public class CSharpSources : CodeGenerator
    {
        public CSharpTypePrinter TypePrinter { get; set; }
        public CSharpExpressionPrinter ExpressionPrinter { get; protected set; }

        public override string FileExtension => "cs";

        public CSharpSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            TypePrinter = new CSharpTypePrinter(context);
            ExpressionPrinter = new CSharpExpressionPrinter(TypePrinter);
        }

        #region Identifiers

        // Extracted from:
        // https://github.com/mono/mono/blob/master/mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
        static readonly string[] ReservedKeywords =
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

        public override string SafeIdentifier(string id)
        {
            if (id.All(char.IsLetterOrDigit))
                return ReservedKeywords.Contains(id) ? "@" + id : id;

            return new string(id.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }

        #endregion

        public Module Module => TranslationUnits.Count == 0 ?
                Context.Options.SystemModule : TranslationUnit.Module;

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            GenerateUsings();

            if (!string.IsNullOrEmpty(Module.OutputNamespace))
            {
                PushBlock(BlockKind.Namespace);
                WriteLine("namespace {0}", Module.OutputNamespace);
                WriteStartBraceIndent();
            }

            foreach (var unit in TranslationUnits)
                unit.Visit(this);

            if (!string.IsNullOrEmpty(Module.OutputNamespace))
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateUsings()
        {
            PushBlock(BlockKind.Usings);
            WriteLine("using System;");
            WriteLine("using System.Runtime.InteropServices;");
            WriteLine("using System.Security;");

            var internalsVisibleTo = (from m in Options.Modules
                                      where m.Dependencies.Contains(Module)
                                      select m.LibraryName).ToList();
            if (internalsVisibleTo.Any())
                WriteLine("using System.Runtime.CompilerServices;");

            foreach (var customUsingStatement in Options.DependentNameSpaces)
                WriteLine("using {0};", customUsingStatement);
            PopBlock(NewLineKind.BeforeNextBlock);

            foreach (var library in internalsVisibleTo)
                WriteLine($"[assembly:InternalsVisibleTo(\"{library}\")]");

            if (internalsVisibleTo.Any())
                NewLine();
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            var context = @namespace;

            var isNamespace = context is Namespace;
            var isTranslationUnit = context is TranslationUnit;

            var shouldGenerateNamespace = isNamespace && !isTranslationUnit &&
                context.Declarations.Any(d => d.IsGenerated || (d is Class && !d.IsIncomplete));

            if (shouldGenerateNamespace)
            {
                PushBlock(BlockKind.Namespace);
                WriteLine("namespace {0}", context.Name);
                WriteStartBraceIndent();
            }

            var ret = base.VisitNamespace(@namespace);

            if (shouldGenerateNamespace)
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            return ret;
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var @enum in context.Enums)
                @enum.Visit(this);

            foreach (var typedef in context.Typedefs)
                typedef.Visit(this);

            foreach (var @class in context.Classes.Where(c => !(c is ClassTemplateSpecialization)))
                @class.Visit(this);

            foreach (var @event in context.Events)
                @event.Visit(this);

            GenerateNamespaceFunctionsAndVariables(context);

            foreach(var childNamespace in context.Namespaces)
                childNamespace.Visit(this);

            return true;
        }

        void GenerateNamespaceFunctionsAndVariables(DeclarationContext context)
        {
            var hasGlobalVariables = !(context is Class) && context.Variables.Any(
                v => v.IsGenerated && v.Access == AccessSpecifier.Public);

            if (!context.Functions.Any(f => f.IsGenerated) && !hasGlobalVariables)
                return;

            PushBlock(BlockKind.Functions);
            var parentName = SafeIdentifier(context.TranslationUnit.FileNameWithoutExtension);
            WriteLine("public unsafe partial class {0}", parentName);
            WriteStartBraceIndent();

            PushBlock(BlockKind.InternalsClass);
            GenerateClassInternalHead();
            WriteStartBraceIndent();

            // Generate all the internal function declarations.
            foreach (var function in context.Functions)
            {
                if ((!function.IsGenerated && !function.IsInternal) || function.IsSynthetized)
                    continue;

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

        private void GenerateClassTemplateSpecializationInternal(Class classTemplate)
        {
            if (classTemplate.Specializations.Count == 0)
                return;

            var specializations = GetSpecializationsToGenerate(classTemplate);

            bool generateClass = specializations.Any(s => s.IsGenerated);
            if (!generateClass)
            {
                PushBlock(BlockKind.Namespace);
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
                    specialization.Visit(this);
            }

            foreach (var nestedClass in classTemplate.Classes)
            {
                NewLine();
                GenerateClassSpecifier(nestedClass);
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

        private static List<ClassTemplateSpecialization> GetSpecializationsToGenerate(Class classTemplate)
        {
            if (classTemplate.Fields.Any(
                f => f.Type.Desugar() is TemplateParameterType))
                return classTemplate.Specializations;

            var specializations = new List<ClassTemplateSpecialization>();
            var specialization = classTemplate.Specializations.FirstOrDefault(s => !s.Ignore);
            if (specialization == null)
                specializations.Add(classTemplate.Specializations[0]);
            else
                specializations.Add(specialization);
            return specializations;
        }

        public override void GenerateDeclarationCommon(Declaration decl)
        {
            base.GenerateDeclarationCommon(decl);

            foreach (Attribute attribute in decl.Attributes)
                WriteLine("[{0}({1})]", attribute.Type.FullName, attribute.Value);
        }

        #region Classes

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete && !@class.IsOpaque)
                return false;

            if (@class.IsInterface)
            {
                GenerateInterface(@class);
                return true;
            }

            foreach (var nestedTemplate in @class.Classes.Where(c => !c.IsIncomplete && c.IsDependent))
                GenerateClassTemplateSpecializationInternal(nestedTemplate);

            if (@class.IsDependent)
            {
                if (!(@class.Namespace is Class))
                    GenerateClassTemplateSpecializationInternal(@class);

                return true;
            }

            System.Type typeMap = null;
            string key = string.Empty;
            var cppTypePrinter = new CppTypePrinter { PrintScopeKind = TypePrintScopeKind.Qualified };
            foreach (var name in new[] { @class.OriginalName, @class.Visit(cppTypePrinter) })
            {
                if (Context.TypeMaps.TypeMaps.ContainsKey(name))
                {
                    key = name;
                    typeMap = Context.TypeMaps.TypeMaps[key];
                    // disable the type map for the mapped class itself so that operator params are not mapped
                    Context.TypeMaps.TypeMaps.Remove(key);
                    break;
                }
            }

            PushBlock(BlockKind.Class);
            GenerateDeclarationCommon(@class);
            GenerateClassSpecifier(@class);

            NewLine();
            WriteStartBraceIndent();

            if (!@class.IsAbstractImpl)
                GenerateClassInternals(@class);

            VisitDeclContext(@class);

            if (@class.IsDependent || !@class.IsGenerated)
                goto exit;

            if (ShouldGenerateClassNativeField(@class))
            {
                PushBlock(BlockKind.Field);
                if (@class.IsValueType)
                {
                    WriteLine("private {0}.{1} {2};", @class.Name, Helpers.InternalStruct,
                        Helpers.InstanceField);
                    WriteLine("internal {0}.{1} {2} {{ get {{ return {3}; }} }}", @class.Name,
                        Helpers.InternalStruct, Helpers.InstanceIdentifier, Helpers.InstanceField);
                }
                else
                {
                    WriteLine("public {0} {1} {{ get; protected set; }}",
                        CSharpTypePrinter.IntPtrType, Helpers.InstanceIdentifier);

                    PopBlock(NewLineKind.BeforeNextBlock);

                    PushBlock(BlockKind.Field);

                    WriteLine("protected int {0};", Helpers.PointerAdjustmentIdentifier);

                    // use interfaces if any - derived types with a secondary base this class must be compatible with the map
                    var @interface = @class.Namespace.Classes.Find(c => c.OriginalClass == @class);
                    var dict = string.Format("global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, {0}>",
                        (@interface ?? @class).Visit(TypePrinter));
                    WriteLine("internal static readonly {0} NativeToManagedMap = new {0}();", dict);
                    WriteLine("protected void*[] __OriginalVTables;");
                }
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            GenerateClassConstructors(@class);

            GenerateClassMethods(@class.Methods);
            GenerateClassVariables(@class);
            GenerateClassProperties(@class);

            if (@class.IsDynamic)
                GenerateVTable(@class);
        exit:
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            if (typeMap != null)
                Context.TypeMaps.TypeMaps.Add(key, typeMap);

            return true;
        }

        private void GenerateInterface(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete)
                return;

            PushBlock(BlockKind.Interface);
            GenerateDeclarationCommon(@class);

            GenerateClassSpecifier(@class);

            NewLine();
            WriteStartBraceIndent();

            foreach (var method in @class.Methods.Where(m =>
                !ASTUtils.CheckIgnoreMethod(m) && m.Access == AccessSpecifier.Public))
            {
                PushBlock(BlockKind.Method);
                GenerateDeclarationCommon(method);

                var functionName = GetMethodIdentifier(method);

                Write("{0} {1}(", method.OriginalReturnType, functionName);

                Write(FormatMethodParameters(method.Parameters));

                WriteLine(");");

                PopBlock(NewLineKind.BeforeNextBlock);
            }
            foreach (var prop in @class.Properties.Where(p => p.IsGenerated && p.Access == AccessSpecifier.Public))
            {
                PushBlock(BlockKind.Property);
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
            PushBlock(BlockKind.InternalsClass);
            if (!Options.GenerateSequentialLayout || @class.IsUnion)
                WriteLine($"[StructLayout(LayoutKind.Explicit, Size = {@class.Layout.Size})]");
            // no else because the layout is sequential for structures by default

            GenerateClassInternalHead(@class);
            WriteStartBraceIndent();

            TypePrinter.PushContext(TypePrinterContextKind.Native);

            foreach (var field in @class.Layout.Fields)
                GenerateClassInternalsField(field, @class);
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

            var currentSpecialization = @class as ClassTemplateSpecialization;
            Class template;
            if (currentSpecialization != null &&
                GetSpecializationsToGenerate(
                    template = currentSpecialization.TemplatedDecl.TemplatedClass).Count == 1)
                foreach (var specialization in template.Specializations.Where(s => !s.Ignore))
                    GatherClassInternalFunctions(specialization, includeCtors, functions);
            else
                GatherClassInternalFunctions(@class, includeCtors, functions);

            return functions;
        }

        private void GatherClassInternalFunctions(Class @class, bool includeCtors,
            List<Function> functions)
        {
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

                if (prop.SetMethod != null && prop.SetMethod != prop.GetMethod)
                    tryAddOverload(prop.SetMethod);
            }
        }

        private IEnumerable<string> GatherInternalParams(Function function, out TypePrinterResult retType)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);

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

            WriteLine($@"{(isSpecialization ? "unsafe " : string.Empty)}partial struct {
                Helpers.InternalStruct}{Helpers.GetSuffixForInternal(@class)}");
        }

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;
            return @class.IsValueType || !@class.HasBase || !@class.HasRefBase();
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            // private classes must be visible to because the internal structs can be used in dependencies
            // the proper fix is InternalsVisibleTo
            var keywords = new List<string>();

            keywords.Add(@class.Access == AccessSpecifier.Protected ? "protected internal" : "public");
            keywords.Add("unsafe");

            if (@class.IsAbstract)
                keywords.Add("abstract");

            if (@class.IsStatic)
                keywords.Add("static");

            // This token needs to directly precede the "class" token.
            keywords.Add("partial");

            keywords.Add(@class.IsInterface ? "interface" : (@class.IsValueType ? "struct" : "class"));
            keywords.Add(SafeIdentifier(@class.Name));

            Write(string.Join(" ", keywords));

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
                if (@class.IsRefType && !@class.IsOpaque)
                    bases.Add("IDisposable");
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

        private void GenerateClassInternalsField(LayoutField field, Class @class)
        {
            Declaration decl;
            field.QualifiedType.Type.TryGetDeclaration(out decl);

            var arrayType = field.QualifiedType.Type.Desugar() as ArrayType;
            var coreType = field.QualifiedType.Type.Desugar();
            if (arrayType != null && arrayType.SizeType == ArrayType.ArraySize.Constant)
                coreType = arrayType.Type.Desugar();
            // we do not support the primitives below yet because their representation in C# is problematic
            if (coreType.IsPrimitiveType(PrimitiveType.Int128) ||
                coreType.IsPrimitiveType(PrimitiveType.UInt128))
                return;

            TypePrinterResult retType = TypePrinter.VisitFieldDecl(
                new Field { Name = field.Name, QualifiedType = field.QualifiedType });

            PushBlock(BlockKind.Field);

            if (!Options.GenerateSequentialLayout || @class.IsUnion)
                WriteLine($"[FieldOffset({field.Offset})]");
            var access = decl != null && !decl.IsGenerated ? "internal" : "public";
            if (field.Expression != null)
            {
                var fieldValuePrinted = field.Expression.CSharpValue(ExpressionPrinter);
                Write($"{access} {retType}{retType.NameSuffix} = {fieldValuePrinted};");
            }
            else
            {
                Write($"{access} {retType}{retType.NameSuffix};");
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassField(Field field, bool @public = false)
        {
            PushBlock(BlockKind.Field);

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
            PushBlock(BlockKind.Method);
            Write("set");

            if (decl is Function)
            {
                if (isAbstract)
                {
                    Write(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteStartBraceIndent();
                GenerateFunctionSetter(property);
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteStartBraceIndent();
                GenerateVariableSetter(decl, @class);
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapSetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteStartBraceIndent();
                GenerateFieldSetter(field, @class);
            }
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVariableSetter<T>(T decl, Class @class) where T : Declaration, ITypedDecl
        {
            var var = decl as Variable;

            TypePrinter.PushContext(TypePrinterContextKind.Native);

            var location = $@"CppSharp.SymbolResolver.ResolveSymbol(""{
                GetLibraryOf(decl)}"", ""{var.Mangled}"")";

            string ptr = Generator.GeneratedIdentifier("ptr");
            var arrayType = decl.Type as ArrayType;
            var isRefTypeArray = arrayType != null && @class != null && @class.IsRefType;
            if (isRefTypeArray)
                WriteLine($@"var {ptr} = {
                    (arrayType.Type.IsPrimitiveType(PrimitiveType.Char) &&
                     arrayType.QualifiedType.Qualifiers.IsConst ?
                        string.Empty : "(byte*)")}{location};");
            else
                WriteLine($"var {ptr} = ({var.Type}*){location};");

            TypePrinter.PopContext();

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = decl.QualifiedType
            };

            var ctx = new CSharpMarshalContext(Context)
            {
                Parameter = param,
                ArgName = param.Name,
                ReturnType = new QualifiedType(var.Type)
            };
            ctx.PushMarshalKind(MarshalKind.Variable);

            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            decl.CSharpMarshalToNative(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                PushIndent();

            WriteLine($"*{ptr} = {marshal.Context.Return};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                WriteCloseBraceIndent();
        }

        private void GenerateFunctionSetter(Property property)
        {
            var param = new Parameter
            {
                Name = "value",
                QualifiedType = property.SetMethod.Parameters[0].QualifiedType
            };

            if (!property.Type.Equals(param.Type) && property.Type.IsEnumType())
                param.Name = "&" + param.Name;

            var @class = (Class) property.Namespace;
            var function = property.SetMethod;
            var method = function as Method;
            if (function.SynthKind == FunctionSynthKind.AbstractImplCall)
                GenerateVirtualPropertyCall(method, @class.BaseClass, property,
                    new List<Parameter> { param });
            else if (method != null && method.IsVirtual)
                GenerateVirtualPropertyCall(method, @class, property, new List<Parameter> { param });
            else if (method != null && method.OperatorKind == CXXOperatorKind.Subscript)
            {
                if (method.OperatorKind == CXXOperatorKind.Subscript)
                    GenerateIndexerSetter(method);
                else
                    GenerateInternalFunctionCall(property.SetMethod, new List<Parameter> { param });
            }
            else
                GenerateInternalFunctionCall(function, new List<Parameter> { param });
        }

        private void GenerateFieldSetter(Field field, Class @class)
        {
            var param = new Parameter
            {
                Name = "value",
                QualifiedType = field.QualifiedType
            };

            var ctx = new CSharpMarshalContext(Context)
            {
                Parameter = param,
                ArgName = param.Name,
            };
            ctx.PushMarshalKind(MarshalKind.NativeField);

            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            ctx.Declaration = field;

            var arrayType = field.Type.Desugar() as ArrayType;

            if (arrayType != null && @class.IsValueType)
            {
                ctx.ReturnVarName = HandleValueArray(arrayType, field);
            }
            else
            {
                var name = @class.Layout.Fields.First(f => f.FieldPtr == field.OriginalPtr).Name;
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                ctx.ReturnVarName = string.Format("{0}{1}{2}",
                    @class.IsValueType
                        ? Helpers.InstanceField
                        : string.Format("(({0}*) {1})", @class.Visit(TypePrinter),
                            Helpers.InstanceIdentifier),
                    @class.IsValueType ? "." : "->",
                    SafeIdentifier(name));
                TypePrinter.PopContext();
            }
            param.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

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
                typePrinter.PushContext(TypePrinterContextKind.Native);
                type = originalType.Visit(typePrinter).Type;
            }
            else
            {
                type = originalType.ToString();
            }

            var name = ((Class) field.Namespace).Layout.Fields.First(
                f => f.FieldPtr == field.OriginalPtr).Name;
            WriteLine(string.Format("fixed ({0} {1} = {2}.{3})",
                type, arrPtr, Helpers.InstanceField, SafeIdentifier(name)));
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
            var @internal = $@"{Helpers.InternalStruct}{
                Helpers.GetSuffixForInternal(function.Namespace)}";
            if (type.IsPrimitiveType(out primitiveType))
            {
                WriteLine($@"*{@internal}.{internalFunction}({
                    GetInstanceParam(function)}, {function.Parameters[0].Name}) = value;");
            }
            else
            {
                var typeString = type.ToString();
                Class @class;
                var isValueType = (type.GetFinalPointee() ?? type).TryGetClass(out @class) &&
                    @class.IsValueType;
                var paramMarshal = GenerateFunctionParamMarshal(
                    function.Parameters[0], 0, function);
                WriteLine($@"*({typeString}.{@internal}*) {@internal}.{internalFunction}({
                    GetInstanceParam(function)}, {(paramMarshal.Context == null ?
                    paramMarshal.Name : paramMarshal.Context.Return)}) = {
                    (isValueType ? string.Empty : $@"*({typeString}.{@internal}*) ")}value.{
                    Helpers.InstanceIdentifier};");
            }
        }

        private void GeneratePropertyGetter<T>(T decl, Class @class,
            bool isAbstract = false, Property property = null)
            where T : Declaration, ITypedDecl
        {
            PushBlock(BlockKind.Method);
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
                if (isAbstract)
                {
                    Write(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteStartBraceIndent();
                GenerateFunctionGetter(property);
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapGetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteStartBraceIndent();
                GenerateFieldGetter(field, @class);
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteStartBraceIndent();
                GenerateVariableGetter(decl, @class);
            }
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVariableGetter<T>(T decl, Class @class) where T : Declaration, ITypedDecl
        {
            var var = decl as Variable;

            TypePrinter.PushContext(TypePrinterContextKind.Native);

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

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                PushIndent();

            WriteLine("return {0};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                WriteCloseBraceIndent();
        }

        private void GenerateFunctionGetter(Property property)
        {
            var @class = (Class) property.Namespace;
            if (property.GetMethod.SynthKind == FunctionSynthKind.AbstractImplCall)
                GenerateVirtualPropertyCall(property.GetMethod, @class.BaseClass, property);
            else if (property.GetMethod.IsVirtual)
                GenerateVirtualPropertyCall(property.GetMethod, @class, property);
            else GenerateInternalFunctionCall(property.GetMethod,
                property.GetMethod.Parameters, property.QualifiedType.Type);
        }

        private void GenerateFieldGetter(Field field, Class @class)
        {
            var name = @class.Layout.Fields.First(f => f.FieldPtr == field.OriginalPtr).Name;
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var ctx = new CSharpMarshalContext(Context)
            {
                ArgName = field.Name,
                Declaration = field,
                ReturnVarName = $@"{(@class.IsValueType ? Helpers.InstanceField :
                    $"(({@class.Visit(TypePrinter)}*) {Helpers.InstanceIdentifier})")}{
                    (@class.IsValueType ? "." : "->")}{SafeIdentifier(name)}",
                ReturnType = field.QualifiedType
            };
            ctx.PushMarshalKind(MarshalKind.NativeField);
            
            TypePrinter.PopContext();

            var arrayType = field.Type.Desugar() as ArrayType;

            if (arrayType != null && @class.IsValueType)
                ctx.ReturnVarName = HandleValueArray(arrayType, field);

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            field.CSharpMarshalToManaged(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

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

                PushBlock(BlockKind.Property);

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
                        GeneratePropertyGetter(prop.Field, @class);

                    if (prop.HasSetter)
                        GeneratePropertySetter(prop.Field, @class);
                }
                else
                {
                    if (prop.HasGetter)
                        GeneratePropertyGetter(prop.GetMethod, @class, prop.IsPure, prop);

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
                return SafeIdentifier(prop.Name);

            var @params = prop.Parameters.Select(param => {
                var p = new Parameter(param);
                p.Usage = ParameterUsage.In;
                return p;
            });

            return $"this[{FormatMethodParameters(@params)}]";
        }

        private void GenerateVariable(Class @class, Variable variable)
        {
            PushBlock(BlockKind.Variable);

            GenerateDeclarationCommon(variable);
            WriteLine("public static {0} {1}", variable.Type, variable.Name);
            WriteStartBraceIndent();

            GeneratePropertyGetter(variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst &&
                !(variable.Type.Desugar() is ArrayType))
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
            var containingClass = @class;
            @class = @class.IsDependent ? @class.Specializations[0] : @class;

            var wrappedEntries = GetUniqueVTableMethodEntries(@class);
            if (wrappedEntries.Count == 0)
                return;

            PushBlock(BlockKind.Region);
            WriteLine("#region Virtual table interop");
            NewLine();

            // Generate a delegate type for each method.
            foreach (var method in wrappedEntries.Select(e => e.Method))
                GenerateVTableMethodDelegates(containingClass, containingClass.IsDependent ?
                   (Method) method.InstantiatedFrom : method);

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
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            if (Context.ParserOptions.IsMicrosoftAbi)
                WriteLine("__OriginalVTables = new void*[] {{ {0} }};",
                    string.Join(", ",
                        @class.Layout.VTablePointers.Select(v =>
                            $"*(void**) ({Helpers.InstanceIdentifier} + {v.Offset})")));
            else
                WriteLine(
                    $@"__OriginalVTables = new void*[] {{ *(void**) ({
                        Helpers.InstanceIdentifier} + {@class.Layout.VTablePointers[0].Offset}) }};");
            TypePrinter.PopContext();
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
                    @class.Layout.VTablePointers[i].Offset, i, destructorOnly);
            }

            WriteCloseBraceIndent();
            NewLine();

            for (int i = 0; i < @class.Layout.VTablePointers.Count; i++)
            {
                var offset = @class.Layout.VTablePointers[i].Offset;
                WriteLine($"*(void**) ({Helpers.InstanceIdentifier} + {offset}) = {managedVTables}[{i}];");
            }
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
                wrappedEntries, @class.Layout.VTablePointers[0].Offset, 0, destructorOnly);

            WriteCloseBraceIndent();
            NewLine();

            var offset = @class.Layout.VTablePointers[0].Offset;
            WriteLine($"*(void**) ({Helpers.InstanceIdentifier} + {offset}) = {managedVTables}[0];");
        }

        private void AllocateNewVTableEntries(IList<VTableComponent> entries,
            IList<VTableComponent> wrappedEntries, uint vptrOffset, int tableIndex, bool destructorOnly)
        {
            var pointerSize = Context.TargetInfo.PointerWidth / 8;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var offset = pointerSize
                    * (i - (Context.ParserOptions.IsMicrosoftAbi ? 0 : VTables.ItaniumOffsetToTopAndRTTI));

                var nativeVftableEntry = $@"*(void**) (new IntPtr(*(void**) {
                    Helpers.InstanceIdentifier}) + {vptrOffset} + {offset})";
                var managedVftableEntry = $"*(void**) (vfptr{tableIndex} + {offset})";

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
                WriteLine($@"SetupVTables(GetType().FullName == ""{
                    typeFullName.Type.Replace("global::", string.Empty)}"");");
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

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

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
                };
                ctx.PushMarshalKind(MarshalKind.VTableReturnValue);

                var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
                method.OriginalReturnType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                if (method.HasIndirectReturnTypeParameter)
                {
                    var retParam = method.Parameters.First(p => p.Kind == ParameterKind.IndirectReturnType);
                    TypePrinter.PushContext(TypePrinterContextKind.Native);
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
            PushBlock(BlockKind.VTableDelegate);

            // This works around a parser bug, see https://github.com/mono/CppSharp/issues/202
            if (method.Signature != null)
            {
                var cleanSig = method.Signature.ReplaceLineBreaks("");
                cleanSig = Regex.Replace(cleanSig, @"\s+", " ");

                WriteLine("// {0}", cleanSig);
            }

            TypePrinterResult retType;
            var @params = GatherInternalParams(method, out retType);

            var vTableMethodDelegateName = GetVTableMethodDelegateName(method);

            WriteLine("private static {0} {1}Instance;",
                Context.Delegates[method].Signature,
                vTableMethodDelegateName);
            NewLine();

            WriteLine("private static {0} {1}Hook({2})", retType, vTableMethodDelegateName,
                string.Join(", ", @params));
            WriteStartBraceIndent();

            WriteLine("if (!NativeToManagedMap.ContainsKey(instance))");
            WriteLineIndent("throw new global::System.Exception(\"No managed instance was found\");");
            NewLine();

            WriteLine("var {0} = ({1}) NativeToManagedMap[instance];", Helpers.TargetIdentifier, @class.Visit(TypePrinter));
            WriteLine("if ({0}.{1})", Helpers.TargetIdentifier, Helpers.OwnsNativeInstanceIdentifier);
            WriteLineIndent("{0}.SetupVTables();", Helpers.TargetIdentifier);
            GenerateVTableManagedCall(method);

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.Always);
        }

        public string GetVTableMethodDelegateName(Function function)
        {
            var nativeId = GetFunctionNativeIdentifier(function, true);

            // Trim '@' (if any) because '@' is valid only as the first symbol.
            nativeId = nativeId.Trim('@');

            return string.Format("_{0}Delegate", nativeId);
        }

        #endregion

        #region Events

        public override bool VisitEvent(Event @event)
        {
            if (!@event.IsGenerated)
                return true;

            PushBlock(BlockKind.Event, @event);
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            var delegateInstance = Generator.GeneratedIdentifier(@event.OriginalName);
            var delegateName = delegateInstance + "Delegate";
            var delegateRaise = delegateInstance + "RaiseInstance";

            WriteLine("[UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.Cdecl)]");
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

            return true;
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
            TypePrinter.PushContext(TypePrinterContextKind.Native);
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
                if (ASTUtils.CheckIgnoreMethod(ctor))
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
                    if(!@class.IsOpaque)GenerateDisposeMethods(@class);
            }
        }

        private void GenerateClassFinalizer(INamedDecl @class)
        {
            if (!Options.GenerateFinalizers)
                return;

            PushBlock(BlockKind.Finalizer);

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
                PushBlock(BlockKind.Method);
                WriteLine("public void Dispose()");
                WriteStartBraceIndent();

                WriteLine("Dispose(disposing: true);");
                if (Options.GenerateFinalizers)
                    WriteLine("GC.SuppressFinalize(this);");

                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate Dispose(bool) method
            PushBlock(BlockKind.Method);
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

                WriteLine("if ({0} == IntPtr.Zero)", Helpers.InstanceIdentifier);
                WriteLineIndent("return;");

                // The local var must be of the exact type in the object map because of TryRemove
                WriteLine("{0} {1};",
                    (@interface ?? (@base.IsAbstractImpl ? @base.BaseClass : @base)).Visit(TypePrinter),
                    Helpers.DummyIdentifier);
                WriteLine("NativeToManagedMap.TryRemove({0}, out {1});",
                    Helpers.InstanceIdentifier, Helpers.DummyIdentifier);
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var classInternal = @class.Visit(TypePrinter);
                TypePrinter.PopContext();
                if (@class.IsDynamic && GetUniqueVTableMethodEntries(@class).Count != 0)
                {
                    if (Context.ParserOptions.IsMicrosoftAbi)
                        for (var i = 0; i < @class.Layout.VTablePointers.Count; i++)
                            WriteLine($@"(({classInternal}*) {Helpers.InstanceIdentifier})->{
                                @class.Layout.VTablePointers[i].Name} = new global::System.IntPtr(__OriginalVTables[{i}]);");
                    else
                        WriteLine($@"(({classInternal}*) {Helpers.InstanceIdentifier})->{
                            @class.Layout.VTablePointers[0].Name} = new global::System.IntPtr(__OriginalVTables[0]);");
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
                        GenerateDestructorCall(dtor);
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

            WriteLine("{0} = IntPtr.Zero;", Helpers.InstanceIdentifier);
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateDestructorCall(Method dtor)
        {
            var @class = (Class) dtor.Namespace;
            GenerateVirtualFunctionCall(dtor, @class, true);
            if (@class.IsAbstract)
            {
                WriteCloseBraceIndent();
                WriteLine("else");
                PushIndent();
                GenerateInternalFunctionCall(dtor);
                PopIndent();
            }
        }

        private void GenerateNativeConstructor(Class @class)
        {
            var shouldGenerateClassNativeField = ShouldGenerateClassNativeField(@class);
            if (@class.IsRefType && shouldGenerateClassNativeField)
            {
                PushBlock(BlockKind.Field);
                WriteLine("protected bool {0};", Helpers.OwnsNativeInstanceIdentifier);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            var suffix = @class.IsAbstract ? "Internal" : string.Empty;
            var ctorCall = $"{@class.Visit(TypePrinter)}{suffix}";
            if (!@class.IsAbstractImpl)
            {
                PushBlock(BlockKind.Method);
                WriteLine("internal static {0}{1} {2}(global::System.IntPtr native, bool skipVTables = false)",
                    @class.NeedsBase && !@class.BaseClass.IsInterface ? "new " : string.Empty,
                    @class.Visit(TypePrinter), Helpers.CreateInstanceIdentifier);
                WriteStartBraceIndent();
                WriteLine("return new {0}(native.ToPointer(), skipVTables);", ctorCall);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            GenerateNativeConstructorByValue(@class, ctorCall);

            PushBlock(BlockKind.Method);
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
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                WriteLine($"{Helpers.InstanceField} = *({@class.Visit(TypePrinter)}*) native;");
                TypePrinter.PopContext();
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNativeConstructorByValue(Class @class, string ctorCall)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var @internal = (@class.IsAbstractImpl ? @class.BaseClass : @class).Visit(TypePrinter);
            TypePrinter.PopContext();

            if (!@class.IsAbstractImpl)
            {
                PushBlock(BlockKind.Method);
                WriteLine("internal static {0} {1}({2} native, bool skipVTables = false)",
                    @class.Visit(TypePrinter), Helpers.CreateInstanceIdentifier, @internal);
                WriteStartBraceIndent();
                WriteLine("return new {0}(native, skipVTables);", ctorCall);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (@class.IsRefType && !@class.IsAbstract)
            {
                PushBlock(BlockKind.Method);
                WriteLine($"private static void* __CopyValue({@internal} native)");
                WriteStartBraceIndent();
                var copyCtorMethod = @class.Methods.FirstOrDefault(method =>
                    method.IsCopyConstructor);
                if (@class.HasNonTrivialCopyConstructor && copyCtorMethod != null &&
                    copyCtorMethod.IsGenerated)
                {
                    // Allocate memory for a new native object and call the ctor.
                    WriteLine($"var ret = Marshal.AllocHGlobal(sizeof({@internal}));");
                    TypePrinter.PushContext(TypePrinterContextKind.Native);
                    WriteLine($"{@class.Visit(TypePrinter)}.{GetFunctionNativeIdentifier(copyCtorMethod)}(ret, new global::System.IntPtr(&native));",
                        @class.Visit(TypePrinter),
                        GetFunctionNativeIdentifier(copyCtorMethod));
                    TypePrinter.PopContext();
                    WriteLine("return ret.ToPointer();");
                }
                else
                {
                    WriteLine($"var ret = Marshal.AllocHGlobal(sizeof({@internal}));");
                    WriteLine($"*({@internal}*) ret = native;");
                    WriteLine("return ret.ToPointer();");
                }
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            if (!@class.IsAbstract)
            {
                PushBlock(BlockKind.Method);
                WriteLine("{0} {1}({2} native, bool skipVTables = false)",
                    @class.IsAbstractImpl ? "internal" : "private", @class.Name, @internal);
                WriteLineIndent(@class.IsRefType ? ": this(__CopyValue(native), skipVTables)" : ": this()");
                WriteStartBraceIndent();
                if (@class.IsRefType)
                {
                    WriteLine($"{Helpers.OwnsNativeInstanceIdentifier} = true;");
                    WriteLine($"NativeToManagedMap[{Helpers.InstanceIdentifier}] = this;");
                }
                else
                {
                    WriteLine($"{Helpers.InstanceField} = native;");
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
            PushBlock(BlockKind.Function);
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

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            if (method.IsVirtual && !method.IsGeneratedOverride() && !method.IsOperator && !method.IsPure)
                Write("virtual ");

            var isBuiltinOperator = method.IsOperator &&
                Operators.IsBuiltinOperator(method.OperatorKind);

            if (method.IsStatic || isBuiltinOperator)
                Write("static ");

            if (method.IsGeneratedOverride())
                Write("override ");

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
        }

        public void GenerateMethod(Method method, Class @class)
        {
            PushBlock(BlockKind.Method, method);
            GenerateDeclarationCommon(method);

            if (method.ExplicitInterfaceImpl == null)
            {
                Write(Helpers.GetAccess(GetValidMethodAccess(method)));
            }

            GenerateMethodSpecifier(method, @class);

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload && method.IsConstructor && !method.IsPure)
            {
                Write(" : this({0})",
                    string.Join(", ",
                        method.Parameters.Where(
                            p => p.Kind == ParameterKind.Regular).Select(
                                p => p.Ignore ? ExpressionPrinter.VisitParameter(p) : p.Name)));
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

            GenerateMethodBody(method);

            SkipImpl:

            WriteCloseBraceIndent();

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                GenerateEqualsAndGetHashCode(method, @class);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateMethodBody(Method method)
        {
            var @class = (Class) method.Namespace;
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
        }

        private string OverloadParamNameWithDefValue(Parameter p, ref int index)
        {
            Class @class;
            return p.Type.IsPointerToPrimitiveType() && p.Usage == ParameterUsage.InOut && p.HasDefaultValue
                ? "ref param" + index++
                : (( p.Type.TryGetClass(out @class) && @class.IsInterface) ? "param" + index++
                     : ExpressionPrinter.VisitParameter(p));
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
                        ExpressionPrinter.VisitParameter(parameter));
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
                    TypePrinter.PushContext(TypePrinterContextKind.Native);
                    WriteLine($@"return (*({@class.Visit(TypePrinter)}*) {
                        Helpers.InstanceIdentifier}).GetHashCode();");
                    TypePrinter.PopContext();
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
            Function @virtual = method;
            if (method.OriginalFunction != null &&
                !((Class) method.OriginalFunction.Namespace).IsInterface)
                @virtual = method.OriginalFunction;

            var i = VTables.GetVTableIndex(@virtual, @class);
            int vtableIndex = 0;
            if (Context.ParserOptions.IsMicrosoftAbi)
                vtableIndex = @class.Layout.VFTables.IndexOf(@class.Layout.VFTables.Where(
                    v => v.Layout.Components.Any(c => c.Method == @virtual)).First());
            WriteLine("var {0} = *(void**) ((IntPtr) __OriginalVTables[{1}] + {2} * {3});",
                Helpers.SlotIdentifier, vtableIndex, i, Context.TargetInfo.PointerWidth / 8);
            if (method.IsDestructor && @class.IsAbstract)
            {
                WriteLine("if ({0} != null)", Helpers.SlotIdentifier);
                WriteStartBraceIndent();
            }

            var @delegate = GetVTableMethodDelegateName(@virtual);
            delegateId = Generator.GeneratedIdentifier(@delegate);

            WriteLine("var {0} = ({1}) Marshal.GetDelegateForFunctionPointer(new IntPtr({2}), typeof({1}));",
                delegateId, Context.Delegates[method].Signature,
                Helpers.SlotIdentifier);
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
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var @internal = (@class.IsAbstractImpl ? @class.BaseClass : @class).Visit(TypePrinter);
            TypePrinter.PopContext();
            WriteLine($"{Helpers.InstanceIdentifier} = Marshal.AllocHGlobal(sizeof({@internal}));");
            WriteLine($"{Helpers.OwnsNativeInstanceIdentifier} = true;");
            WriteLine($"NativeToManagedMap[{Helpers.InstanceIdentifier}] = this;");

            if (method.IsCopyConstructor)
            {
                if (@class.HasNonTrivialCopyConstructor)
                    GenerateInternalFunctionCall(method);
                else
                {
                    TypePrinter.PushContext(TypePrinterContextKind.Native);
                    var classInternal = @class.Visit(TypePrinter);
                    TypePrinter.PopContext();
                    WriteLine($@"*(({classInternal}*) {Helpers.InstanceIdentifier}) = *(({
                        classInternal}*) {method.Parameters[0].Name}.{Helpers.InstanceIdentifier});");
                }
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

            var @class = function.Namespace as Class;

            string @internal = Helpers.InternalStruct;
            if (@class != null && @class is ClassTemplateSpecialization)
            {
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                @internal = @class.Visit(TypePrinter).Type;
                TypePrinter.PopContext();
            }

            var functionName = string.Format("{0}.{1}", @internal,
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
                    TypePrinter.PushContext(TypePrinterContextKind.Native);
                    WriteLine($"var {Helpers.ReturnIdentifier} = new {@class.Visit(TypePrinter)}();");
                    TypePrinter.PopContext();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(construct))
                    {
		                var typePrinterContext = new TypePrinterContext
		                {
		                    Type = indirectRetType.Type.Desugar()
		                };

                        WriteLine("{0} {1};", typeMap.CSharpSignature(typePrinterContext),
                            Helpers.ReturnIdentifier);
                    }
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
                    WriteLine($@"fixed ({Helpers.InternalStruct}{
                        Helpers.GetSuffixForInternal(originalFunction.Namespace)}* __instancePtr = &{
                        Helpers.InstanceField})");
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

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

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

        private string GetInstanceParam(Function function)
        {
            var from = (Class) function.Namespace;
            var to = (function.OriginalFunction == null ||
                // we don't need to offset the instance with Itanium if there's an existing interface impl
                (Context.ParserOptions.IsItaniumLikeAbi &&
                 !((Class) function.OriginalNamespace).IsInterface)) &&
                 function.SynthKind != FunctionSynthKind.AbstractImplCall ?
                @from.BaseClass : (Class) function.OriginalFunction.Namespace;

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
                if (param.Type.Desugar().IsPrimitiveTypeConvertibleToRef())
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

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

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
            // Do not delete instance in MS ABI.
            var name = param.Name;
            param.Name = param.Kind == ParameterKind.ImplicitDestructorParameter ? "0" : name;

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

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (paramMarshal.HasUsingBlock)
                PushIndent();

            if (marshal.Context.Return.ToString() == param.Name)
                paramMarshal.Name = param.Name;
            else
                WriteLine("var {0} = {1};", argName, marshal.Context.Return);

            param.Name = name;

            return paramMarshal;
        }

        private string FormatMethodParameters(IEnumerable<Parameter> @params)
        {
            return TypePrinter.VisitParameters(@params, true).Type;
        }

        #endregion

        public override bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            if (!typedef.IsGenerated)
                return false;

            GenerateDeclarationCommon(typedef);

            FunctionType functionType;

            if (typedef.Type.IsPointerTo(out functionType))
            {
                PushBlock(BlockKind.Typedef);
                var attributedType = typedef.Type.GetPointee() as AttributedType;
                var callingConvention = attributedType == null
                    ? functionType.CallingConvention
                    : ((FunctionType) attributedType.Equivalent.Type).CallingConvention;
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var interopCallConv = callingConvention.ToInteropCallConv();
                if (interopCallConv == System.Runtime.InteropServices.CallingConvention.Winapi)
                    WriteLine("[SuppressUnmanagedCodeSecurity]");
                else
                    WriteLine(
                        "[SuppressUnmanagedCodeSecurity, " +
                        "UnmanagedFunctionPointer(global::System.Runtime.InteropServices.CallingConvention.{0})]",
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

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return true;

            PushBlock(BlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            if (@enum.IsFlags)
                WriteLine("[Flags]");

            Write(Helpers.GetAccess(@enum.Access));
            // internal P/Invoke declarations must see protected enums
            if (@enum.Access == AccessSpecifier.Protected)
                Write("internal ");
            Write("enum {0}", SafeIdentifier(@enum.Name));

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int)
                Write(" : {0}", typeName);

            NewLine();

            WriteStartBraceIndent();
            GenerateEnumItems(@enum);
            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
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

        public static string GetFunctionNativeIdentifier(Function function,
            bool ignoreSpecialization = false)
        {
            var identifier = new StringBuilder();

            if (function.IsOperator)
                identifier.Append($"Operator{function.OperatorKind}");
            else
            {
                var method = function as Method;
                if (method != null)
                {
                    if (method.IsConstructor && !method.IsCopyConstructor)
                        identifier.Append("ctor");
                    else if (method.IsCopyConstructor)
                        identifier.Append("cctor");
                    else if (method.IsDestructor)
                        identifier.Append("dtor");
                    else
                        identifier.Append(GetMethodIdentifier(method));
                }
                else
                {
                    identifier.Append(function.Name);
                }
            }

            var specialization = function.Namespace as ClassTemplateSpecialization;
            if (specialization != null && !ignoreSpecialization)
                identifier.Append(Helpers.GetSuffixFor(specialization));

            var overloads = function.Namespace.GetOverloads(function)
                .ToList();
            var index = overloads.IndexOf(function);

            if (index >= 0)
            {
                identifier.Append('_');
                identifier.Append(index.ToString(CultureInfo.InvariantCulture));
            }
            else if (function.Index.HasValue)
            {
                identifier.Append('_');
                identifier.Append(function.Index.Value);
            }

            return identifier.ToString();
        }

        public void GenerateInternalFunction(Function function)
        {
            if (function.IsPure)
                return;

            if (function.OriginalFunction != null)
            {
                var @class = function.OriginalNamespace as Class;
                if (@class != null && @class.IsInterface)
                    function = function.OriginalFunction;
            }

            PushBlock(BlockKind.InternalsClassMethod);
            WriteLine("[SuppressUnmanagedCodeSecurity]");
            Write("[DllImport(\"{0}\", ", GetLibraryOf(function));

            var callConv = function.CallingConvention.ToInteropCallConv();
            WriteLine("CallingConvention = global::System.Runtime.InteropServices.CallingConvention.{0},",
                callConv);

            WriteLineIndent("EntryPoint=\"{0}\")]", function.Mangled);

            if (function.ReturnType.Type.IsPrimitiveType(PrimitiveType.Bool))
                WriteLine("[return: MarshalAs(UnmanagedType.I1)]");

            TypePrinterResult retType;
            var @params = GatherInternalParams(function, out retType);

            WriteLine("internal static extern {0} {1}({2});", retType,
                      GetFunctionNativeIdentifier(function),
                      string.Join(", ", @params));
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private string GetLibraryOf(Declaration declaration)
        {
            if (declaration.TranslationUnit.IsSystemHeader)
                return Context.Options.SystemModule.SymbolsLibraryName;

            string libName = declaration.TranslationUnit.Module.SharedLibraryName;

            NativeLibrary library;
            Context.Symbols.FindLibraryBySymbol(((IMangledDecl) declaration).Mangled, out library);

            if (library != null)
                libName = Path.GetFileNameWithoutExtension(library.FileName);

            if (Options.StripLibPrefix && libName != null && libName.Length > 3 &&
                libName.StartsWith("lib", StringComparison.Ordinal))
            {
                libName = libName.Substring(3);
            }
            if (libName == null)
                libName = declaration.TranslationUnit.Module.SharedLibraryName;

            var targetTriple = Context.ParserOptions.TargetTriple;
            if (Options.GenerateInternalImports)
                libName = "__Internal";
            else if (TargetTriple.IsWindows(targetTriple) &&
                libName.Contains('.') && Path.GetExtension(libName) != ".dll")
                libName += ".dll";

            if (targetTriple.Contains("apple") || targetTriple.Contains("darwin") ||
                targetTriple.Contains("osx"))
            {
                var framework = libName + ".framework";
                for (uint i = 0; i < Context.ParserOptions.LibraryDirsCount; i++)
                {
                    var libDir = Context.ParserOptions.GetLibraryDirs(i);
                    if (Path.GetFileName(libDir) == framework && File.Exists(Path.Combine(libDir, libName)))
                        return $"@executable_path/../Frameworks/{framework}/{libName}";
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