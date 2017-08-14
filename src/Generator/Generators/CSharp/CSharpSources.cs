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

            GenerateExternalClassTemplateSpecializations();
        }

        private void GenerateExternalClassTemplateSpecializations()
        {
            foreach (var group in from spec in Module.ExternalClassTemplateSpecializations
                                  let module = spec.TranslationUnit.Module
                                  group spec by module.OutputNamespace into @group
                                  select @group)
            {
                PushBlock(BlockKind.Namespace);
                if (!string.IsNullOrEmpty(group.Key))
                {
                    WriteLine($"namespace {group.Key}");
                    WriteStartBraceIndent();
                }

                foreach (var template in from s in @group
                                         group s by s.TemplatedDecl.TemplatedClass into template
                                         select template)
                {
                    var declContext = template.Key.Namespace;
                    var declarationContexts = new Stack<DeclarationContext>();
                    while (!(declContext is TranslationUnit))
                    {
                        declarationContexts.Push(declContext);
                        declContext = declContext.Namespace;
                    }

                    foreach (var declarationContext in declarationContexts)
                    {
                        WriteLine($"namespace {declarationContext.Name}");
                        WriteStartBraceIndent();
                    }

                    GenerateClassTemplateSpecializationsInternals(
                        template.Key, template.ToList(), false);

                    foreach (var declarationContext in declarationContexts)
                        WriteCloseBraceIndent();
                }

                if (!string.IsNullOrEmpty(group.Key))
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
            var isTranslationUnit = context is TranslationUnit;

            var shouldGenerateNamespace = !@namespace.IsInline && !isTranslationUnit &&
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

            GenerateClassTemplateSpecializationsInternals(classTemplate,
                classTemplate.Specializations, true);
        }

        private void GenerateClassTemplateSpecializationsInternals(Class classTemplate,
            IList<ClassTemplateSpecialization> specializations, bool generateNested)
        {
            PushBlock(BlockKind.Namespace);
            var generated = GetGenerated(specializations);
            WriteLine("namespace {0}{1}",
                classTemplate.OriginalNamespace is Class ?
                    classTemplate.OriginalNamespace.Name + '_' : string.Empty,
                classTemplate.Name);
            WriteStartBraceIndent();

            foreach (var specialization in generated)
                GenerateClassInternals(specialization);

            if (generateNested)
            {
                var specialization = generated.FirstOrDefault(s => !s.Ignore) ??
                    generated.First();
                foreach (var nestedClass in classTemplate.Classes.Union(
                    specialization.Classes).Where(c => !c.IsDependent))
                {
                    GenerateClassInternalsOnly(nestedClass);
                    foreach (var nestedNestedClass in nestedClass.Classes)
                    {
                        GenerateClassInternalsOnly(nestedNestedClass);
                        WriteCloseBraceIndent();
                    }
                    WriteCloseBraceIndent();
                }
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private IEnumerable<ClassTemplateSpecialization> GetGenerated(
            IList<ClassTemplateSpecialization> specializations)
        {
            var specialization = specializations.FirstOrDefault(s => !s.Ignore);
            if (specialization == null)
                specialization = specializations[0];

            Class classTemplate = specialization.TemplatedDecl.TemplatedClass;
            if (classTemplate.Fields.Any(
                f => f.Type.Desugar() is TemplateParameterType))
                return specializations;

            return new List<ClassTemplateSpecialization> { specialization };
        }

        private void GenerateClassInternalsOnly(Class c)
        {
            NewLine();
            GenerateClassSpecifier(c);
            NewLine();
            WriteStartBraceIndent();
            GenerateClassInternals(c);
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

                if (@class.Specializations.All(s => s.Ignore))
                    return true;
            }

            var typeMaps = new List<System.Type>();
            var keys = new List<string>();
            // disable the type maps, if any, for this class because of copy ctors, operators and others
            this.DisableTypeMap(@class, typeMaps, keys);

            PushBlock(BlockKind.Class);
            GenerateDeclarationCommon(@class);
            GenerateClassSpecifier(@class);

            NewLine();
            WriteStartBraceIndent();

            if (!@class.IsAbstractImpl && !@class.IsDependent)
                GenerateClassInternals(@class);

            VisitDeclContext(@class);

            if (!@class.IsGenerated)
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
                    var @interface = @class.Namespace.Classes.FirstOrDefault(
                        c => c.IsInterface && c.OriginalClass == @class);
                    var printedClass = (@interface ?? @class).Visit(TypePrinter);
                    var dict = $@"global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, {
                        printedClass}>";
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

            for (int i = 0; i < typeMaps.Count; i++)
                Context.TypeMaps.TypeMaps.Add(keys[i], typeMaps[i]);

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
            else if (@class.MaxFieldAlignment > 0)
                WriteLine($"[StructLayout(LayoutKind.Sequential, Pack = {@class.MaxFieldAlignment})]");

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
                (template = currentSpecialization.TemplatedDecl.TemplatedClass)
                .GetSpecializationsToGenerate().Count == 1)
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
                if (method.IsSynthetized)
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
                    if (@class.IsStatic)
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

            retType = function.ReturnType.Visit(TypePrinter);

            var @params = function.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi).Select(p =>
                $"{p.Visit(TypePrinter)} {p.Name}").ToList();

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
            if (@class.IsDependent && @class.TemplateParameters.Any())
                Write($"<{string.Join(", ", @class.TemplateParameters.Select(p => p.Name))}>");

            var bases = new List<string>();

            if (@class.NeedsBase)
            {
                foreach (var @base in @class.Bases.Where(b => b.IsClass))
                {
                    var typeMaps = new List<System.Type>();
                    var keys = new List<string>();
                    this.DisableTypeMap(@base.Class, typeMaps, keys);
                    var printedBase = @base.Type.Desugar().Visit(TypePrinter);
                    bases.Add(printedBase.Type);
                    for (int i = 0; i < typeMaps.Count; i++)
                        Context.TypeMaps.TypeMaps.Add(keys[i], typeMaps[i]);
                }
            }

            if (@class.IsGenerated)
            {
                if (@class.IsRefType && !@class.IsOpaque)
                    bases.Add("IDisposable");
            }

            if (bases.Count > 0 && !@class.IsStatic)
                Write(" : {0}", string.Join(", ", bases));
        }

        private void GenerateClassInternalsField(LayoutField field, Class @class)
        {
            TypePrinterResult retType = TypePrinter.VisitFieldDecl(
                new Field { Name = field.Name, QualifiedType = field.QualifiedType });

            PushBlock(BlockKind.Field);

            if (!Options.GenerateSequentialLayout || @class.IsUnion)
                WriteLine($"[FieldOffset({field.Offset})]");
            Write($"internal {retType}{retType.NameSuffix}");
            if (field.Expression != null)
            {
                var fieldValuePrinted = field.Expression.CSharpValue(ExpressionPrinter);
                Write($" = {fieldValuePrinted}");
            }
            Write(";");

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

                this.GenerateMember(@class, c => GenerateFunctionSetter(c, property), true);
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteStartBraceIndent();
                var var = decl as Variable;
                this.GenerateMember(@class, c => GenerateVariableSetter(
                    c is ClassTemplateSpecialization ?
                        c.Variables.First(v => v.Name == decl.Name) : var),
                    true);
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapSetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteStartBraceIndent();

                this.GenerateField(@class, field, GenerateFieldSetter, true);
            }
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVariableSetter<T>(T decl) where T : Declaration, ITypedDecl
        {
            var var = decl as Variable;

            TypePrinter.PushContext(TypePrinterContextKind.Native);

            var location = $@"CppSharp.SymbolResolver.ResolveSymbol(""{
                GetLibraryOf(decl)}"", ""{var.Mangled}"")";

            string ptr = Generator.GeneratedIdentifier("ptr");
            var arrayType = decl.Type as ArrayType;
            var @class = decl.Namespace as Class;
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
            decl.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                PushIndent();

            WriteLine($"*{ptr} = {marshal.Context.Return};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                WriteCloseBraceIndent();
        }

        private void GenerateFunctionSetter(Class @class, Property property)
        {
            var actualProperty = GetActualProperty(property, @class);
            if (actualProperty == null)
            {
                WriteLine($@"throw new MissingMethodException(""Method {
                    property.Name} missing from explicit specialization {
                    @class.Visit(TypePrinter)}."");");
                return;
            }
            property = actualProperty;
            var param = new Parameter
            {
                Name = "value",
                QualifiedType = property.SetMethod.Parameters[0].QualifiedType,
                Kind = ParameterKind.PropertyValue
            };

            if (!property.Type.Equals(param.Type) && property.Type.IsEnumType())
                param.Name = "&" + param.Name;

            var parameters = new List<Parameter> { param };
            var @void = new QualifiedType(new BuiltinType(PrimitiveType.Void));
            if (property.SetMethod.SynthKind == FunctionSynthKind.AbstractImplCall)
                GenerateVirtualPropertyCall(property.SetMethod, @class.BaseClass,
                   property, parameters, @void);
            else if (property.SetMethod.IsVirtual)
                GenerateVirtualPropertyCall(property.SetMethod, @class,
                    property, parameters, @void);
            else if (property.SetMethod.OperatorKind == CXXOperatorKind.Subscript)
                GenerateIndexerSetter(property.SetMethod);
            else
                GenerateInternalFunctionCall(property.SetMethod, parameters, @void);
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
                var printed = TypePrinter.PrintNative(@class);
                ctx.ReturnVarName = string.Format("{0}{1}{2}",
                    @class.IsValueType
                        ? Helpers.InstanceField
                        : $"(({printed}*) {Helpers.InstanceIdentifier})",
                    @class.IsValueType ? "." : "->",
                    SafeIdentifier(name));
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
                type = TypePrinter.PrintNative(originalType).Type;
            else
                type = originalType.ToString();

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
            function.OriginalReturnType.Type.IsPointerTo(out type);

            var @internal = TypePrinter.PrintNative(function.Namespace);
            var ctx = new CSharpMarshalContext(Context)
            {
                Parameter = new Parameter
                {
                    Name = "value",
                    QualifiedType = new QualifiedType(type)
                },
                ReturnType = new QualifiedType(type)
            };
            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            type.Visit(marshal);
            Write(marshal.Context.Before);

            var internalFunction = GetFunctionNativeIdentifier(function);
            if (type.IsPrimitiveType())
            {
                WriteLine($@"*{@internal}.{internalFunction}({
                    GetInstanceParam(function)}, {function.Parameters[0].Name}) = {
                    marshal.Context.Return};");
            }
            else
            {
                var typeInternal = TypePrinter.PrintNative(type);
                var paramMarshal = GenerateFunctionParamMarshal(
                    function.Parameters[0], 0, function);
                WriteLine($@"*({typeInternal}*) {@internal}.{internalFunction}({
                    GetInstanceParam(function)}, {(paramMarshal.Context == null ?
                    paramMarshal.Name : paramMarshal.Context.Return)}) = {marshal.Context.Return};");
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

                this.GenerateMember(@class, c => GenerateFunctionGetter(c, property));
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapGetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteStartBraceIndent();
                this.GenerateField(@class, field, GenerateFieldGetter, false);
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteStartBraceIndent();
                var var = decl as Variable;
                this.GenerateMember(@class, c => GenerateVariableGetter(
                    c is ClassTemplateSpecialization ?
                        c.Variables.First(v => v.Name == decl.Name) : var));
            }
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVariableGetter<T>(T decl) where T : Declaration, ITypedDecl
        {
            var var = decl as Variable;

            TypePrinter.PushContext(TypePrinterContextKind.Native);

            var location = string.Format("CppSharp.SymbolResolver.ResolveSymbol(\"{0}\", \"{1}\")",
                GetLibraryOf(decl), var.Mangled);

            var arrayType = decl.Type as ArrayType;
            var @class = decl.Namespace as Class;
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
            decl.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                PushIndent();

            WriteLine("return {0};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                WriteCloseBraceIndent();
        }

        private void GenerateFunctionGetter(Class @class, Property property)
        {
            var actualProperty = GetActualProperty(property, @class);
            if (actualProperty == null)
            {
                WriteLine($@"throw new MissingMethodException(""Method {
                    property.Name} missing from explicit specialization {
                    @class.Visit(TypePrinter)}."");");
                return;
            }
            if (actualProperty.GetMethod.SynthKind == FunctionSynthKind.AbstractImplCall)
                GenerateVirtualPropertyCall(actualProperty.GetMethod,
                    @class.BaseClass, actualProperty);
            else if (actualProperty.GetMethod.IsVirtual)
                GenerateVirtualPropertyCall(actualProperty.GetMethod,
                    @class, actualProperty);
            else GenerateInternalFunctionCall(actualProperty.GetMethod,
                actualProperty.GetMethod.Parameters, property.QualifiedType);
        }

        private static Property GetActualProperty(Property property, Class c)
        {
            if (!(c is ClassTemplateSpecialization))
                return property;
            return c.Properties.SingleOrDefault(p => p.GetMethod != null &&
                p.GetMethod.InstantiatedFrom == property.GetMethod);
        }

        private void GenerateFieldGetter(Field field, Class @class)
        {
            var name = @class.Layout.Fields.First(f => f.FieldPtr == field.OriginalPtr).Name;
            var ctx = new CSharpMarshalContext(Context)
            {
                ArgName = field.Name,
                Declaration = field,
                ReturnVarName = $@"{(@class.IsValueType ? Helpers.InstanceField :
                    $"(({TypePrinter.PrintNative(@class)}*) {Helpers.InstanceIdentifier})")}{
                    (@class.IsValueType ? "." : "->")}{SafeIdentifier(name)}",
                ReturnType = field.QualifiedType
            };
            ctx.PushMarshalKind(MarshalKind.NativeField);

            var arrayType = field.Type.Desugar() as ArrayType;

            if (arrayType != null && @class.IsValueType)
                ctx.ReturnVarName = HandleValueArray(arrayType, field);

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            field.QualifiedType.Visit(marshal);

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
                var printedType = prop.Type.Visit(TypePrinter);
                if (prop.ExplicitInterfaceImpl == null)
                {
                    Write(Helpers.GetAccess(prop.Access));

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

                    WriteLine($"{printedType} {GetPropertyName(prop)}");
                }
                else
                {
                    WriteLine($@"{printedType} {
                        prop.ExplicitInterfaceImpl.Name}.{GetPropertyName(prop)}");
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
            if (Context.ParserOptions.IsMicrosoftAbi)
                WriteLine("__OriginalVTables = new void*[] {{ {0} }};",
                    string.Join(", ",
                        @class.Layout.VTablePointers.Select(v =>
                            $"*(void**) ({Helpers.InstanceIdentifier} + {v.Offset})")));
            else
                WriteLine(
                    $@"__OriginalVTables = new void*[] {{ *(void**) ({
                        Helpers.InstanceIdentifier} + {@class.Layout.VTablePointers[0].Offset}) }};");
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
            var numBlocks = 0;
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var param = method.Parameters[i];
                if (param.Ignore)
                    continue;

                if (param.Kind == ParameterKind.IndirectReturnType)
                    continue;

                var ctx = new CSharpMarshalContext(Context)
                {
                    ReturnType = param.QualifiedType,
                    ReturnVarName = param.Name,
                    ParameterIndex = i
                };

                ctx.PushMarshalKind(MarshalKind.GenericDelegate);
                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx) { MarshalsParameter = true };
                param.Visit(marshal);
                ctx.PopMarshalKind();

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                marshals.Add(marshal.Context.Return);
                if (ctx.HasCodeBlock)
                {
                    PushIndent();
                    numBlocks++;
                }
            }

            bool isVoid = method.OriginalReturnType.Type.Desugar().IsPrimitiveType(
                PrimitiveType.Void);
            var property = ((Class) method.Namespace).Properties.Find(
                p => p.GetMethod == method || p.SetMethod == method);
            bool isSetter = property != null && property.SetMethod == method;
            var hasReturn = !isVoid && !isSetter;

            if (hasReturn)
                Write($"var {Helpers.ReturnIdentifier} = ");

            Write($"{Helpers.TargetIdentifier}.");
            string marshalsCode = string.Join(", ", marshals);
            if (property == null)
            {
                Write($"{method.Name}({marshalsCode})");
            }
            else
            {
                Write($"{property.Name}");
                if (isSetter)
                    Write($" = {marshalsCode}");
            }
            WriteLine(";");

            if (hasReturn)
            {
                var param = new Parameter
                {
                    Name = Helpers.ReturnIdentifier,
                    QualifiedType = method.OriginalReturnType,
                    Namespace = method
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
                    WriteLine("*({0}*) {1} = {2};",
                        TypePrinter.PrintNative(method.OriginalReturnType),
                        retParam.Name, marshal.Context.Return);
                }
                else
                {
                    WriteLine("return {0};", marshal.Context.Return);
                }
            }

            if (!isVoid && isSetter)
                WriteLine("return false;");

            for (var i = 0; i < numBlocks; ++i)
                WriteCloseBraceIndent();
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
            TypePrinter.PushMarshalKind(MarshalKind.GenericDelegate);
            var @params = GatherInternalParams(method, out retType);

            var vTableMethodDelegateName = GetVTableMethodDelegateName(method);
            TypePrinter.PopMarshalKind();

            WriteLine("private static {0} {1}Instance;",
                method.FunctionType.ToString(),
                vTableMethodDelegateName);
            NewLine();

            WriteLine("private static {0} {1}Hook({2})", retType, vTableMethodDelegateName,
                string.Join(", ", @params));
            WriteStartBraceIndent();

            WriteLine("if (!NativeToManagedMap.ContainsKey(instance))");
            WriteLineIndent("throw new global::System.Exception(\"No managed instance was found\");");
            NewLine();

            var printedClass = @class.Visit(TypePrinter);
            WriteLine($"var {Helpers.TargetIdentifier} = ({printedClass}) NativeToManagedMap[instance];");
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
                var @interface = @base.Namespace.Classes.FirstOrDefault(
                    c => c.IsInterface && c.OriginalClass == @base);

                WriteLine("if ({0} == IntPtr.Zero)", Helpers.InstanceIdentifier);
                WriteLineIndent("return;");

                // The local var must be of the exact type in the object map because of TryRemove
                var printedClass = (@interface ?? (
                    @base.IsAbstractImpl ? @base.BaseClass : @base)).Visit(TypePrinter);
                WriteLine($"{printedClass} {Helpers.DummyIdentifier};");
                WriteLine("NativeToManagedMap.TryRemove({0}, out {1});",
                    Helpers.InstanceIdentifier, Helpers.DummyIdentifier);
                var classInternal = TypePrinter.PrintNative(@class);
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
                    if (@class.IsDependent || dtor.IsVirtual)
                        WriteStartBraceIndent();
                    else
                        PushIndent();
                    if (dtor.IsVirtual)
                        this.GenerateMember(@class, c => GenerateDestructorCall(
                            c is ClassTemplateSpecialization ?
                                c.Methods.First(m => m.InstantiatedFrom == dtor) : dtor), true);
                    else
                        this.GenerateMember(@class, c => GenerateMethodBody(c, dtor), true);
                    if (@class.IsDependent || dtor.IsVirtual)
                        WriteCloseBraceIndent();
                    else
                        PopIndent();
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

            if (!@class.IsAbstractImpl)
            {
                PushBlock(BlockKind.Method);
                var printedClass = @class.Visit(TypePrinter);
                WriteLine("internal static {0}{1} {2}(global::System.IntPtr native, bool skipVTables = false)",
                    @class.NeedsBase && !@class.BaseClass.IsInterface ? "new " : string.Empty,
                    printedClass, Helpers.CreateInstanceIdentifier);
                WriteStartBraceIndent();
                var suffix = @class.IsAbstract ? "Internal" : string.Empty;
                var ctorCall = $"{printedClass}{suffix}";
                WriteLine("return new {0}(native.ToPointer(), skipVTables);", ctorCall);
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            this.GenerateNativeConstructorsByValue(@class);

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
                WriteLine($"{Helpers.InstanceField} = *({TypePrinter.PrintNative(@class)}*) native;");
            }

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateNativeConstructorByValue(Class @class, string returnType)
        {
            var @internal = TypePrinter.PrintNative(@class.IsAbstractImpl ? @class.BaseClass : @class);

            if (!@class.IsAbstractImpl)
            {
                PushBlock(BlockKind.Method);
                WriteLine("internal static {0} {1}({2} native, bool skipVTables = false)",
                    returnType, Helpers.CreateInstanceIdentifier, @internal);
                WriteStartBraceIndent();
                var suffix = @class.IsAbstract ? "Internal" : "";
                WriteLine($"return new {returnType}{suffix}(native, skipVTables);");
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
                    var printed = TypePrinter.PrintNative(@class);
                    WriteLine($"{printed}.{GetFunctionNativeIdentifier(copyCtorMethod)}(ret, new global::System.IntPtr(&native));",
                        printed, GetFunctionNativeIdentifier(copyCtorMethod));
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
            {
                var printedType = method.OriginalReturnType.Visit(TypePrinter);
                Write($"{functionName} {printedType}(");
            }
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
                Write(Helpers.GetAccess(method.Access));
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

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload ||
                method.SynthKind == FunctionSynthKind.ComplementOperator)
            {
                GenerateMethodBody(@class, method);
            }
            else
            {
                var isVoid = method.OriginalReturnType.Type.Desugar().IsPrimitiveType(PrimitiveType.Void) ||
                    method.IsConstructor;
                this.GenerateMember(@class, c => GenerateMethodBody(
                    c, method, method.OriginalReturnType), isVoid);
            }

            SkipImpl:

            WriteCloseBraceIndent();

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                GenerateEqualsAndGetHashCode(method, @class);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateMethodBody(Class @class, Method method,
            QualifiedType returnType = default(QualifiedType))
        {
            var specialization = @class as ClassTemplateSpecialization;
            if (specialization != null)
            {
                var specializedMethod = @class.Methods.FirstOrDefault(
                    m => m.InstantiatedFrom == method);
                if (specializedMethod != null)
                    method = specializedMethod;
                else
                {
                    WriteLine($@"throw new MissingMethodException(""Method {
                        method.Name} missing from explicit specialization {
                        @class.Visit(TypePrinter)}."");");
                    return;
                }
            }
            if (@class.IsRefType)
            {
                if (method.IsConstructor)
                {
                    GenerateClassConstructor(method, @class);
                }
                else if (method.IsOperator)
                {
                    GenerateOperator(method, returnType);
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
                    GenerateInternalFunctionCall(method, returnType: returnType);
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
                    GenerateOperator(method, returnType);
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
                var printedClass = @class.Visit(TypePrinter);
                if (@class.IsRefType)
                {
                    WriteLine($"return this == obj as {printedClass};");
                }
                else
                {
                    WriteLine($"if (!(obj is {printedClass})) return false;");
                    WriteLine($"return this == ({printedClass}) obj;");
                }
                WriteCloseBraceIndent();

                NewLine();

                WriteLine("public override int GetHashCode()");
                WriteStartBraceIndent();
                if (@class.IsRefType)
                    this.GenerateMember(@class, GenerateGetHashCode);
                else
                    WriteLine($"return {Helpers.InstanceIdentifier}.GetHashCode();");
                WriteCloseBraceIndent();
            }
        }

        private void GenerateGetHashCode(Class @class)
        {
            WriteLine($"if ({Helpers.InstanceIdentifier} == global::System.IntPtr.Zero)");
            WriteLineIndent("return global::System.IntPtr.Zero.GetHashCode();");
            WriteLine($@"return (*({TypePrinter.PrintNative(@class)}*) {
                Helpers.InstanceIdentifier}).GetHashCode();");
        }

        private void GenerateVirtualPropertyCall(Method method, Class @class,
            Property property, List<Parameter> parameters = null,
            QualifiedType returnType = default(QualifiedType))
        {
            if (property.IsOverride && !property.IsPure &&
                method.SynthKind != FunctionSynthKind.AbstractImplCall &&
                @class.HasNonAbstractBasePropertyInPrimaryBase(property))
                WriteLine(parameters == null ?
                    "return base.{0};" : "base.{0} = value;", property.Name);
            else
                GenerateFunctionCall(GetVirtualCallDelegate(method, @class),
                    parameters ?? method.Parameters, method, returnType);
        }

        private void GenerateVirtualFunctionCall(Method method, Class @class,
            bool forceVirtualCall = false)
        {
            if (!forceVirtualCall && method.IsGeneratedOverride() &&
                !method.BaseMethod.IsPure)
                GenerateManagedCall(method, true);
            else
                GenerateFunctionCall(GetVirtualCallDelegate(method, @class),
                    method.Parameters, method);
        }

        private string GetVirtualCallDelegate(Method method, Class @class)
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
            var delegateId = Generator.GeneratedIdentifier(@delegate);

            WriteLine("var {0} = ({1}) Marshal.GetDelegateForFunctionPointer(new IntPtr({2}), typeof({1}));",
                delegateId, method.FunctionType.ToString(),
                Helpers.SlotIdentifier);

            return delegateId;
        }

        private void GenerateOperator(Method method, QualifiedType returnType)
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
                    var printedType = method.ConversionType.Visit(TypePrinter);
                    if (@interface != null)
                    {
                        var printedInterface = @interface.Visit(TypePrinter);
                        WriteLine($"return new {printedType}(({printedInterface}) {paramName});");
                    }
                    else
                        WriteLine($"return new {printedType}({paramName});");
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

            GenerateInternalFunctionCall(method, returnType: returnType);
        }

        private void GenerateClassConstructor(Method method, Class @class)
        {
            var @internal = TypePrinter.PrintNative(
                @class.IsAbstractImpl ? @class.BaseClass : @class);
            WriteLine($"{Helpers.InstanceIdentifier} = Marshal.AllocHGlobal(sizeof({@internal}));");
            WriteLine($"{Helpers.OwnsNativeInstanceIdentifier} = true;");
            WriteLine($"NativeToManagedMap[{Helpers.InstanceIdentifier}] = this;");

            if (method.IsCopyConstructor)
            {
                if (@class.HasNonTrivialCopyConstructor)
                    GenerateInternalFunctionCall(method);
                else
                {
                    var classInternal = TypePrinter.PrintNative(@class);
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
            List<Parameter> parameters = null,
            QualifiedType returnType = default(QualifiedType))
        {
            if (parameters == null)
                parameters = function.Parameters;

            var @class = function.Namespace as Class;

            string @internal = Helpers.InternalStruct;
            if (@class is ClassTemplateSpecialization)
                @internal = TypePrinter.PrintNative(@class).Type;

            var nativeFunction = GetFunctionNativeIdentifier(function);
            var functionName = $"{@internal}.{nativeFunction}";
            GenerateFunctionCall(functionName, parameters, function, returnType);
        }

        public void GenerateFunctionCall(string functionName, List<Parameter> parameters,
            Function function, QualifiedType returnType = default(QualifiedType))
        {
            if (function.IsPure)
            {
                WriteLine("throw new System.NotImplementedException();");
                return;
            }

            var retType = function.OriginalReturnType;
            if (returnType.Type == null)
                returnType = retType;

            var method = function as Method;
            var hasThisReturnStructor = method != null && (method.IsConstructor || method.IsDestructor);
            var needsReturn = !returnType.Type.IsPrimitiveType(PrimitiveType.Void) && !hasThisReturnStructor;

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
                    WriteLine($@"var {Helpers.ReturnIdentifier} = new {
                        TypePrinter.PrintNative(@class)}();");
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

            if (method != null && !method.IsConstructor && method.OriginalFunction != null &&
                ((Method) method.OriginalFunction).IsConstructor)
            {
                WriteLine($@"Marshal.AllocHGlobal({
                    ((Class) method.OriginalNamespace).Layout.Size});");
                names.Insert(0, Helpers.ReturnIdentifier);
            }
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
                    ReturnType = returnType,
                    Parameter = operatorParam,
                    Function = function
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                retType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                if (ctx.HasCodeBlock)
                    PushIndent();

                WriteLine($"return {marshal.Context.Return};");

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
                param.QualifiedType.Visit(marshal);

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
            param.QualifiedType.Visit(marshal);
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

            var functionType = typedef.Type as FunctionType;

            if (functionType != null || typedef.Type.IsPointerTo(out functionType))
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

                if (functionType.ReturnType.Type.Desugar().IsPrimitiveType(PrimitiveType.Bool))
                    WriteLine("[return: MarshalAs(UnmanagedType.I1)]");

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

            if (@enum.BuiltinType.Type != PrimitiveType.Int &&
                @enum.BuiltinType.Type != PrimitiveType.Null)
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

        public string GetFunctionNativeIdentifier(Function function,
            bool isForDelegate = false)
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
            if (specialization != null && !isForDelegate)
                identifier.Append(Helpers.GetSuffixFor(specialization));

            var internalParams = function.GatherInternalParams(
                Context.ParserOptions.IsItaniumLikeAbi);
            var overloads = function.Namespace.GetOverloads(function)
                .Where(f => !f.Ignore && (isForDelegate || internalParams.SequenceEqual(
                    f.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi),
                    new MarshallingParamComparer()))).ToList();
            var index = -1;
            if (overloads.Count > 1)
                index = overloads.IndexOf(function);

            if (index > 0)
            {
                identifier.Append('_');
                identifier.Append(index.ToString(CultureInfo.InvariantCulture));
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

        private class MarshallingParamComparer : IEqualityComparer<Parameter>
        {
            public bool Equals(Parameter x, Parameter y) =>
                IsIntPtr(x) == IsIntPtr(y) || x.Type.Equals(y.Type);

            private static bool IsIntPtr(Parameter p)
            {
                var type = p.Type.Desugar();
                return p.Kind == ParameterKind.IndirectReturnType
                    || (type.IsAddress()
                    && (!type.GetPointee().Desugar().IsPrimitiveType()
                    || type.GetPointee().Desugar().IsPrimitiveType(PrimitiveType.Void)));
            }

            public int GetHashCode(Parameter obj)
            {
                return obj.Type.Desugar().GetHashCode();
            }
        }
    }

    internal class SymbolNotFoundException : Exception
    {
        public SymbolNotFoundException(string msg) : base(msg)
        {}
    }
}