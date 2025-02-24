using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
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

        internal Dictionary<string, CSharpLibrarySymbolTable> LibrarySymbolTables { get; } = new();

        public override string FileExtension => "cs";

        public CSharpSources(BindingContext context)
            : base(context)
        {
            Init();
        }

        public CSharpSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            Init();
        }

        private void Init()
        {
            TypePrinter = new CSharpTypePrinter(Context);
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

        public static string SafeIdentifier(string id)
        {
            if (id.All(char.IsLetterOrDigit))
                return ReservedKeywords.Contains(id) ? "@" + id : id;

            return new string((from c in id
                               where c != '$'
                               select char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }

        #endregion

        public Module Module => TranslationUnits.Count == 0 ?
                Context.Options.SystemModule : TranslationUnit.Module;

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            GenerateUsings();

            WriteLine("#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required");
            WriteLine("#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference");
            NewLine();

            if (!string.IsNullOrEmpty(Module.OutputNamespace))
            {
                PushBlock(BlockKind.Namespace);
                WriteLine("namespace {0}", Module.OutputNamespace);
                WriteOpenBraceAndIndent();
            }

            foreach (var unit in TranslationUnits)
                unit.Visit(this);

            if (!string.IsNullOrEmpty(Module.OutputNamespace))
            {
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            foreach (var lib in LibrarySymbolTables)
                WriteLine(lib.Value.Generate());

            GenerateExternalClassTemplateSpecializations();
        }

        private void GenerateExternalClassTemplateSpecializations()
        {
            foreach (var group in from spec in Module.ExternalClassTemplateSpecializations
                                  let module = spec.TranslationUnit.Module
                                  group spec by module.OutputNamespace into @group
                                  select @group)
            {
                using (!string.IsNullOrEmpty(group.Key)
                    ? PushWriteBlock(BlockKind.Namespace, $"namespace {group.Key}", NewLineKind.BeforeNextBlock)
                    : default)
                {
                    foreach (var template in from s in @group
                                             group s by s.TemplatedDecl.TemplatedClass into template
                                             select template)
                    {
                        var declContext = template.Key.Namespace;
                        var declarationContexts = new Stack<DeclarationContext>();
                        while (!(declContext is TranslationUnit))
                        {
                            if (!(declContext is Namespace @namespace) || !@namespace.IsInline)
                                declarationContexts.Push(declContext);
                            declContext = declContext.Namespace;
                        }

                        foreach (var declarationContext in declarationContexts)
                        {
                            WriteLine($"namespace {declarationContext.Name}");
                            WriteOpenBraceAndIndent();
                        }

                        GenerateClassTemplateSpecializationsInternals(
                            template.Key, template.ToList());

                        foreach (var declarationContext in declarationContexts)
                            UnindentAndWriteCloseBrace();
                    }
                }
            }

            if (Options.GenerationOutputMode == GenerationOutputMode.FilePerUnit)
                Module.ExternalClassTemplateSpecializations.Clear();
        }

        public virtual void GenerateUsings()
        {
            PushBlock(BlockKind.Usings);
            var requiredNameSpaces = new List<string> {
                "System",
                "System.Runtime.InteropServices",
                "System.Security",
            };

            var internalsVisibleTo = (from m in Options.Modules
                                      where m.Dependencies.Contains(Module)
                                      select m.LibraryName).ToList();

            if (internalsVisibleTo.Any())
                requiredNameSpaces.Add("System.Runtime.CompilerServices");

            foreach (var @namespace in requiredNameSpaces.Union(Options.DependentNameSpaces).OrderBy(x => x))
                WriteLine($"using {@namespace};");

            WriteLine("using __CallingConvention = global::System.Runtime.InteropServices.CallingConvention;");
            WriteLine("using __IntPtr = global::System.IntPtr;");

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

            using var _ = shouldGenerateNamespace
                ? PushWriteBlock(BlockKind.Namespace, $"namespace {context.Name}", NewLineKind.BeforeNextBlock)
                : default;

            return base.VisitNamespace(@namespace);
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

            foreach (var childNamespace in context.Namespaces)
                childNamespace.Visit(this);

            return true;
        }

        private IEnumerable<Class> EnumerateClasses()
        {
            foreach (var tu in TranslationUnits)
            {
                foreach (var cls in EnumerateClasses(tu))
                    yield return cls;
            }
        }

        private IEnumerable<Class> EnumerateClasses(DeclarationContext context)
        {
            foreach (var cls in context.Classes)
                yield return cls;

            foreach (var ns in context.Namespaces)
            {
                foreach (var cls in EnumerateClasses(ns))
                    yield return cls;
            }
        }

        public virtual void GenerateNamespaceFunctionsAndVariables(DeclarationContext context)
        {
            var hasGlobalVariables = !(context is Class) && context.Variables.Any(
                v => v.IsGenerated && v.Access == AccessSpecifier.Public);

            if (!context.Functions.Any(f => f.IsGenerated) && !hasGlobalVariables)
                return;

            var parentName = SafeIdentifier(Context.Options.GenerateFreeStandingFunctionsClassName(context.TranslationUnit));
            var isStruct = EnumerateClasses()
                .ToList()
                .FindAll(cls => cls.IsValueType && cls.Name == parentName && context.QualifiedLogicalName == cls.Namespace.QualifiedLogicalName)
                .Any();

            using (PushWriteBlock(BlockKind.Functions, $"public unsafe partial {(isStruct ? "struct" : "class")} {parentName}", NewLineKind.BeforeNextBlock))
            {
                using (PushWriteBlock(BlockKind.InternalsClass, GetClassInternalHead(new Class { Name = parentName }), NewLineKind.BeforeNextBlock))
                {
                    // Generate all the internal function declarations.
                    foreach (var function in context.Functions)
                    {
                        if ((!function.IsGenerated && !function.IsInternal) || function.IsSynthesized)
                            continue;

                        GenerateInternalFunction(function);
                    }
                }

                foreach (var function in context.Functions)
                {
                    if (!function.IsGenerated) continue;

                    GenerateFunction(function, parentName);
                }

                foreach (var variable in context.Variables.Where(
                    v => v.IsGenerated && v.Access == AccessSpecifier.Public))
                    GenerateVariable(null, variable);
            }
        }

        private void GenerateClassTemplateSpecializationInternal(Class classTemplate)
        {
            if (classTemplate.Specializations.Count == 0)
                return;

            GenerateClassTemplateSpecializationsInternals(classTemplate,
                classTemplate.Specializations);
        }

        private void GenerateClassTemplateSpecializationsInternals(Class template,
            IList<ClassTemplateSpecialization> specializations)
        {
            var namespaceName = string.Format("namespace {0}{1}",
                template.OriginalNamespace is Class &&
                !template.OriginalNamespace.IsDependent ?
                    template.OriginalNamespace.Name + '_' : string.Empty,
                template.Name);

            using (PushWriteBlock(BlockKind.Namespace, namespaceName, NewLineKind.BeforeNextBlock))
            {
                var generated = GetGeneratedClasses(template, specializations);

                foreach (var nestedTemplate in template.Classes.Where(
                    c => c.IsDependent && !c.Ignore && c.Specializations.Any(s => !s.Ignore)))
                    GenerateClassTemplateSpecializationsInternals(
                        nestedTemplate, nestedTemplate.Specializations);

                if (template.HasDependentValueFieldInLayout(specializations) ||
                    template.Specializations.Intersect(specializations).Count() == specializations.Count)
                    foreach (var specialization in generated)
                        GenerateClassInternals(specialization);

                foreach (var group in specializations.SelectMany(s => s.Classes).Where(
                    c => !c.IsIncomplete).GroupBy(c => c.Name))
                {
                    var nested = template.Classes.FirstOrDefault(c => c.Name == group.Key);
                    if (nested != null)
                        GenerateNestedInternals(group.Key, GetGeneratedClasses(nested, group));
                }
            }
        }

        private void GenerateNestedInternals(string name, IEnumerable<Class> nestedClasses)
        {
            using (WriteBlock($"namespace {name}"))
            {
                foreach (var nestedClass in nestedClasses)
                {
                    GenerateClassInternals(nestedClass);
                    foreach (var nestedInNested in nestedClass.Classes)
                        GenerateNestedInternals(nestedInNested.Name, new[] { nestedInNested });
                }
            }
            NewLine();
        }

        private IEnumerable<Class> GetGeneratedClasses(
            Class dependentClass, IEnumerable<Class> specializedClasses)
        {
            if (dependentClass.HasDependentValueFieldInLayout())
                return specializedClasses.KeepSingleAllPointersSpecialization();

            return new[] {
                specializedClasses.FirstOrDefault(
                    s => s.IsGenerated && s.Classes.All(c => !c.IsIncomplete)) ??
                specializedClasses.FirstOrDefault(s => s.IsGenerated) ??
                specializedClasses.First()};
        }

        public override void GenerateDeclarationCommon(Declaration decl)
        {
            base.GenerateDeclarationCommon(decl);

            foreach (Attribute attribute in decl.Attributes)
                WriteLine("[global::{0}({1})]", attribute.Type.FullName, attribute.Value);
        }

        #region Classes

        public override bool VisitClassDecl(Class @class)
        {
            if ((@class.IsIncomplete && !@class.IsOpaque) || @class.Ignore)
                return false;

            if (@class.IsInterface)
            {
                GenerateInterface(@class);
                return true;
            }

            if (!@class.IsDependent && !@class.IsAbstractImpl)
                foreach (var nestedTemplate in @class.Classes.Where(
                    c => !c.IsIncomplete && c.IsDependent))
                    GenerateClassTemplateSpecializationInternal(nestedTemplate);

            if (@class.IsTemplate && !@class.IsAbstractImpl)
            {
                if (!(@class.Namespace is Class))
                    GenerateClassTemplateSpecializationInternal(@class);

                if (@class.Specializations.All(s => !s.IsGenerated))
                    return true;
            }

            if (@class.IsDependent && !@class.IsGenerated)
                return true;

            // disable the type maps, if any, for this class because of copy ctors, operators and others
            this.DisableTypeMap(@class);

            PushBlock(BlockKind.Class);
            GenerateDeclarationCommon(@class);
            GenerateClassSpecifier(@class);

            NewLine();
            WriteOpenBraceAndIndent();

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
                    WriteLine($"private {@class.Name}.{Helpers.InternalStruct} {Helpers.InstanceField};");
                    WriteLine($"internal ref {@class.Name}.{Helpers.InternalStruct} {Helpers.InstanceIdentifier} => ref {Helpers.InstanceField};");
                }
                else
                {
                    WriteLine($"public {TypePrinter.IntPtrType} {Helpers.InstanceIdentifier} {{ get; protected set; }}");
                    if (@class.Layout.HasSubclassAtNonZeroOffset)
                        WriteLine($"protected int {Helpers.PrimaryBaseOffsetIdentifier};");
                }
                PopBlock(NewLineKind.BeforeNextBlock);

                if (!@class.IsValueType)
                {
                    PushBlock(BlockKind.Method);
                    if (Options.GenerateNativeToManagedFor(@class))
                        GenerateNativeToManaged(@class);
                    PopBlock(NewLineKind.BeforeNextBlock);
                }
            }

            // Add booleans to track who owns unmanaged memory for string fields
            foreach (var prop in @class.GetConstCharFieldProperties())
            {
                WriteLine($"private bool __{prop.Field.OriginalName}_OwnsNativeMemory = false;");
            }

            GenerateClassConstructors(@class);

            GenerateClassMethods(@class.Methods);
            GenerateClassVariables(@class);
            GenerateClassProperties(@class);

            if (@class.IsDynamic)
                GenerateVTable(@class);
            exit:
            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);

            foreach (var typeMap in Context.TypeMaps.TypeMaps.Values)
                typeMap.IsEnabled = true;

            return true;
        }

        public void GenerateNativeToManaged(Class @class)
        {
            // use interfaces if any - derived types with a secondary base this class must be compatible with the map
            var @interface = @class.Namespace.Classes.FirstOrDefault(c => c.IsInterface && c.OriginalClass == @class);
            var printedClass = (@interface ?? @class).Visit(TypePrinter);

            // Create a method to record/recover a mapping to make it easier to call from template instantiation
            // implementations. If finalizers are in play, use weak references; otherwise, the finalizer
            // will never get invoked: unless the managed object is Disposed, there will always be a reference
            // in the dictionary.

            bool generateFinalizer = Options.GenerateFinalizerFor(@class);
            var type = generateFinalizer ? $"global::System.WeakReference<{printedClass}>" : $"{printedClass}";

            WriteLines($@"
internal static readonly new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, {type}> NativeToManagedMap =
    new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, {type}>();

internal static void {Helpers.RecordNativeToManagedMappingIdentifier}(IntPtr native, {printedClass} managed)
{{
    NativeToManagedMap[native] = {(generateFinalizer ? $"new {type}(managed)" : "managed")};
}}

internal static bool {Helpers.TryGetNativeToManagedMappingIdentifier}(IntPtr native, out {printedClass} managed)
{{
    {(generateFinalizer
    ? @"
    managed = default;
    return NativeToManagedMap.TryGetValue(native, out var wr) && wr.TryGetTarget(out managed);"
    : @"
    return NativeToManagedMap.TryGetValue(native, out managed);")}
}}");
        }

        private void GenerateInterface(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete)
                return;

            PushBlock(BlockKind.Interface);
            GenerateDeclarationCommon(@class);

            GenerateClassSpecifier(@class);

            var shouldInheritFromIDisposable = !@class.HasBase;
            if (shouldInheritFromIDisposable)
                Write(" : IDisposable");

            NewLine();
            WriteOpenBraceAndIndent();

            foreach (var method in @class.Methods.Where(m =>
                (m.OriginalFunction == null ||
                 !ASTUtils.CheckIgnoreFunction(m.OriginalFunction)) &&
                m.Access == AccessSpecifier.Public &&
                (!shouldInheritFromIDisposable || !IsDisposeMethod(m))))
            {
                PushBlock(BlockKind.Method);
                GenerateDeclarationCommon(method);

                var functionName = GetMethodIdentifier(method);

                Write($"{method.OriginalReturnType} {functionName}(");

                Write(FormatMethodParameters(method.Parameters));

                WriteLine(");");

                PopBlock(NewLineKind.BeforeNextBlock);
            }
            foreach (var prop in @class.Properties.Where(p => p.IsGenerated &&
                (p.GetMethod == null || p.GetMethod.OriginalFunction == null ||
                 !p.GetMethod.OriginalFunction.Ignore) &&
                (p.SetMethod == null || p.SetMethod.OriginalFunction == null ||
                 !p.SetMethod.OriginalFunction.Ignore) &&
                p.Access == AccessSpecifier.Public))
            {
                PushBlock(BlockKind.Property);
                var type = prop.Type;
                if (prop.Parameters.Count > 0 && prop.Type.IsPointerToPrimitiveType())
                    type = ((PointerType)prop.Type).Pointee;
                GenerateDeclarationCommon(prop);
                Write($"{type} {GetPropertyName(prop)} {{ ");
                if (prop.HasGetter)
                    Write("get; ");
                if (prop.HasSetter)
                    Write("set; ");

                WriteLine("}");
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public static bool IsDisposeMethod(Method method)
        {
            return method.Name == "Dispose" && method.Parameters.Count == 0 && method.ReturnType.Type.Desugar().IsPrimitiveType(PrimitiveType.Void);
        }

        public void GenerateClassInternals(Class @class)
        {
            var sequentialLayout = Options.GenerateSequentialLayout && CanUseSequentialLayout(@class);

            if (@class.Layout.Size > 0)
            {
                var layout = sequentialLayout ? "Sequential" : "Explicit";
                var pack = @class.MaxFieldAlignment > 0 ? $", Pack = {@class.MaxFieldAlignment}" : string.Empty;
                WriteLine($"[StructLayout(LayoutKind.{layout}, Size = {@class.Layout.Size}{pack})]");
            }

            using (PushWriteBlock(BlockKind.InternalsClass, GetClassInternalHead(@class), NewLineKind.BeforeNextBlock))
            {
                TypePrinter.PushContext(TypePrinterContextKind.Native);

                GenerateClassInternalsFields(@class, sequentialLayout);

                if (@class.IsGenerated)
                {
                    var functions = GatherClassInternalFunctions(@class);

                    foreach (var function in functions)
                        GenerateInternalFunction(function);
                }

                TypePrinter.PopContext();
            }
        }

        private IEnumerable<Function> GatherClassInternalFunctions(Class @class,
            bool includeCtors = true)
        {
            var functions = new List<Function>();
            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    functions.AddRange(GatherClassInternalFunctions(@base.Class, false));

            var currentSpecialization = @class as ClassTemplateSpecialization;
            if (currentSpecialization != null)
            {
                Class template = currentSpecialization.TemplatedDecl.TemplatedClass;
                IEnumerable<ClassTemplateSpecialization> specializations = null;
                if (template.GetSpecializedClassesToGenerate().Count() == 1)
                    specializations = template.Specializations.Where(s => s.IsGenerated);
                else
                {
                    Func<TemplateArgument, bool> allPointers = (TemplateArgument a) =>
                        a.Type.Type?.Desugar().IsAddress() == true;
                    if (currentSpecialization.Arguments.All(allPointers))
                    {
                        specializations = template.Specializations.Where(
                            s => s.IsGenerated && s.Arguments.All(allPointers));
                    }
                }

                if (specializations != null)
                {
                    foreach (var specialization in specializations)
                        GatherClassInternalFunctions(specialization, includeCtors, functions);
                    return functions;
                }
            }

            GatherClassInternalFunctions(@class, includeCtors, functions);

            return functions;
        }

        private void GatherClassInternalFunctions(Class @class, bool includeCtors,
            List<Function> functions)
        {
            Action<Method> tryAddOverload = method =>
            {
                if (method.IsSynthesized)
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
                foreach (var dtor in @class.Destructors.Where(d => !d.IsVirtual))
                    tryAddOverload(dtor);

            foreach (var method in @class.Methods)
            {
                if (!method.IsGenerated || ASTUtils.CheckIgnoreMethod(method) ||
                    (!method.NeedsSymbol() && !method.IsOperator))
                    continue;

                if (method.IsConstructor)
                    continue;

                tryAddOverload(method);
            }

            foreach (var prop in @class.Properties.Where(p => p.Field == null))
            {
                if ((!prop.IsOverride || prop.GetMethod.Namespace == @class) &&
                    !functions.Contains(prop.GetMethod) && !prop.GetMethod.Ignore)
                    tryAddOverload(prop.GetMethod);

                if (prop.SetMethod != null &&
                    (!prop.IsOverride || prop.SetMethod.Namespace == @class) &&
                    !functions.Contains(prop.SetMethod) &&
                    !prop.GetMethod.Ignore)
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

        private string GetClassInternalHead(Class @class)
        {
            bool isSpecialization = false;
            DeclarationContext declContext = @class;
            while (declContext != null)
            {
                isSpecialization = declContext is ClassTemplateSpecialization;
                if (isSpecialization)
                    break;
                declContext = declContext.Namespace;
            }
            var @new = @class != null && @class.NeedsBase &&
                !@class.BaseClass.IsInterface && !(@class.BaseClass is ClassTemplateSpecialization) && !isSpecialization;

            return $@"public {(@new ? "new " : "")}{(isSpecialization ? "unsafe " : string.Empty)}partial struct {Helpers.InternalStruct}{Helpers.GetSuffixForInternal(@class)}";
        }

        public static bool ShouldGenerateClassNativeField(Class @class)
        {
            if (@class.IsStatic)
                return false;
            return @class.IsValueType || !@class.HasBase || !@class.HasRefBase();
        }

        public virtual string GetBaseClassTypeName(BaseClassSpecifier @base)
        {
            this.DisableTypeMap(@base.Class);

            var typeName = @base.Type.Desugar().Visit(TypePrinter);

            foreach (var typeMap in Context.TypeMaps.TypeMaps.Values)
                typeMap.IsEnabled = true;

            return typeName;
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            // private classes must be visible to because the internal structs can be used in dependencies
            // the proper fix is InternalsVisibleTo
            var keywords = new List<string>();

            keywords.Add(@class.Access == AccessSpecifier.Protected ? "protected internal" : "public");

            var isBindingGen = this.GetType() == typeof(CSharpSources);
            if (isBindingGen)
                keywords.Add("unsafe");

            if (@class.IsAbstract)
                keywords.Add("abstract");

            if (@class.IsStatic)
                keywords.Add("static");

            // This token needs to directly precede the "class" token.
            keywords.Add("partial");

            keywords.Add(@class.IsInterface ? "interface" : (@class.IsValueType ? "struct" : "class"));
            keywords.Add(@class.Name);

            Write(string.Join(" ", keywords));
            if (@class.IsDependent && @class.TemplateParameters.Any())
                Write($"<{string.Join(", ", @class.TemplateParameters.Select(p => p.Name))}>");

            var bases = new List<string>();

            if (@class.NeedsBase)
            {
                foreach (var @base in @class.Bases.Where(b => b.IsGenerated &&
                    b.IsClass && b.Class.IsGenerated))
                {
                    var printedBase = GetBaseClassTypeName(@base);
                    bases.Add(printedBase);
                }
            }

            if (@class.IsGenerated && isBindingGen && NeedsDispose(@class) && !@class.IsOpaque)
            {
                bases.Add("IDisposable");
            }

            if (bases.Count > 0 && !@class.IsStatic)
                Write(" : {0}", string.Join(", ", bases));
        }

        private bool NeedsDispose(Class @class)
        {
            return @class.IsRefType || @class.IsValueType &&
                (@class.GetConstCharFieldProperties().Any() || @class.HasNonTrivialDestructor);
        }

        private bool CanUseSequentialLayout(Class @class)
        {
            if (@class.IsUnion || @class.HasUnionFields)
                return false;

            foreach (var field in @class.Fields)
            {
                if (field.AlignAs != 0)
                {
                    // https://github.com/dotnet/runtime/issues/22990
                    return false;
                }
            }

            var fields = @class.Layout.Fields;

            if (fields.Count > 1)
            {
                for (var i = 1; i < fields.Count; ++i)
                {
                    if (fields[i].Offset == fields[i - 1].Offset)
                        return false;

                    var type = fields[i].QualifiedType.Type.Desugar();

                    if (type.TryGetDeclaration(out Declaration declaration) && declaration.AlignAs != 0)
                    {
                        // https://github.com/dotnet/runtime/issues/9089
                        return false;
                    }
                }
            }

            return true;
        }

        private void GenerateClassInternalsFields(Class @class, bool sequentialLayout)
        {
            var fields = @class.Layout.Fields;

            for (var i = 0; i < fields.Count; ++i)
            {
                var field = fields[i];

                TypePrinterResult retType = TypePrinter.VisitFieldDecl(
                    new Field { Name = field.Name, QualifiedType = field.QualifiedType });

                PushBlock(BlockKind.Field);

                if (sequentialLayout && i > 0)
                {
                    var padding = field.Offset - field.CalculateOffset(fields[i - 1], Context.TargetInfo);

                    if (padding > 1)
                        WriteLine($"internal fixed byte {field.Name}Padding[{padding}];");
                    else if (padding > 0)
                        WriteLine($"internal byte {field.Name}Padding;");
                }

                if (!sequentialLayout)
                    WriteLine($"[FieldOffset({field.Offset})]");

                Write($"internal {retType}");
                if (field.Expression != null)
                {
                    var fieldValuePrinted = field.Expression.CSharpValue(ExpressionPrinter);
                    Write($" = {fieldValuePrinted}");
                }
                WriteLine(";");

                PopBlock(sequentialLayout && i + 1 != fields.Count ? NewLineKind.Never : NewLineKind.BeforeNextBlock);
            }
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
                    WriteLine(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteOpenBraceAndIndent();

                this.GenerateMember(@class, c => GenerateFunctionSetter(c, property));
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteOpenBraceAndIndent();
                var var = decl as Variable;
                this.GenerateMember(@class, c => GenerateVariableSetter(
                    c is ClassTemplateSpecialization ?
                        c.Variables.First(v => v.Name == decl.Name) : var));
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapSetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteOpenBraceAndIndent();

                this.GenerateField(@class, field, GenerateFieldSetter, true);
            }
            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private bool GenerateVariableSetter(Variable var)
        {
            string ptr = GeneratePointerTo(var);

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = var.QualifiedType
            };

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                Parameter = param,
                ArgName = param.Name,
                ReturnType = var.QualifiedType
            };
            ctx.PushMarshalKind(MarshalKind.Variable);

            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            var.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                Indent();

            WriteLine($@"*{ptr} = {marshal.Context.ArgumentPrefix}{marshal.Context.Return};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();

            return true;
        }

        private string GeneratePointerTo(Variable var)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);

            var libraryPath = GetLibraryOf(var);

            if (!LibrarySymbolTables.TryGetValue(libraryPath, out var lib))
                LibrarySymbolTables[libraryPath] = lib = new CSharpLibrarySymbolTable(libraryPath, Module.OutputNamespace);

            var location = lib.GetFullVariablePath(var.Mangled);

            var arrayType = var.Type as ArrayType;
            string ptr = Generator.GeneratedIdentifier("ptr");
            if (arrayType != null)
            {
                if (Context.Options.MarshalConstCharArrayAsString && arrayType.Type.IsPrimitiveType(PrimitiveType.Char) && arrayType.SizeType != ArrayType.ArraySize.Constant)
                    WriteLine($"var {ptr} = {location};");
                else
                    WriteLine($"var {ptr} = ({arrayType.Type.Visit(TypePrinter)}*){location};");
            }
            else
            {
                TypePrinter.PushMarshalKind(MarshalKind.ReturnVariableArray);
                var varReturnType = var.Type.Visit(TypePrinter);
                TypePrinter.PopMarshalKind();
                WriteLine($"var {ptr} = ({varReturnType}*){location};");
            }

            TypePrinter.PopContext();
            return ptr;
        }

        private bool GenerateFunctionSetter(Class @class, Property property)
        {
            var actualProperty = GetActualProperty(property, @class);
            if (actualProperty == null)
            {
                WriteLine($@"throw new MissingMethodException(""Method {property.Name} missing from explicit specialization {@class.Visit(TypePrinter)}."");");
                return false;
            }
            property = actualProperty;

            if (property.SetMethod.OperatorKind == CXXOperatorKind.Subscript)
                GenerateIndexerSetter(property.SetMethod);
            else
                GenerateFunctionInProperty(@class, property.SetMethod, actualProperty,
                    new QualifiedType(new BuiltinType(PrimitiveType.Void)));
            return true;
        }

        private void GenerateFieldSetter(Field field, Class @class, QualifiedType fieldType)
        {
            string returnVar;
            Type type = field.Type.Desugar();
            var arrayType = type as ArrayType;
            if (arrayType != null && @class.IsValueType)
            {
                returnVar = HandleValueArray(arrayType, field);
            }
            else
            {
                var name = ((Class)field.Namespace).Layout.Fields.First(
                    f => f.FieldPtr == field.OriginalPtr).Name;
                if (@class.IsValueType)
                    returnVar = $"{Helpers.InstanceField}.{name}";
                else
                {
                    var typeName = TypePrinter.PrintNative(@class);
                    if (IsInternalClassNested(@class))
                        typeName.RemoveNamespace();
                    returnVar = $"(({typeName}*){Helpers.InstanceIdentifier})->{name}";
                }
            }

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = field.QualifiedType
            };

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                Parameter = param,
                ArgName = param.Name,
                ReturnVarName = returnVar
            };
            ctx.PushMarshalKind(MarshalKind.NativeField);

            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            ctx.PushMarshalKind(MarshalKind.NativeField);
            ctx.ReturnType = field.QualifiedType;

            param.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                Indent();

            if (marshal.Context.Return.StringBuilder.Length > 0)
            {
                if (ctx.ReturnVarName.Length > 0)
                    Write($"{ctx.ReturnVarName} = ");
                if (type.IsPointer())
                {
                    Type pointee = type.GetFinalPointee();
                    if (pointee.IsPrimitiveType())
                    {
                        Write($"({TypePrinter.IntPtrType}) ");
                        var templateSubstitution = pointee.Desugar(false) as TemplateParameterSubstitutionType;
                        if (templateSubstitution != null)
                            Write("(object) ");
                    }
                }
                WriteLine($"{marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
            }

            if ((arrayType != null && @class.IsValueType) || ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();
        }

        private static bool IsInternalClassNested(Class @class)
        {
            return !(@class is ClassTemplateSpecialization) && !(@class.Namespace is ClassTemplateSpecialization);
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

            var name = ((Class)field.Namespace).Layout.Fields.First(
                f => f.FieldPtr == field.OriginalPtr).Name;
            WriteLine(string.Format("fixed ({0} {1} = {2}.{3})",
                type, arrPtr, Helpers.InstanceField, name));
            WriteOpenBraceAndIndent();
            return arrPtr;
        }

        private bool WrapSetterArrayOfPointers(string name, Type fieldType)
        {
            var arrayType = fieldType as ArrayType;
            if (arrayType == null || !arrayType.Type.IsPointerToPrimitiveType())
                return false;

            NewLine();
            WriteOpenBraceAndIndent();
            WriteLine("{0} = value;", name);
            WriteLine("if (!{0}{1})", name, "Initialised");
            WriteOpenBraceAndIndent();
            WriteLine("{0}{1} = true;", name, "Initialised");
            UnindentAndWriteCloseBrace();
            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        private void GenerateIndexerSetter(Function function)
        {
            Type type;
            function.OriginalReturnType.Type.IsPointerTo(out type);

            var @internal = TypePrinter.PrintNative(function.Namespace);
            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                Parameter = new Parameter
                {
                    Name = "value",
                    QualifiedType = new QualifiedType(type)
                },
                ParameterIndex = function.Parameters.Count(
                    p => p.Kind != ParameterKind.IndirectReturnType),
                ReturnType = new QualifiedType(type),
                ArgName = "value"
            };
            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            type.Visit(marshal);
            Write(marshal.Context.Before);

            var internalFunction = GetFunctionNativeIdentifier(function);
            var paramMarshal = GenerateFunctionParamMarshal(
                function.Parameters[0], 0);
            string call = $@"{@internal}.{internalFunction}({GetInstanceParam(function)}, {paramMarshal.Context.ArgumentPrefix}{paramMarshal.Name})";
            if (type.IsPrimitiveType())
            {
                WriteLine($"*{call} = {marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
            }
            else
            {
                if (type.TryGetClass(out Class @class) && @class.HasNonTrivialCopyConstructor)
                {
                    Method cctor = @class.Methods.First(c => c.IsCopyConstructor);
                    WriteLine($@"{TypePrinter.PrintNative(type)}.{GetFunctionNativeIdentifier(cctor)}({call}, {marshal.Context.Return});");
                }
                else
                {
                    WriteLine($@"*({TypePrinter.PrintNative(type)}*) {call} = {marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
                }
            }
            if (paramMarshal.HasUsingBlock)
                UnindentAndWriteCloseBrace();

            if (ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();
        }

        private void GeneratePropertyGetterForVariableWithInitializer(Variable variable, string signature)
        {
            var initializerString = variable.Initializer.String;
            Write($"{signature} {{ get; }} = ");
            Type type = variable.Type.Desugar();

            if (type is ArrayType arrayType)
            {
                var systemType = Internal.ExpressionHelper.GetSystemType(Context, arrayType.Type.Desugar());
                Write($"new {arrayType.Type}[{arrayType.Size}] ");
                Write("{ ");

                List<string> elements = Internal.ExpressionHelper.SplitInitListExpr(initializerString);

                while (elements.Count < arrayType.Size)
                    elements.Add(systemType == typeof(string) ? "\"\"" : null);

                for (int i = 0; i < elements.Count; ++i)
                {
                    var e = elements[i];

                    if (e == null)
                        Write("default");
                    else
                    {
                        if (!Internal.ExpressionHelper.TryParseExactLiteralExpression(ref e, systemType))
                            Write($"({arrayType.Type})");
                        Write(e);
                    }

                    if (i + 1 != elements.Count)
                        Write(", ");
                }

                Write(" }");
            }
            else
            {
                Write(initializerString);
            }
            WriteLine(";");
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
                WriteOpenBraceAndIndent();
                var to = ((Class)property.OriginalNamespace).OriginalClass;
                var baseOffset = GetOffsetToBase(@class, to);
                WriteLine("return {0} + {1};", Helpers.InstanceIdentifier, baseOffset);
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
                return;
            }

            if (decl is Function)
            {
                if (isAbstract)
                {
                    WriteLine(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteOpenBraceAndIndent();

                this.GenerateMember(@class, c => GenerateFunctionGetter(c, property));
            }
            else if (decl is Field)
            {
                var field = decl as Field;
                if (WrapGetterArrayOfPointers(decl.Name, field.Type))
                    return;

                NewLine();
                WriteOpenBraceAndIndent();
                this.GenerateField(@class, field, GenerateFieldGetter, false);
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteOpenBraceAndIndent();
                var var = decl as Variable;
                this.GenerateMember(@class, c => GenerateVariableGetter(
                    c is ClassTemplateSpecialization ?
                        c.Variables.First(v => v.Name == decl.Name) : var));
            }
            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private bool GenerateVariableGetter(Variable var)
        {
            string ptr = GeneratePointerTo(var);

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                ArgName = var.Name,
                ReturnType = var.QualifiedType
            };
            ctx.PushMarshalKind(MarshalKind.ReturnVariableArray);

            if (var.Type.Desugar().IsPointer())
            {
                var pointerType = var.Type.Desugar() as PointerType;
                while (pointerType != null && !pointerType.Pointee.Desugar().IsPrimitiveType(PrimitiveType.Char))
                {
                    ptr = $"*{ptr}";
                    pointerType = pointerType.Pointee.Desugar() as PointerType;
                }
                ptr = $"({TypePrinter.IntPtrType}*)({ptr})";
            }

            var arrayType = var.Type.Desugar() as ArrayType;
            var isRefTypeArray = arrayType != null && var.Namespace is Class context && context.IsRefType;
            var elementType = arrayType?.Type.Desugar();
            Type type = (var.QualifiedType.Type.GetFinalPointee() ?? var.QualifiedType.Type).Desugar();
            if (type.TryGetClass(out Class @class) && @class.IsRefType)
                ctx.ReturnVarName = $"new {TypePrinter.IntPtrType}({ptr})";
            else if (!isRefTypeArray && elementType == null)
                ctx.ReturnVarName = $"*{ptr}";
            else if (elementType == null || elementType.IsPrimitiveType() ||
                arrayType.SizeType == ArrayType.ArraySize.Constant)
                ctx.ReturnVarName = ptr;
            else
                ctx.ReturnVarName = $@"{elementType}.{Helpers.CreateInstanceIdentifier}(new {TypePrinter.IntPtrType}({ptr}))";

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            var.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                Indent();

            WriteLine("return {0};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();

            return true;
        }

        private bool GenerateFunctionGetter(Class @class, Property property)
        {
            var actualProperty = GetActualProperty(property, @class);
            if (actualProperty == null)
            {
                WriteLine($@"throw new MissingMethodException(""Method {property.Name} missing from explicit specialization {@class.Visit(TypePrinter)}."");");
                return false;
            }
            QualifiedType type = default;
            if (actualProperty != property ||
                // indexers
                (property.QualifiedType.Type.IsPrimitiveType() &&
                 actualProperty.GetMethod.ReturnType.Type.IsPointerToPrimitiveType()))
            {
                type = property.QualifiedType;
            }
            GenerateFunctionInProperty(@class, actualProperty.GetMethod, actualProperty, type);
            return false;
        }

        private static Property GetActualProperty(Property property, Class c)
        {
            if (!(c is ClassTemplateSpecialization))
                return property;
            return c.Properties.SingleOrDefault(p => p.GetMethod != null &&
                p.GetMethod.InstantiatedFrom ==
                (property.GetMethod.OriginalFunction ?? property.GetMethod));
        }

        private void GenerateFunctionInProperty(Class @class, Method constituent,
            Property property, QualifiedType type)
        {
            bool isInSecondaryBase = constituent.OriginalFunction != null &&
                 constituent.Namespace == @class;
            if (constituent.IsVirtual && (!property.IsOverride ||
                isInSecondaryBase || @class.GetBaseProperty(property).IsPure))
                GenerateFunctionCall(GetVirtualCallDelegate(constituent),
                    constituent, type);
            else if (property.IsOverride && !isInSecondaryBase)
                WriteLine(property.GetMethod == constituent ?
                    "return base.{0};" : "base.{0} = value;", property.Name);
            else
                GenerateInternalFunctionCall(constituent, type);
        }

        private void GenerateFieldGetter(Field field, Class @class, QualifiedType returnType)
        {
            var name = ((Class)field.Namespace).Layout.Fields.First(
                f => f.FieldPtr == field.OriginalPtr).Name;
            string returnVar;
            Type fieldType = field.Type.Desugar();
            var arrayType = fieldType as ArrayType;
            if (@class.IsValueType)
            {
                if (arrayType != null)
                    returnVar = HandleValueArray(arrayType, field);
                else
                    returnVar = $"{Helpers.InstanceField}.{name}";
            }
            else
            {
                var typeName = TypePrinter.PrintNative(@class);
                if (IsInternalClassNested(@class))
                    typeName.RemoveNamespace();
                returnVar = $"(({typeName}*){Helpers.InstanceIdentifier})->{name}";
                // Class field getter should return a reference object instead of a copy. Wrapping `returnVar` in
                // IntPtr ensures that non-copying object constructor is invoked.
                Class typeClass;
                if (fieldType.TryGetClass(out typeClass) && !typeClass.IsValueType &&
                    !ASTUtils.IsMappedToPrimitive(Context.TypeMaps, fieldType))
                    returnVar = $"new {TypePrinter.IntPtrType}(&{returnVar})";
            }

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                ArgName = field.Name,
                ReturnVarName = returnVar,
                ReturnType = returnType
            };
            ctx.PushMarshalKind(MarshalKind.NativeField);

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            field.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                Indent();

            Write("return ");

            var @return = marshal.Context.Return.ToString();
            if (fieldType.IsPointer())
            {
                var final = fieldType.GetFinalPointee().Desugar(resolveTemplateSubstitution: false);
                var templateSubstitution = final as TemplateParameterSubstitutionType;
                if (templateSubstitution != null && returnType.Type.IsDependent)
                    Write($"({templateSubstitution.ReplacedParameter.Parameter.Name}) (object) ");
                if ((final.IsPrimitiveType() && !final.IsPrimitiveType(PrimitiveType.Void) &&
                    ((!final.IsPrimitiveType(PrimitiveType.Char) &&
                      !final.IsPrimitiveType(PrimitiveType.WideChar) &&
                      !final.IsPrimitiveType(PrimitiveType.Char16) &&
                      !final.IsPrimitiveType(PrimitiveType.Char32)) ||
                     (!Context.Options.MarshalCharAsManagedChar &&
                      !((PointerType)fieldType).QualifiedPointee.Qualifiers.IsConst)) &&
                    templateSubstitution == null) ||
                    (!((PointerType)fieldType).QualifiedPointee.Qualifiers.IsConst &&
                      (final.IsPrimitiveType(PrimitiveType.WideChar) ||
                       final.IsPrimitiveType(PrimitiveType.Char16) ||
                       final.IsPrimitiveType(PrimitiveType.Char32))))
                    Write($"({fieldType.GetPointee().Desugar()}*) ");
            }
            WriteLine($"{@return};");

            if ((arrayType != null && @class.IsValueType) || ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();
        }

        private bool WrapGetterArrayOfPointers(string name, Type fieldType)
        {
            var arrayType = fieldType as ArrayType;
            if (arrayType != null && arrayType.Type.IsPointerToPrimitiveType())
            {
                NewLine();
                WriteOpenBraceAndIndent();
                WriteLine("if (!{0}{1})", name, "Initialised");
                WriteOpenBraceAndIndent();
                WriteLine("{0} = null;", name);
                WriteLine("{0}{1} = true;", name, "Initialised");
                UnindentAndWriteCloseBrace();
                WriteLine("return {0};", name);
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
                return true;
            }
            return false;
        }

        public void GenerateClassMethods(IList<Method> methods)
        {
            if (methods.Count == 0)
                return;

            var @class = (Class)methods[0].Namespace;

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
                    string safeIdentifier = name.StartsWith("@") ? name.Substring(1) : name;
                    WriteLine($"private bool __{safeIdentifier}Initialised;");
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
                        (rootBaseProperty = @class.GetBasePropertyByName(prop, true)) != null &&
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
                    WriteLine($@"{printedType} {prop.ExplicitInterfaceImpl.Name}.{GetPropertyName(prop)}");
                }
                WriteOpenBraceAndIndent();

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

                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private string GetPropertyName(Property prop)
        {
            var isIndexer = prop.Parameters.Count != 0;
            if (!isIndexer)
                return prop.Name;

            var @params = prop.Parameters.Select(param =>
            {
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
            TypePrinter.PushMarshalKind(MarshalKind.ReturnVariableArray);
            var variableType = variable.Type.Visit(TypePrinter);
            TypePrinter.PopMarshalKind();

            bool hasInitializer = variable.Initializer != null && !string.IsNullOrWhiteSpace(variable.Initializer.String);

            if (hasInitializer && variable.QualifiedType.Qualifiers.IsConst &&
                (variable.Type.Desugar() is BuiltinType || variableType.ToString() == "string"))
                Write($"public const {variableType} {variable.Name} = {variable.Initializer.String};");
            else
            {
                var signature = $"public static {variableType} {variable.Name}";

                if (hasInitializer)
                    GeneratePropertyGetterForVariableWithInitializer(variable, signature);
                else
                {
                    using (WriteBlock(signature))
                    {
                        GeneratePropertyGetter(variable, @class);

                        if (!variable.QualifiedType.Qualifiers.IsConst &&
                            !(variable.Type.Desugar() is ArrayType))
                            GeneratePropertySetter(variable, @class);
                    }
                }
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        #region Virtual Tables

        public List<VTableComponent> GetUniqueVTableMethodEntries(Class @class)
        {
            if (@class.IsDependent)
                @class = @class.Specializations[0];

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

            bool generateNativeToManaged = Options.GenerateNativeToManagedFor(@class);

            PushBlock(BlockKind.Region);
            WriteLine("#region Virtual table interop");
            NewLine();

            bool hasDynamicBase = @class.NeedsBase && @class.BaseClass.IsDynamic;
            var originalTableClass = @class.IsDependent ? @class.Specializations[0] : @class;

            // vtable hooks don't work without a NativeToManaged map, because we can't look up the managed
            // instance from a native pointer without the map, so don't generate them here.
            // this also means we can't inherit from this class and override virtual methods in C#
            if (generateNativeToManaged)
            {
                // Generate a delegate type for each method.
                foreach (var method in wrappedEntries.Select(e => e.Method).Where(m => !m.Ignore))
                    GenerateVTableMethodDelegates(containingClass, method.Namespace.IsDependent ?
                       (Method)method.InstantiatedFrom : method);

                var hasVirtualDtor = wrappedEntries.Any(e => e.Method.IsDestructor);
                var destructorOnly = "destructorOnly";

                using (WriteBlock($"internal static{(hasDynamicBase ? " new" : string.Empty)} class VTableLoader"))
                {
                    WriteLines($@"
                    private static volatile bool initialized;
                    private static readonly IntPtr*[] ManagedVTables = new IntPtr*[{@class.Layout.VTablePointers.Count}];{(hasVirtualDtor ? $@"
                    private static readonly IntPtr*[] ManagedVTablesDtorOnly = new IntPtr*[{@class.Layout.VTablePointers.Count}];" : "")}
                    private static readonly IntPtr[] Thunks = new IntPtr[{wrappedEntries.Count}];
                    private static CppSharp.Runtime.VTables VTables;
                    private static readonly global::System.Collections.Generic.List<CppSharp.Runtime.SafeUnmanagedMemoryHandle>
                        SafeHandles = new global::System.Collections.Generic.List<CppSharp.Runtime.SafeUnmanagedMemoryHandle>();
                ", trimIndentation: true);

                    using (WriteBlock($"static VTableLoader()"))
                    {
                        foreach (var entry in wrappedEntries.Distinct().Where(e => !e.Method.Ignore))
                        {
                            var name = GetVTableMethodDelegateName(entry.Method);
                            WriteLine($"{name + "Instance"} += {name}Hook;");
                        }
                        for (var i = 0; i < wrappedEntries.Count; ++i)
                        {
                            var entry = wrappedEntries[i];
                            if (!entry.Method.Ignore)
                            {
                                var name = GetVTableMethodDelegateName(entry.Method);
                                WriteLine($"Thunks[{i}] = Marshal.GetFunctionPointerForDelegate({name + "Instance"});");
                            }
                        }
                    }
                    NewLine();

                    using (WriteBlock($"public static CppSharp.Runtime.VTables SetupVTables(IntPtr instance, bool {destructorOnly} = false)"))
                    {
                        WriteLine($"if (!initialized)");
                        {
                            WriteOpenBraceAndIndent();
                            WriteLine($"lock (ManagedVTables)");
                            WriteOpenBraceAndIndent();
                            WriteLine($"if (!initialized)");
                            {
                                WriteOpenBraceAndIndent();
                                WriteLine($"initialized = true;");
                                WriteLine($"VTables.Tables = {($"new IntPtr[] {{ {string.Join(", ", originalTableClass.Layout.VTablePointers.Select(x => $"*(IntPtr*)(instance + {x.Offset})"))} }}")};");
                                WriteLine($"VTables.Methods = new Delegate[{originalTableClass.Layout.VTablePointers.Count}][];");

                                if (hasVirtualDtor)
                                    AllocateNewVTables(@class, wrappedEntries, destructorOnly: true, "ManagedVTablesDtorOnly");

                                AllocateNewVTables(@class, wrappedEntries, destructorOnly: false, "ManagedVTables");

                                if (!hasVirtualDtor)
                                {
                                    WriteLine($"if ({destructorOnly})");
                                    WriteLineIndent("return VTables;");
                                }
                                UnindentAndWriteCloseBrace();
                            }
                            UnindentAndWriteCloseBrace();
                            UnindentAndWriteCloseBrace();
                        }
                        NewLine();

                        if (hasVirtualDtor)
                        {
                            WriteLine($"if ({destructorOnly})");
                            {
                                WriteOpenBraceAndIndent();
                                AssignNewVTableEntries(@class, "ManagedVTablesDtorOnly");
                                UnindentAndWriteCloseBrace();
                            }
                            WriteLine("else");
                            {
                                WriteOpenBraceAndIndent();
                                AssignNewVTableEntries(@class, "ManagedVTables");
                                UnindentAndWriteCloseBrace();
                            }
                        }
                        else
                        {
                            AssignNewVTableEntries(@class, "ManagedVTables");
                        }

                        WriteLine("return VTables;");
                    }
                }
                NewLine();
            }

            if (!hasDynamicBase)
                WriteLine("protected CppSharp.Runtime.VTables __vtables;");

            using (WriteBlock($"internal {(hasDynamicBase ? "override" : "virtual")} CppSharp.Runtime.VTables __VTables"))
            {
                WriteLines($@"
                get {{
                    if (__vtables.IsEmpty)
                        __vtables.Tables = {($"new IntPtr[] {{ {string.Join(", ", originalTableClass.Layout.VTablePointers.Select(x => $"*(IntPtr*)({Helpers.InstanceIdentifier} + {x.Offset})"))} }}")};
                    return __vtables;
                }}

                set {{
                    __vtables = value;
                }}", trimIndentation: true);
            }

            using (WriteBlock($"internal {(hasDynamicBase ? "override" : "virtual")} void SetupVTables(bool destructorOnly = false)"))
            {
                // same reason as above, we can't hook vtable without ManagedToNative map
                if (generateNativeToManaged)
                {
                    WriteLines($@"
                    if (__VTables.IsTransient)
                        __VTables = VTableLoader.SetupVTables(__Instance, destructorOnly);", trimIndentation: true);
                }
            }

            WriteLine("#endregion");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void AllocateNewVTables(Class @class, IList<VTableComponent> wrappedEntries,
            bool destructorOnly, string table)
        {
            if (Context.ParserOptions.IsMicrosoftAbi)
                AllocateNewVTablesMS(@class, wrappedEntries, destructorOnly, table);
            else
                AllocateNewVTablesItanium(@class, wrappedEntries, destructorOnly, table);
        }

        private void AssignNewVTableEntries(Class @class, string table)
        {
            for (int i = 0; i < @class.Layout.VTablePointers.Count; i++)
            {
                var offset = @class.Layout.VTablePointers[i].Offset;
                WriteLine($"*(IntPtr**)(instance + {offset}) = {table}[{i}];");
            }
        }

        private void AllocateNewVTablesMS(Class @class, IList<VTableComponent> wrappedEntries,
            bool destructorOnly, string table)
        {
            for (int i = 0; i < @class.Layout.VFTables.Count; i++)
            {
                VFTableInfo vftable = @class.Layout.VFTables[i];

                AllocateNewVTableEntries(vftable.Layout.Components, wrappedEntries,
                    @class.Layout.VTablePointers[i].Offset, i,
                    vftable.Layout.Components.Any(c => c.Kind == VTableComponentKind.RTTI) ? 1 : 0,
                    destructorOnly,
                    table);
            }
        }

        private void AllocateNewVTablesItanium(Class @class, IList<VTableComponent> wrappedEntries,
            bool destructorOnly, string table)
        {
            AllocateNewVTableEntries(@class.Layout.Layout.Components,
                wrappedEntries, @class.Layout.VTablePointers[0].Offset, 0,
                VTables.ItaniumOffsetToTopAndRTTI, destructorOnly, table);
        }

        private void AllocateNewVTableEntries(IList<VTableComponent> entries,
            IList<VTableComponent> wrappedEntries, uint vptrOffset, int tableIndex,
            int offsetRTTI, bool destructorOnly, string table)
        {
            string suffix = (destructorOnly ? "_dtor" : string.Empty) +
                (tableIndex == 0 ? string.Empty : tableIndex.ToString(CultureInfo.InvariantCulture));

            WriteLine($"{table}[{tableIndex}] = CppSharp.Runtime.VTables.CloneTable(SafeHandles, instance, {vptrOffset}, {entries.Count}, {offsetRTTI});");

            // fill the newly allocated v-table
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if ((entry.Kind == VTableComponentKind.FunctionPointer ||
                     entry.Kind == VTableComponentKind.DeletingDtorPointer) &&
                    !entry.IsIgnored() &&
                    (!destructorOnly || entry.Method.IsDestructor ||
                     Context.Options.ExplicitlyPatchedVirtualFunctions.Contains(entry.Method.QualifiedOriginalName)))
                    // patch with pointers to managed code where needed
                    WriteLine("{0}[{1}][{2}] = Thunks[{3}];", table, tableIndex, i - offsetRTTI, wrappedEntries.IndexOf(entry));
            }

            if (!destructorOnly)
                WriteLine($"VTables.Methods[{tableIndex}] = new Delegate[{entries.Count}];");
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
                WriteLine($@"SetupVTables(GetType().FullName == ""{typeFullName.Type.Replace("global::", string.Empty)}"");");
            }
        }

        private void GenerateVTableManagedCall(Method method)
        {
            if (method.IsDestructor)
            {
                WriteLine("{0}.Dispose(disposing: true, callNativeDtor: true);", Helpers.TargetIdentifier);
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

                var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
                {
                    ReturnType = param.QualifiedType,
                    ReturnVarName = param.Name,
                    ParameterIndex = i,
                    Parameter = param
                };

                ctx.PushMarshalKind(MarshalKind.GenericDelegate);
                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);
                ctx.PopMarshalKind();

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                marshals.Add(marshal.Context.Return);
                if (ctx.HasCodeBlock)
                {
                    Indent();
                    numBlocks++;
                }
            }

            Type returnType = method.OriginalReturnType.Type.Desugar();
            bool isPrimitive = returnType.IsPrimitiveType();
            bool isVoid = returnType.IsPrimitiveType(PrimitiveType.Void);
            var property = ((Class)method.Namespace).Properties.Find(
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
                Write(property.Name);
                if (isSetter)
                    Write($" = {marshalsCode}");
            }
            WriteLine(";");

            // on Microsoft ABIs, the destructor on copy-by-value parameters is
            // called by the called function, not the caller, so we are generating
            // code to do that for classes that have a non-trivial destructor.
            if (Context.ParserOptions.IsMicrosoftAbi)
            {
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var param = method.Parameters[i];
                    if (param.Ignore)
                        continue;

                    if (param.Kind == ParameterKind.IndirectReturnType)
                        continue;

                    var paramType = param.Type.GetFinalPointee();

                    if (param.IsIndirect &&
                        paramType.TryGetClass(out Class paramClass) && !(paramClass is ClassTemplateSpecialization) &&
                        paramClass.HasNonTrivialDestructor)
                    {
                        WriteLine($"{Generator.GeneratedIdentifier("result")}{i}.Dispose(false, true);");
                    }
                }
            }

            if (hasReturn && isPrimitive && !isSetter)
            {
                WriteLine($"return { Helpers.ReturnIdentifier};");
                return;
            }

            if (hasReturn)
            {
                var param = new Parameter
                {
                    Name = Helpers.ReturnIdentifier,
                    QualifiedType = method.OriginalReturnType,
                    Namespace = method
                };

                // Marshal the managed result to native
                var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
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
                    WriteLine($@"*({TypePrinter.PrintNative(method.OriginalReturnType)}*) {retParam.Name} = {marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
                }
                else
                {
                    WriteLine($"return {marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
                }

                if (ctx.HasCodeBlock)
                    UnindentAndWriteCloseBrace();
            }

            if (!isVoid && isSetter)
                WriteLine("return false;");

            for (var i = 0; i < numBlocks; ++i)
                UnindentAndWriteCloseBrace();
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
            TypePrinter.PushMarshalKind(MarshalKind.VTableReturnValue);
            var @params = GatherInternalParams(method, out retType);

            var vTableMethodDelegateName = GetVTableMethodDelegateName(method);
            TypePrinter.PopMarshalKind();

            WriteLine($"private static {method.FunctionType} {vTableMethodDelegateName}Instance;");
            NewLine();

            using (WriteBlock($"private static {retType} {vTableMethodDelegateName}Hook({string.Join(", ", @params)})"))
            {
                WriteLine($@"var {Helpers.TargetIdentifier} = {@class.Visit(TypePrinter)}.__GetInstance({Helpers.InstanceField});");
                GenerateVTableManagedCall(method);
            }

            PopBlock(NewLineKind.Always);
        }

        public string GetVTableMethodDelegateName(Function function)
        {
            var nativeId = GetFunctionNativeIdentifier(function, true);

            // Trim '@' (if any) because '@' is valid only as the first symbol.
            nativeId = nativeId.Trim('@');

            return string.Format("_{0}Delegate", nativeId);
        }

        public bool HasVirtualTables(Class @class)
        {
            return @class.IsGenerated && @class.IsDynamic && GetUniqueVTableMethodEntries(@class).Count > 0;
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

            WriteLine("[UnmanagedFunctionPointer(__CallingConvention.Cdecl)]");
            WriteLine("delegate void {0}({1});", delegateName, args);
            WriteLine("{0} {1};", delegateName, delegateRaise);
            NewLine();

            WriteLine("{0} {1};", @event.Type, delegateInstance);
            WriteLine("public event {0} {1}", @event.Type, @event.Name);
            WriteOpenBraceAndIndent();

            GenerateEventAdd(@event, delegateRaise, delegateName, delegateInstance);
            NewLine();

            GenerateEventRemove(@event, delegateInstance);

            UnindentAndWriteCloseBrace();
            NewLine();

            GenerateEventRaiseWrapper(@event, delegateInstance);
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        private void GenerateEventAdd(Event @event, string delegateRaise, string delegateName, string delegateInstance)
        {
            WriteLine("add");
            WriteOpenBraceAndIndent();

            WriteLine("if ({0} == null)", delegateRaise);
            WriteOpenBraceAndIndent();

            WriteLine("{0} = new {1}(_{2}Raise);", delegateRaise, delegateName, @event.Name);

            WriteLine("var {0} = Marshal.GetFunctionPointerForDelegate({1}).ToPointer();",
                Generator.GeneratedIdentifier("ptr"), delegateInstance);

            // Call type map here.

            //WriteLine("((::{0}*)NativePtr)->{1}.Connect(_fptr);", @class.QualifiedOriginalName,
            //    @event.OriginalName);

            UnindentAndWriteCloseBrace();

            WriteLine("{0} = ({1})System.Delegate.Combine({0}, value);",
                delegateInstance, @event.Type);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateEventRemove(ITypedDecl @event, string delegateInstance)
        {
            WriteLine("remove");
            WriteOpenBraceAndIndent();

            WriteLine("{0} = ({1})System.Delegate.Remove({0}, value);",
                delegateInstance, @event.Type);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateEventRaiseWrapper(Event @event, string delegateInstance)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);
            var args = TypePrinter.VisitParameters(@event.Parameters, hasNames: true);
            TypePrinter.PopContext();

            WriteLine("void _{0}Raise({1})", @event.Name, args);
            WriteOpenBraceAndIndent();

            var returns = new List<string>();
            foreach (var param in @event.Parameters)
            {
                var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
                {
                    ReturnVarName = param.Name,
                    ReturnType = param.QualifiedType
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                param.Visit(marshal);

                returns.Add(marshal.Context.Return);
            }

            WriteLine("if ({0} != null)", delegateInstance);
            WriteOpenBraceAndIndent();
            WriteLine("{0}({1});", delegateInstance, string.Join(", ", returns));
            UnindentAndWriteCloseBrace();

            UnindentAndWriteCloseBrace();
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

            // We used to always call GenerateClassFinalizer here. However, the
            // finalizer calls Dispose which is conditionally implemented below.
            // Instead, generate the finalizer only if Dispose is also implemented.

            // ensure any virtual dtor in the chain is called
            var dtor = @class.Destructors.FirstOrDefault(d => d.Access != AccessSpecifier.Private);
            if (ShouldGenerateClassNativeField(@class) ||
                (dtor != null && (dtor.IsVirtual || @class.HasNonTrivialDestructor)) ||
                // virtual destructors in abstract classes may lack a pointer in the v-table
                // so they have to be called by symbol; thus we need an explicit Dispose override
                @class.IsAbstract)
                if (!@class.IsOpaque)
                {
                    if (@class.IsRefType)
                        GenerateClassFinalizer(@class);
                    if (NeedsDispose(@class))
                        GenerateDisposeMethods(@class);
                }
        }

        private void GenerateClassFinalizer(Class @class)
        {
            if (!Options.GenerateFinalizerFor(@class))
                return;

            using (PushWriteBlock(BlockKind.Finalizer, $"~{@class.Name}()", NewLineKind.BeforeNextBlock))
                WriteLine($"Dispose(false, callNativeDtor: {Helpers.OwnsNativeInstanceIdentifier});");
        }

        private void GenerateDisposeMethods(Class @class)
        {
            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;

            var hasDtorParam = @class.IsRefType;

            // Generate the IDispose Dispose() method.
            if (!hasBaseClass)
            {
                using (PushWriteBlock(BlockKind.Method, "public void Dispose()", NewLineKind.BeforeNextBlock))
                {
                    WriteLine(hasDtorParam
                        ? $"Dispose(disposing: true, callNativeDtor: {Helpers.OwnsNativeInstanceIdentifier});"
                        : "Dispose(disposing: true);");
                    if (Options.GenerateFinalizerFor(@class))
                        WriteLine("GC.SuppressFinalize(this);");
                }
            }

            // Declare partial method that the partial class can implement to participate
            // in dispose.
            PushBlock(BlockKind.Method);
            WriteLine("partial void DisposePartial(bool disposing);");
            PopBlock(NewLineKind.BeforeNextBlock);

            // Generate Dispose(bool, bool) method
            var ext = !@class.IsValueType ? (hasBaseClass ? "override " : "virtual ") : string.Empty;
            var protectionLevel = @class.IsValueType ? "private" : "internal protected";
            using var _ = PushWriteBlock(BlockKind.Method,
                $"{protectionLevel} {ext}void Dispose(bool disposing{(hasDtorParam ? ", bool callNativeDtor" : "")})",
                NewLineKind.BeforeNextBlock);

            if (@class.IsRefType)
            {
                WriteLine("if ({0} == IntPtr.Zero)", Helpers.InstanceIdentifier);
                WriteLineIndent("return;");

                // The local var must be of the exact type in the object map because of TryRemove
                if (Options.GenerateNativeToManagedFor(@class))
                    WriteLine("NativeToManagedMap.TryRemove({0}, out _);", Helpers.InstanceIdentifier);
                var realClass = @class.IsTemplate ? @class.Specializations[0] : @class;
                var classInternal = TypePrinter.PrintNative(realClass);
                if (@class.IsDynamic && GetUniqueVTableMethodEntries(realClass).Count != 0)
                {
                    for (int i = 0; i < realClass.Layout.VTablePointers.Count; i++)
                    {
                        var offset = realClass.Layout.VTablePointers[i].Offset;
                        WriteLine($"*(IntPtr*)({Helpers.InstanceIdentifier} + {offset}) = __VTables.Tables[{i}];");
                    }
                }
            }

            // TODO: if disposing == true we should delegate to the dispose methods of references
            // we hold to other generated type instances since those instances could also hold
            // references to unmanaged memory.
            //
            // Delegate to partial method if implemented
            WriteLine("DisposePartial(disposing);");

            var dtor = @class.Destructors.FirstOrDefault();
            if (dtor != null && dtor.Access != AccessSpecifier.Private &&
                @class.HasNonTrivialDestructor && !@class.IsAbstract)
            {
                NativeLibrary library;
                if (!Options.CheckSymbols ||
                    Context.Symbols.FindLibraryBySymbol(dtor.Mangled, out library))
                {
                    // Normally, calling the native dtor should be controlled by whether or not we
                    // we own the underlying instance. (i.e. Helpers.OwnsNativeInstanceIdentifier).
                    // However, there are 2 situations when the caller needs to have direct control
                    //
                    // 1. When we have a virtual dtor on the native side we detour the vtable entry
                    // even when we don't own the underlying native instance. I think we do this
                    // so that the managed side can null out the __Instance pointer and remove the
                    // instance from the NativeToManagedMap. Of course, this is somewhat half-hearted
                    // since we can't/don't do this when there's no virtual dtor available to detour.
                    // Anyway, we must be able to call the native dtor in this case even if we don't
                    // own the underlying native instance.
                    //
                    // 2. When we we pass a disposable object to a function "by value" then the callee
                    // calls the dtor on the argument so our marshalling code must have a way from preventing
                    // a duplicate call. Here's a native function that exhibits this behavior:
                    // void f(std::string f)
                    // {
                    //   ....
                    // compiler generates call to f.dtor() at the end of function
                    // }
                    //
                    // IDisposable.Dispose() and Object.Finalize() set callNativeDtor = Helpers.OwnsNativeInstanceIdentifier
                    if (hasDtorParam)
                    {
                        WriteLine("if (callNativeDtor)");
                        if (@class.IsDependent || dtor.IsVirtual)
                            WriteOpenBraceAndIndent();
                        else
                            Indent();
                    }

                    if (dtor.IsVirtual)
                        this.GenerateMember(@class, c => GenerateDestructorCall(
                            c is ClassTemplateSpecialization ?
                                c.Methods.First(m => m.InstantiatedFrom == dtor) : dtor));
                    else
                        this.GenerateMember(@class, c => GenerateMethodBody(c, dtor));

                    if (hasDtorParam)
                    {
                        if (@class.IsDependent || dtor.IsVirtual)
                            UnindentAndWriteCloseBrace();
                        else
                            Unindent();
                    }
                }
            }

            // If we have any fields holding references to unmanaged memory allocated here, free the
            // referenced memory. Don't rely on testing if the field's IntPtr is IntPtr.Zero since
            // unmanaged memory isn't always initialized and/or a reference may be owned by the
            // native side.

            string ptr;
            if (@class.IsValueType)
            {
                ptr = $"{Helpers.InstanceIdentifier}Ptr";
                WriteLine($"fixed ({Helpers.InternalStruct}* {ptr} = &{Helpers.InstanceIdentifier})");
                WriteOpenBraceAndIndent();
            }
            else
            {
                ptr = $"(({Helpers.InternalStruct}*){Helpers.InstanceIdentifier})";
            }

            foreach (var prop in @class.GetConstCharFieldProperties())
            {
                string name = prop.Field.OriginalName;
                WriteLine($"if (__{name}_OwnsNativeMemory)");
                WriteLineIndent($"Marshal.FreeHGlobal({ptr}->{name});");
            }

            if (@class.IsValueType)
            {
                UnindentAndWriteCloseBrace();
            }
            else
            {
                WriteLine("if ({0})", Helpers.OwnsNativeInstanceIdentifier);
                WriteLineIndent("Marshal.FreeHGlobal({0});", Helpers.InstanceIdentifier);

                WriteLine("{0} = IntPtr.Zero;", Helpers.InstanceIdentifier);
            }
        }

        private bool GenerateDestructorCall(Method dtor)
        {
            var @class = (Class)dtor.Namespace;
            GenerateVirtualFunctionCall(dtor, true);
            if (@class.IsAbstract)
            {
                UnindentAndWriteCloseBrace();
                WriteLine("else");
                Indent();
                GenerateInternalFunctionCall(dtor);
                Unindent();
            }
            return true;
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
                TypePrinterResult printedClass = @class.Visit(TypePrinter);
                printedClass.RemoveNamespace();

                WriteLine("internal static {0}{1} {2}({3} native, bool skipVTables = false)",
                    @class.NeedsBase && !@class.BaseClass.IsInterface ? "new " : string.Empty,
                    printedClass, Helpers.CreateInstanceIdentifier, TypePrinter.IntPtrType);
                WriteOpenBraceAndIndent();

                if (@class.IsRefType)
                {
                    WriteLine($"if (native == {TypePrinter.IntPtrType}.Zero)");
                    WriteLineIndent("return null;");
                }

                var suffix = @class.IsAbstract ? "Internal" : string.Empty;
                var ctorCall = $"{printedClass.Type}{suffix}{printedClass.NameSuffix}";
                WriteLine("return new {0}(native.ToPointer(), skipVTables);", ctorCall);
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);

                if (@class.IsRefType)
                {
                    var @new = @class.HasBase && @class.HasRefBase();

                    bool generateNativeToManaged = Options.GenerateNativeToManagedFor(@class);
                    if (generateNativeToManaged)
                    {
                        WriteLines($@"
internal static{(@new ? " new" : string.Empty)} {printedClass} __GetOrCreateInstance({TypePrinter.IntPtrType} native, bool saveInstance = false, bool skipVTables = false)
{{
    if (native == {TypePrinter.IntPtrType}.Zero)
        return null;
    if ({Helpers.TryGetNativeToManagedMappingIdentifier}(native, out var managed))
        return ({printedClass})managed;
    var result = {Helpers.CreateInstanceIdentifier}(native, skipVTables);
    if (saveInstance)
        {Helpers.RecordNativeToManagedMappingIdentifier}(native, result);
    return result;
}}");
                        NewLine();
                    }

                    // __GetInstance doesn't work without a ManagedToNativeMap, so don't generate it
                    if (HasVirtualTables(@class) && generateNativeToManaged)
                    {
                        @new = @class.HasBase && HasVirtualTables(@class.Bases.First().Class);

                        WriteLines($@"
internal static{(@new ? " new" : string.Empty)} {printedClass} __GetInstance({TypePrinter.IntPtrType} native)
{{
    if (!{Helpers.TryGetNativeToManagedMappingIdentifier}(native, out var managed))
        throw new global::System.Exception(""No managed instance was found"");
    var result = ({printedClass})managed;
    if (result.{Helpers.OwnsNativeInstanceIdentifier})
        result.SetupVTables();
    return result;
}}");
                        NewLine();
                    }
                }
            }

            this.GenerateNativeConstructorsByValue(@class);

            PushBlock(BlockKind.Method);
            WriteLine("{0} {1}(void* native, bool skipVTables = false){2}",
                @class.IsAbstractImpl ? "internal" : (@class.IsRefType ? "protected" : "private"),
                @class.Name, @class.IsValueType ? " : this()" : string.Empty);

            var hasBaseClass = @class.HasBaseClass && @class.BaseClass.IsRefType;
            if (hasBaseClass)
                WriteLineIndent(": base((void*) native)", @class.BaseClass.Visit(TypePrinter));

            WriteOpenBraceAndIndent();

            if (@class.IsRefType)
            {
                if (@class.BaseClass?.Layout.HasSubclassAtNonZeroOffset == true)
                    WriteLine("{0} = {1};", Helpers.PrimaryBaseOffsetIdentifier,
                        GetOffsetToBase(@class, @class.BaseClass));
                var hasVTables = @class.IsDynamic && GetUniqueVTableMethodEntries(@class).Count > 0;
                if (!hasBaseClass || hasVTables)
                {
                    WriteLine("if (native == null)");
                    WriteLineIndent("return;");
                    if (!hasBaseClass)
                        WriteLine($"{Helpers.InstanceIdentifier} = new {TypePrinter.IntPtrType}(native);");
                }
                var dtor = @class.Destructors.FirstOrDefault();
                var setupVTables = !@class.IsAbstractImpl && hasVTables && dtor?.IsVirtual == true;
                if (setupVTables)
                {
                    WriteLine("if (!skipVTables)");
                    Indent();
                    GenerateVTableClassSetupCall(@class, destructorOnly: true);
                    Unindent();
                }
            }
            else if (!hasBaseClass)
            {
                WriteLine($"{Helpers.InstanceField} = *({TypePrinter.PrintNative(@class)}*) native;");
            }

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateNativeConstructorByValue(Class @class, TypePrinterResult returnType)
        {
            var @internal = TypePrinter.PrintNative(@class.IsAbstractImpl ? @class.BaseClass : @class);

            if (IsInternalClassNested(@class))
                @internal.RemoveNamespace();

            if (!@class.IsAbstractImpl)
            {
                returnType.RemoveNamespace();
                PushBlock(BlockKind.Method);
                WriteLine("internal static {0} {1}({2} native, bool skipVTables = false)",
                    returnType, Helpers.CreateInstanceIdentifier, @internal);
                WriteOpenBraceAndIndent();
                var suffix = @class.IsAbstract ? "Internal" : "";
                WriteLine($"return new {returnType.Type}{suffix}{returnType.NameSuffix}(native, skipVTables);");
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (@class.IsRefType && !@class.IsAbstract)
            {
                PushBlock(BlockKind.Method);
                Generate__CopyValue(@class, @internal);
                PopBlock(NewLineKind.BeforeNextBlock);
            }
            if (!@class.IsAbstract)
            {
                PushBlock(BlockKind.Method);
                WriteLine("{0} {1}({2} native, bool skipVTables = false)",
                    @class.IsAbstractImpl ? "internal" : "private", @class.Name, @internal);
                WriteLineIndent(@class.IsRefType ? ": this(__CopyValue(native), skipVTables)" : ": this()");
                WriteOpenBraceAndIndent();
                if (@class.IsRefType)
                {
                    WriteLine($"{Helpers.OwnsNativeInstanceIdentifier} = true;");
                    if (Options.GenerateNativeToManagedFor(@class))
                        WriteLine($"{Helpers.RecordNativeToManagedMappingIdentifier}({Helpers.InstanceIdentifier}, this);");
                }
                else
                {
                    WriteLine($"{Helpers.InstanceField} = native;");
                }
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private void Generate__CopyValue(Class @class, string @internal)
        {
            using (WriteBlock($"private static void* __CopyValue({@internal} native)"))
            {
                var copyCtorMethod = @class.Methods.FirstOrDefault(method => method.IsCopyConstructor);

                if (@class.HasNonTrivialCopyConstructor && copyCtorMethod != null && copyCtorMethod.IsGenerated)
                {
                    // Allocate memory for a new native object and call the ctor.
                    var printed = TypePrinter.PrintNative(@class);
                    string defaultValue = string.Empty;
                    if (copyCtorMethod.Parameters.Count > 1)
                        defaultValue = $", {ExpressionPrinter.VisitParameter(copyCtorMethod.Parameters.Last())}";
                    WriteLine($@"var ret = Marshal.AllocHGlobal(sizeof({@internal}));");
                    WriteLine($@"{printed}.{GetFunctionNativeIdentifier(copyCtorMethod)}(ret, new {TypePrinter.IntPtrType}(&native){defaultValue});",
                        printed, GetFunctionNativeIdentifier(copyCtorMethod));
                    WriteLine("return ret.ToPointer();");
                }
                else
                {
                    WriteLine($"var ret = Marshal.AllocHGlobal(sizeof({@internal}));");
                    WriteLine($"*({@internal}*) ret = native;");
                    WriteLine("return ret.ToPointer();");
                }
            }
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

            using (WriteBlock($"public static {function.OriginalReturnType} {functionName}({FormatMethodParameters(function.Parameters)})"))
            {
                if (function.SynthKind == FunctionSynthKind.DefaultValueOverload)
                    GenerateOverloadCall(function);
                else
                    GenerateInternalFunctionCall(function);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodSpecifier(Method method,
            MethodSpecifierKind? kind = null)
        {
            bool isTemplateMethod = method.Parameters.Any(
                p => p.Kind == ParameterKind.Extension);
            if (method.IsVirtual && !method.IsGeneratedOverride() &&
                !method.IsOperator && !method.IsPure && !isTemplateMethod)
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
            var printedType = method.OriginalReturnType.Visit(TypePrinter);
            var parameters = FormatMethodParameters(method.Parameters);

            if (method.IsConstructor || method.IsDestructor)
                Write($"{functionName}({parameters})");
            else if (method.ExplicitInterfaceImpl != null)
                Write($"{printedType} {method.ExplicitInterfaceImpl.Name}.{functionName}({parameters})");
            else if (method.OperatorKind == CXXOperatorKind.Conversion || method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                Write($"{functionName} {printedType}({parameters})");
            else
                Write($"{printedType} {functionName}({parameters})", printedType);
        }

        public void GenerateMethod(Method method, Class @class)
        {
            PushBlock(BlockKind.Method, method);
            GenerateDeclarationCommon(method);

            if (method.ExplicitInterfaceImpl == null)
            {
                Write(Helpers.GetAccess(method.Access));
            }

            GenerateMethodSpecifier(method);

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
                WriteLine(";");
                PopBlock(NewLineKind.BeforeNextBlock);
                return;
            }
            NewLine();

            if (method.Kind == CXXMethodKind.Constructor &&
                method.SynthKind != FunctionSynthKind.DefaultValueOverload)
            {
                var hasBase = @class.HasBaseClass;

                if (hasBase && !@class.IsValueType)
                    WriteLineIndent($": this({(method != null ? "(void*) null" : "native")})");

                if (@class.IsValueType && method.Parameters.Count > 0)
                    WriteLineIndent(": this()");
            }

            WriteOpenBraceAndIndent();

            if (method.IsProxy)
                goto SkipImpl;

            if (method.SynthKind == FunctionSynthKind.DefaultValueOverload)
            {
                if (!method.IsConstructor)
                    GenerateOverloadCall(method);
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
                    c, method, method.OriginalReturnType));
            }

        SkipImpl:

            UnindentAndWriteCloseBrace();

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                if (ShouldGenerateEqualsAndGetHashCode(@class, method))
                {
                    NewLine();
                    GenerateEquals(@class);
                    NewLine();
                    GenerateGetHashCode(@class);
                }
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private bool GenerateMethodBody(Class @class, Method method,
            QualifiedType returnType = default)
        {
            var specialization = @class as ClassTemplateSpecialization;
            if (specialization != null)
            {
                var specializedMethod = @class.Methods.FirstOrDefault(
                    m => m.InstantiatedFrom == (method.OriginalFunction ?? method));
                if (specializedMethod == null)
                {
                    WriteLine($@"throw new MissingMethodException(""Method {method.Name} missing from explicit specialization {@class.Visit(TypePrinter)}."");");
                    return false;
                }
                if (specializedMethod.Ignore)
                {
                    WriteLine($@"throw new MissingMethodException(""Method {method.Name} ignored in specialization {@class.Visit(TypePrinter)}."");");
                    return false;
                }

                method = specializedMethod;
            }
            if (@class.IsRefType)
            {
                if (method.IsConstructor)
                {
                    GenerateClassConstructor(method, @class);
                    return true;
                }
                if (method.IsOperator)
                {
                    GenerateOperator(method, returnType);
                }
                else if (method.IsVirtual)
                {
                    GenerateVirtualFunctionCall(method);
                }
                else if (method.IsDestructor)
                {
                    // It is possible that HasNonTrivialDestructor property different for specialization vs.
                    // the template. When we generate the Internal struct we only put a dtor there if the specialization
                    // has a non-trivial dtor. So we must make sure do the same test here.
                    if (@class.HasNonTrivialDestructor)
                        GenerateInternalFunctionCall(method, returnType: returnType);
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

            return method.OriginalReturnType.Type.Desugar().IsPrimitiveType(PrimitiveType.Void);
        }

        private string OverloadParamNameWithDefValue(Parameter p, ref int index)
        {
            return (p.Type.IsPointerToPrimitiveType() || p.Type.IsPointerToEnum()) && p.Usage == ParameterUsage.InOut && p.HasDefaultValue
                ? "ref param" + index++
                : ExpressionPrinter.VisitParameter(p);
        }

        private void GenerateOverloadCall(Function function)
        {
            if (function.OriginalFunction.GenerationKind == GenerationKind.Internal)
            {
                var property = ((Class)function.Namespace).Properties.First(
                    p => p.SetMethod == function.OriginalFunction);
                WriteLine($@"{property.Name} = {ExpressionPrinter.VisitParameter(
                    function.Parameters.First(p => p.Kind == ParameterKind.Regular))};");
                return;
            }

            for (int i = 0, j = 0; i < function.Parameters.Count; i++)
            {
                var parameter = function.Parameters[i];
                PrimitiveType primitiveType = PrimitiveType.Null;
                Enumeration enumeration = null;
                if (parameter.Kind == ParameterKind.Regular && parameter.Ignore &&
                        (parameter.Type.IsPointerToPrimitiveType(out primitiveType) ||
                        parameter.Type.IsPointerToEnum(out enumeration)) &&
                    parameter.Usage == ParameterUsage.InOut && parameter.HasDefaultValue)
                {
                    var pointeeType = ((PointerType)parameter.Type).Pointee.ToString();
                    WriteLine($@"{pointeeType} param{j++} = {(primitiveType == PrimitiveType.Bool ? "false" : $"({pointeeType})0")};");
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

        private bool ShouldGenerateEqualsAndGetHashCode(Class @class, Function method)
        {
            return method.Parameters[0].Type.SkipPointerRefs().TryGetClass(out Class leftHandSide)
                && leftHandSide.OriginalPtr == @class.OriginalPtr
                && method.Parameters[1].Type.SkipPointerRefs().TryGetClass(out Class rightHandSide)
                && rightHandSide.OriginalPtr == @class.OriginalPtr;
        }

        private void GenerateEquals(Class @class)
        {
            using (WriteBlock("public override bool Equals(object obj)"))
            {
                var printedClass = @class.Visit(TypePrinter);
                if (@class.IsRefType)
                    WriteLine($"return this == obj as {printedClass};");
                else
                {
                    WriteLine($"if (!(obj is {printedClass})) return false;");
                    WriteLine($"return this == ({printedClass}) obj;");
                }
            }
        }

        private void GenerateGetHashCode(Class @class)
        {
            using (WriteBlock("public override int GetHashCode()"))
            {
                if (!@class.IsRefType)
                    WriteLine($"return {Helpers.InstanceIdentifier}.GetHashCode();");
                else
                {
                    this.GenerateMember(@class, c =>
                    {
                        WriteLine($"if ({Helpers.InstanceIdentifier} == {TypePrinter.IntPtrType}.Zero)");
                        WriteLineIndent($"return {TypePrinter.IntPtrType}.Zero.GetHashCode();");
                        WriteLine($@"return (*({TypePrinter.PrintNative(c)}*) {Helpers.InstanceIdentifier}).GetHashCode();");
                        return false;
                    });
                }
            }
        }

        private void GenerateVirtualFunctionCall(Method method,
            bool forceVirtualCall = false)
        {
            if (!forceVirtualCall && method.IsGeneratedOverride() &&
                !method.BaseMethod.IsPure)
                GenerateManagedCall(method, true);
            else
                GenerateFunctionCall(GetVirtualCallDelegate(method), method);
        }

        private string GetVirtualCallDelegate(Method method)
        {
            Function @virtual = method;
            if (method.OriginalFunction != null &&
                !((Class)method.OriginalFunction.Namespace).IsInterface)
                @virtual = method.OriginalFunction;

            var i = VTables.GetVTableIndex(@virtual);
            int vtableIndex = 0;
            var @class = (Class)method.Namespace;
            var thisParam = method.Parameters.Find(
                p => p.Kind == ParameterKind.Extension);
            if (thisParam != null)
                @class = (Class)method.OriginalFunction.Namespace;

            if (Context.ParserOptions.IsMicrosoftAbi)
                vtableIndex = @class.Layout.VFTables.IndexOf(@class.Layout.VFTables.First(
                    v => v.Layout.Components.Any(c => c.Method == @virtual)));

            var @delegate = GetVTableMethodDelegateName(@virtual);
            var delegateId = Generator.GeneratedIdentifier(@delegate);

            int id = @class is ClassTemplateSpecialization specialization ? specialization.TemplatedDecl.Specializations.IndexOf(specialization) : 0;
            WriteLine($"var {delegateId} = {(thisParam != null ? $"{thisParam.Name}." : "")}__VTables.GetMethodDelegate<{method.FunctionType}>({vtableIndex}, {i}{(id > 0 ? $", {id}" : string.Empty)});");

            if (method.IsDestructor && @class.IsAbstract)
            {
                WriteLine("if ({0} != null)", delegateId);
                WriteOpenBraceAndIndent();
            }

            return delegateId;
        }

        private void GenerateOperator(Method method, QualifiedType returnType)
        {
            if (method.SynthKind == FunctionSynthKind.ComplementOperator)
            {
                Parameter parameter = method.Parameters[0];
                if (method.Kind == CXXMethodKind.Conversion)
                {
                    // To avoid ambiguity when having the multiple inheritance pass enabled
                    var paramType = parameter.Type.SkipPointerRefs().Desugar();
                    paramType = (paramType.GetPointee() ?? paramType).Desugar();
                    Class paramClass;
                    Class @interface = null;
                    if (paramType.TryGetClass(out paramClass))
                        @interface = paramClass.GetInterface();

                    var paramName = string.Format("{0}{1}",
                        !parameter.QualifiedType.IsConstRefToPrimitive() &&
                        parameter.Type.IsPrimitiveTypeConvertibleToRef() ?
                        "ref *" : string.Empty,
                        parameter.Name);
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

                    WriteLine("return !({0} {1} {2});", parameter.Name,
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
            var generateNativeToManaged = Options.GenerateNativeToManagedFor(@class);
            if (!generateNativeToManaged)
            {
                // if we don't have a NativeToManaged map, we can't do vtable hooking, because we can't
                // fetch the managed class from the native pointer. vtable hooking is required to allow C++
                // code to call virtual methods defined on a C++ class but overwritten in a C# class.
                // todo: throwing an exception at runtime is ugly, we should seal the class instead
                var typeFullName = TypePrinter.VisitClassDecl(@class).Type.Replace("global::", string.Empty);
                WriteLine($@"if (GetType().FullName != ""{typeFullName}"")");
                WriteLineIndent($@"throw new Exception(""{typeFullName}: Can't inherit from classes with disabled NativeToManaged map"");");
            }

            var @internal = TypePrinter.PrintNative(
                @class.IsAbstractImpl ? @class.BaseClass : @class);
            WriteLine($"{Helpers.InstanceIdentifier} = Marshal.AllocHGlobal(sizeof({@internal}));");
            WriteLine($"{Helpers.OwnsNativeInstanceIdentifier} = true;");

            if (generateNativeToManaged)
                WriteLine($"{Helpers.RecordNativeToManagedMappingIdentifier}({Helpers.InstanceIdentifier}, this);");

            if (method.IsCopyConstructor)
            {
                if (@class.HasNonTrivialCopyConstructor)
                    GenerateInternalFunctionCall(method);
                else
                {
                    var classInternal = TypePrinter.PrintNative(@class);
                    WriteLine($@"*(({classInternal}*) {Helpers.InstanceIdentifier}) = *(({classInternal}*) {method.Parameters[0].Name}.{Helpers.InstanceIdentifier});");

                    // Copy any string references owned by the source to the new instance so we
                    // don't have to ref count them.
                    // If there is no property or no setter then this instance can never own the native
                    // memory. Worry about the case where there's only a setter (write-only) when we
                    // understand the use case and how it can occur.
                    foreach (var prop in @class.GetConstCharFieldProperties())
                    {
                        WriteLine($"if ({method.Parameters[0].Name}.__{prop.Field.OriginalName}_OwnsNativeMemory)");
                        WriteLineIndent($"this.{prop.Name} = {method.Parameters[0].Name}.{prop.Name};");
                    }
                }
            }
            else
            {
                if (method.IsDefaultConstructor && Context.Options.ZeroAllocatedMemory(@class))
                    WriteLine($"global::System.Runtime.CompilerServices.Unsafe.InitBlock((void*){Helpers.InstanceIdentifier}, 0, (uint)sizeof({@internal}));");

                if (!method.IsDefaultConstructor || @class.HasNonTrivialDefaultConstructor)
                    GenerateInternalFunctionCall(method);
            }

            GenerateVTableClassSetupCall(@class);
        }

        public void GenerateInternalFunctionCall(Function function,
            QualifiedType returnType = default)
        {
            var @class = function.Namespace as Class;

            string @internal = Helpers.InternalStruct;
            if (@class is ClassTemplateSpecialization)
                @internal = TypePrinter.PrintNative(@class).Type;

            var nativeFunction = GetFunctionNativeIdentifier(function);
            var functionName = $"{@internal}.{nativeFunction}";
            GenerateFunctionCall(functionName, function, returnType);
        }

        public void GenerateFunctionCall(string functionName, Function function,
            QualifiedType returnType = default(QualifiedType))
        {
            // ignored functions may get here from interfaces for secondary bases
            if (function.Ignore)
            {
                WriteLine("throw new System.MissingMethodException(\"No C++ symbol to call.\");");
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
                var @class = (Class)method.Namespace;
                isValueType = @class.IsValueType;

                operatorParam = method.Parameters.FirstOrDefault(
                    p => p.Kind == ParameterKind.OperatorParameter);
                needsInstance = !method.IsStatic || operatorParam != null;
            }

            var @params = GenerateFunctionParamsMarshal(function.Parameters);

            var originalFunction = function.OriginalFunction ?? function;

            if (originalFunction.HasIndirectReturnTypeParameter)
            {
                var indirectRetType = originalFunction.Parameters.First(
                    parameter => parameter.Kind == ParameterKind.IndirectReturnType);

                Type type = indirectRetType.Type.Desugar();

                TypeMap typeMap;
                string construct = null;
                if (Context.TypeMaps.FindTypeMap(type, out typeMap))
                    construct = typeMap.CSharpConstruct();

                if (construct == null)
                {
                    Class retClass;
                    type.TryGetClass(out retClass);
                    var @class = retClass.OriginalClass ?? retClass;
                    WriteLine($@"var {Helpers.ReturnIdentifier} = new {TypePrinter.PrintNative(@class)}();");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(construct))
                    {
                        var typePrinterContext = new TypePrinterContext
                        {
                            Type = indirectRetType.Type.Desugar()
                        };

                        WriteLine("{0} {1};", typeMap.SignatureType(typePrinterContext),
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
                    names.Insert(instanceIndex, $"new {TypePrinter.IntPtrType}(__instancePtr)");
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
                    WriteLine($@"fixed ({Helpers.InternalStruct}{Helpers.GetSuffixForInternal((Class)originalFunction.Namespace)}* __instancePtr = &{Helpers.InstanceField})");
                    WriteOpenBraceAndIndent();
                }
                else
                {
                    WriteLine("var __instancePtr = &{0}.{1};", operatorParam.Name, Helpers.InstanceField);
                }
            }

            if (needsReturn && !originalFunction.HasIndirectReturnTypeParameter)
                Write("var {0} = ", Helpers.ReturnIdentifier);

            if (method != null && !method.IsConstructor && method.OriginalFunction != null &&
                ((Method)method.OriginalFunction).IsConstructor)
            {
                WriteLine($@"Marshal.AllocHGlobal({((Class)method.OriginalNamespace).Layout.Size});");
                names.Insert(0, Helpers.ReturnIdentifier);
            }
            WriteLine("{0}({1});", functionName, string.Join(", ", names));

            foreach (var param in @params)
            {
                if (param.Param.IsInOut && param.Param.Type.TryGetReferenceToPtrToClass(out var classType) && classType.TryGetClass(out var @class))
                {
                    var qualifiedClass = param.Param.Type.Visit(TypePrinter);
                    var get = Options.GenerateNativeToManagedFor(@class) ? "GetOr" : "";
                    WriteLine($"if ({param.Name} != {param.Param.Name}.__Instance)");
                    WriteLine($"{param.Param.Name} = {qualifiedClass}.__{get}CreateInstance(__{param.Name}, false);");
                }
            }

            foreach (TextGenerator cleanup in from p in @params
                                              select p.Context.Cleanup)
                Write(cleanup);

            if (needsReturn)
            {
                var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
                {
                    ArgName = Helpers.ReturnIdentifier,
                    ReturnVarName = Helpers.ReturnIdentifier,
                    ReturnType = returnType,
                    Function = function
                };

                var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
                retType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    Write(marshal.Context.Before);

                if (ctx.HasCodeBlock)
                    Indent();

                WriteLine($"return {marshal.Context.Return};");

                if (ctx.HasCodeBlock)
                    UnindentAndWriteCloseBrace();
            }

            if (needsFixedThis && operatorParam == null)
                UnindentAndWriteCloseBrace();

            var numFixedBlocks = @params.Count(param => param.HasUsingBlock);
            for (var i = 0; i < numFixedBlocks; ++i)
                UnindentAndWriteCloseBrace();
        }

        private string GetInstanceParam(Function function)
        {
            var from = (Class)function.Namespace;
            var to = (function.OriginalFunction == null ||
                // we don't need to offset the instance with Itanium if there's an existing interface impl
                (Context.ParserOptions.IsItaniumLikeAbi &&
                 !((Class)function.OriginalNamespace).IsInterface)) &&
                 function.SynthKind != FunctionSynthKind.AbstractImplCall ?
                @from.BaseClass : (Class)function.OriginalFunction.Namespace;

            var baseOffset = 0u;
            if (to != null)
            {
                to = to.OriginalClass ?? to;
                baseOffset = GetOffsetToBase(from, to);
            }
            bool isPrimaryBase = (from.BaseClass?.OriginalClass ?? from.BaseClass) == to;
            bool isOrHasSubclassAtNonZeroOffset =
                from.Layout.HasSubclassAtNonZeroOffset ||
                to?.Layout.HasSubclassAtNonZeroOffset == true;
            if (isPrimaryBase && isOrHasSubclassAtNonZeroOffset)
            {
                return Helpers.InstanceIdentifier +
                    (baseOffset == 0 ? " + " + Helpers.PrimaryBaseOffsetIdentifier : string.Empty);
            }
            return Helpers.InstanceIdentifier +
                (baseOffset == 0 ? string.Empty : " + " + baseOffset);
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

        public struct ParamMarshal
        {
            public string Name;
            public Parameter Param;
            public CSharpMarshalContext Context;
            public bool HasUsingBlock;
        }

        public List<ParamMarshal> GenerateFunctionParamsMarshal(IEnumerable<Parameter> @params)
        {
            var marshals = new List<ParamMarshal>();

            var paramIndex = 0;
            foreach (var param in @params)
            {
                if (param.Kind == ParameterKind.IndirectReturnType)
                    continue;

                marshals.Add(GenerateFunctionParamMarshal(param, paramIndex++));
            }

            return marshals;
        }

        private ParamMarshal GenerateFunctionParamMarshal(Parameter param, int paramIndex)
        {
            // Do not delete instance in MS ABI.
            var name = param.Name;
            var function = (Function)param.Namespace;
            param.Name = param.Kind == ParameterKind.ImplicitDestructorParameter ? "0" :
                ActiveBlock.Parent.Kind != BlockKind.Property ||
                function.OperatorKind == CXXOperatorKind.Subscript ? name : "value";

            var argName = Generator.GeneratedIdentifier("arg") + paramIndex.ToString(CultureInfo.InvariantCulture);
            var paramMarshal = new ParamMarshal { Name = argName, Param = param };

            if (param.IsOut)
            {
                var paramType = param.Type;

                Class @class;
                if ((paramType.GetFinalPointee() ?? paramType).Desugar().TryGetClass(out @class))
                {
                    var qualifiedIdentifier = (@class.OriginalClass ?? @class).Visit(TypePrinter);
                    WriteLine("{0} = new {1}();", name, qualifiedIdentifier);
                }
            }

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
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
                Indent();

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
                    : ((FunctionType)attributedType.Equivalent.Type).CallingConvention;
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var interopCallConv = callingConvention.ToInteropCallConv();
                if (interopCallConv == System.Runtime.InteropServices.CallingConvention.Winapi)
                    WriteLine("[SuppressUnmanagedCodeSecurity]");
                else
                    WriteLine(
                        "[SuppressUnmanagedCodeSecurity, " +
                        "UnmanagedFunctionPointer(__CallingConvention.{0})]",
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
            if (@enum.IsIncomplete || @enum.Ignore)
                return true;

            PushBlock(BlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            if (@enum.IsFlags)
                WriteLine("[Flags]");

            Write(Helpers.GetAccess(@enum.Access));
            // internal P/Invoke declarations must see protected enums
            if (@enum.Access == AccessSpecifier.Protected)
                Write("internal ");
            Write("enum {0}", @enum.Name);

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int &&
                @enum.BuiltinType.Type != PrimitiveType.Null)
                Write(" : {0}", typeName);

            NewLine();

            WriteOpenBraceAndIndent();
            GenerateEnumItems(@enum);
            UnindentAndWriteCloseBrace();

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
                    if (method.IsCopyConstructor)
                        identifier.Append("cctor");
                    else if (method.IsConstructor)
                        identifier.Append("ctor");
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
                .Where(f => (!f.Ignore ||
                    (f.OriginalFunction != null && !f.OriginalFunction.Ignore)) &&
                    (isForDelegate || internalParams.SequenceEqual(
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

            PushBlock(BlockKind.InternalsClassMethod);
            var callConv = function.CallingConvention.ToInteropCallConv();

            WriteLine("[SuppressUnmanagedCodeSecurity, DllImport(\"{0}\", EntryPoint = \"{1}\", CallingConvention = __CallingConvention.{2})]",
                GetLibraryOf(function),
                function.Mangled,
                callConv);

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
            Context.Symbols.FindLibraryBySymbol(((IMangledDecl)declaration).Mangled, out library);

            if (library != null)
                libName = Path.GetFileNameWithoutExtension(library.FileName);

            if (Options.StripLibPrefix && libName?.Length > 3 &&
                libName.StartsWith("lib", StringComparison.Ordinal))
                libName = libName.Substring(3);

            if (libName == null)
                libName = declaration.TranslationUnit.Module.SharedLibraryName;

            var targetTriple = Context.ParserOptions.TargetTriple;
            if (Options.GenerateInternalImports)
                libName = "__Internal";
            else if (targetTriple.IsWindows() &&
                libName.Contains('.') && Path.GetExtension(libName) != ".dll")
                libName += ".dll";

            if (targetTriple.IsMacOS())
            {
                var framework = libName + ".framework";
                foreach (var libDir in declaration.TranslationUnit.Module.LibraryDirs)
                {
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
}
