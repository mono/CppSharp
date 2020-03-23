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
                    WriteOpenBraceAndIndent();
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
                        WriteOpenBraceAndIndent();
                    }

                    GenerateClassTemplateSpecializationsInternals(
                        template.Key, template.ToList());

                    foreach (var declarationContext in declarationContexts)
                        UnindentAndWriteCloseBrace();
                }

                if (!string.IsNullOrEmpty(group.Key))
                    UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public virtual void GenerateUsings()
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
                WriteOpenBraceAndIndent();
            }

            var ret = base.VisitNamespace(@namespace);

            if (shouldGenerateNamespace)
            {
                UnindentAndWriteCloseBrace();
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
            var hasGlobalFunctions = !(context is Class) && context.Functions.Any(
                f => f.IsGenerated);

            var hasGlobalVariables = !(context is Class) && context.Variables.Any(
                v => v.IsGenerated && v.Access == AccessSpecifier.Public);

            if (!hasGlobalFunctions && !hasGlobalVariables)
                return;

            var parentName = SafeIdentifier(context.TranslationUnit.FileNameWithoutExtension);

            PushBlock(BlockKind.Functions);

            var keyword = "class";
            var classes = EnumerateClasses().ToList();
            if (classes.FindAll(cls => cls.IsValueType && cls.Name == parentName && context.QualifiedLogicalName == cls.Namespace.QualifiedLogicalName).Any())
                keyword = "struct";
            WriteLine($"public unsafe partial {keyword} {parentName}");
            WriteOpenBraceAndIndent();

            PushBlock(BlockKind.InternalsClass);
            GenerateClassInternalHead();
            WriteOpenBraceAndIndent();

            // Generate all the internal function declarations.
            foreach (var function in context.Functions)
            {
                if ((!function.IsGenerated && !function.IsInternal) || function.IsSynthetized)
                    continue;

                GenerateInternalFunction(function);
            }

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);

            foreach (Function function in context.Functions.Where(f => f.IsGenerated))
                GenerateFunction(function, parentName);

            foreach (var variable in context.Variables.Where(
                v => v.IsGenerated && v.Access == AccessSpecifier.Public))
                GenerateVariable(null, variable);

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassTemplateSpecializationInternal(Class classTemplate)
        {
            if (classTemplate.Specializations.Count == 0)
                return;

            GenerateClassTemplateSpecializationsInternals(classTemplate,
                classTemplate.Specializations);
        }

        private void GenerateClassTemplateSpecializationsInternals(Class classTemplate,
            IList<ClassTemplateSpecialization> specializations)
        {
            PushBlock(BlockKind.Namespace);
            var generated = GetGeneratedClasses(classTemplate, specializations);
            WriteLine("namespace {0}{1}",
                classTemplate.OriginalNamespace is Class &&
                !classTemplate.OriginalNamespace.IsDependent ?
                    classTemplate.OriginalNamespace.Name + '_' : string.Empty,
                classTemplate.Name);
            WriteOpenBraceAndIndent();

            foreach (var nestedTemplate in classTemplate.Classes.Where(
                c => c.IsDependent && !c.Ignore && c.Specializations.Any(s => !s.Ignore)))
                GenerateClassTemplateSpecializationsInternals(
                    nestedTemplate, nestedTemplate.Specializations);

            foreach (var specialization in generated.KeepSingleAllPointersSpecialization())
                GenerateClassInternals(specialization);

            foreach (var group in generated.SelectMany(s => s.Classes).Where(
                c => !c.IsIncomplete).GroupBy(c => c.Name))
            {
                var nested = classTemplate.Classes.FirstOrDefault(c => c.Name == group.Key);
                if (nested != null)
                    GenerateNestedInternals(group.Key, GetGeneratedClasses(nested, group));
            }

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateNestedInternals(string name, IEnumerable<Class> nestedClasses)
        {
            WriteLine($"namespace {name}");
            WriteOpenBraceAndIndent();
            foreach (var nestedClass in nestedClasses)
            {
                GenerateClassInternals(nestedClass);
                foreach (var nestedInNested in nestedClass.Classes)
                    GenerateNestedInternals(nestedInNested.Name, new[] { nestedInNested });
            }
            UnindentAndWriteCloseBrace();
            NewLine();
        }

        private IEnumerable<Class> GetGeneratedClasses(
            Class dependentClass, IEnumerable<Class> specializedClasses)
        {
            var specialization = specializedClasses.FirstOrDefault(s => s.IsGenerated) ??
                specializedClasses.First();

            if (dependentClass.HasDependentValueFieldInLayout())
                return specializedClasses;

            return new[] { specialization };
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
            if ((@class.IsIncomplete && !@class.IsOpaque) || @class.Ignore)
                return false;

            if (@class.IsInterface)
            {
                GenerateInterface(@class);
                return true;
            }

            if (!@class.IsDependent)
                foreach (var nestedTemplate in @class.Classes.Where(
                    c => !c.IsIncomplete && c.IsDependent))
                    GenerateClassTemplateSpecializationInternal(nestedTemplate);

            if (@class.IsTemplate)
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
                    WriteLine("private {0}.{1} {2};", @class.Name, Helpers.InternalStruct,
                        Helpers.InstanceField);
                    WriteLine("internal {0}.{1} {2} {{ get {{ return {3}; }} }}", @class.Name,
                        Helpers.InternalStruct, Helpers.InstanceIdentifier, Helpers.InstanceField);
                }
                else
                {
                    WriteLine("public {0} {1} {{ get; protected set; }}",
                        TypePrinter.IntPtrType, Helpers.InstanceIdentifier);

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
                    WriteLine("protected internal void*[] __OriginalVTables;");
                }
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            GenerateClassConstructors(@class);
            foreach (Function function in @class.Functions.Where(f => f.IsGenerated))
                GenerateFunction(function, @class.Name);
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

        private void GenerateInterface(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete)
                return;

            PushBlock(BlockKind.Interface);
            GenerateDeclarationCommon(@class);

            GenerateClassSpecifier(@class);

            if (@class.Bases.Count == 0)
                Write(" : IDisposable");

            NewLine();
            WriteOpenBraceAndIndent();

            foreach (var method in @class.Methods.Where(m =>
                (m.OriginalFunction == null ||
                 !ASTUtils.CheckIgnoreFunction(m.OriginalFunction)) &&
                m.Access == AccessSpecifier.Public))
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
                    type = ((PointerType) prop.Type).Pointee;
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

        public void GenerateClassInternals(Class @class)
        {
            PushBlock(BlockKind.InternalsClass);
            if (!Options.GenerateSequentialLayout || @class.IsUnion)
                WriteLine($"[StructLayout(LayoutKind.Explicit, Size = {@class.Layout.GetSize()})]");
            else if (@class.MaxFieldAlignment > 0)
                WriteLine($"[StructLayout(LayoutKind.Sequential, Pack = {@class.MaxFieldAlignment})]");

            GenerateClassInternalHead(@class);
            WriteOpenBraceAndIndent();

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

            UnindentAndWriteCloseBrace();
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
                foreach (var dtor in @class.Destructors.Where(d => !d.IsVirtual))
                    tryAddOverload(dtor);

            foreach (var method in @class.Methods)
            {
                if (!method.IsGenerated || ASTUtils.CheckIgnoreMethod(method))
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

            functions.AddRange(from function in @class.Functions
                               where function.IsGenerated && !function.IsSynthetized
                               select function);
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

            bool isSpecialization = false;
            DeclarationContext declContext = @class;
            while (declContext != null)
            {
                isSpecialization = declContext is ClassTemplateSpecialization;
                if (isSpecialization)
                    break;
                declContext = declContext.Namespace;
            }
            if (@class != null && @class.NeedsBase &&
                !@class.BaseClass.IsInterface && !isSpecialization)
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

            if (@class.IsGenerated && isBindingGen && @class.IsRefType && !@class.IsOpaque)
            {
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
            Write($"internal {retType}");
            if (field.Expression != null)
            {
                var fieldValuePrinted = field.Expression.CSharpValue(ExpressionPrinter);
                Write($" = {fieldValuePrinted}");
            }
            WriteLine(";");

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
                    WriteLine(";");
                    PopBlock(NewLineKind.BeforeNextBlock);
                    return;
                }

                NewLine();
                WriteOpenBraceAndIndent();

                this.GenerateMember(@class, c => GenerateFunctionSetter(c, property), true);
            }
            else if (decl is Variable)
            {
                NewLine();
                WriteOpenBraceAndIndent();
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
                WriteOpenBraceAndIndent();

                this.GenerateField(@class, field, GenerateFieldSetter, true);
            }
            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateVariableSetter(Variable var)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);

            var location = $@"CppSharp.SymbolResolver.ResolveSymbol(""{
                GetLibraryOf(var)}"", ""{var.Mangled}"")";

            string ptr = Generator.GeneratedIdentifier("ptr");
            Type varType = var.Type.Desugar();
            var arrayType = varType as ArrayType;
            var @class = var.Namespace as Class;
            var isRefTypeArray = arrayType != null && @class != null && @class.IsRefType;
            if (isRefTypeArray)
                WriteLine($@"var {ptr} = {
                    (arrayType.Type.IsPrimitiveType(PrimitiveType.Char) &&
                     arrayType.QualifiedType.Qualifiers.IsConst ?
                        string.Empty : "(byte*)")}{location};");
            else
                WriteLine($"var {ptr} = ({varType}*){location};");

            TypePrinter.PopContext();

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = var.QualifiedType
            };

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                Parameter = param,
                ArgName = param.Name,
                ReturnType = new QualifiedType(varType)
            };
            ctx.PushMarshalKind(MarshalKind.Variable);

            var marshal = new CSharpMarshalManagedToNativePrinter(ctx);
            var.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                Indent();

            WriteLine($@"*{ptr} = {marshal.Context.ArgumentPrefix}{
                marshal.Context.Return};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();
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

            if (property.SetMethod.OperatorKind == CXXOperatorKind.Subscript)
                GenerateIndexerSetter(property.SetMethod);
            else
                GenerateFunctionInProperty(@class, property.SetMethod, actualProperty,
                    new QualifiedType(new BuiltinType(PrimitiveType.Void)));
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
                var name = ((Class) field.Namespace).Layout.Fields.First(
                    f => f.FieldPtr == field.OriginalPtr).Name;
                if (@class.IsValueType)
                    returnVar = $"{Helpers.InstanceField}.{name}";
                else
                    returnVar = $"(({TypePrinter.PrintNative(@class)}*){Helpers.InstanceIdentifier})->{name}";
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
            string call = $@"{@internal}.{internalFunction}({
                GetInstanceParam(function)}, {paramMarshal.Context.ArgumentPrefix}{paramMarshal.Name})";
            if (type.IsPrimitiveType())
            {
                WriteLine($"*{call} = {marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
            }
            else
            {
                Class @class;
                if (type.TryGetClass(out @class) && @class.HasNonTrivialCopyConstructor)
                {
                    Method cctor = @class.Methods.First(c => c.IsCopyConstructor);
                    WriteLine($@"{TypePrinter.PrintNative(type)}.{
                        GetFunctionNativeIdentifier(cctor)}({call}, {marshal.Context.Return});");
                }
                else
                {
                    WriteLine($@"*({TypePrinter.PrintNative(type)}*) {call} = {
                        marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
                }
            }
            if (paramMarshal.HasUsingBlock)
                UnindentAndWriteCloseBrace();

            if (ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();
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
                var to = ((Class) property.OriginalNamespace).OriginalClass;
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

        private void GenerateVariableGetter(Variable var)
        {
            TypePrinter.PushContext(TypePrinterContextKind.Native);

            string library = GetLibraryOf(var);
            var location = $"CppSharp.SymbolResolver.ResolveSymbol(\"{library}\", \"{var.Mangled}\")";

            var ptr = Generator.GeneratedIdentifier("ptr");

            Type varType = var.Type.Desugar();
            var arrayType = varType as ArrayType;
            var elementType = arrayType?.Type.Desugar();
            var @class = var.Namespace as Class;
            var isRefTypeArray = arrayType != null && @class != null && @class.IsRefType;
            if (isRefTypeArray)
            {
                string cast = elementType.IsPrimitiveType(PrimitiveType.Char) &&
                    arrayType.QualifiedType.Qualifiers.IsConst
                        ? string.Empty : "(byte*)";
                WriteLine($"var {ptr} = {cast}{location};");
            }
            else
            {
                TypePrinter.PushMarshalKind(MarshalKind.ReturnVariableArray);
                var varReturnType = varType.Visit(TypePrinter);
                TypePrinter.PopMarshalKind();
                WriteLine($"var {ptr} = ({varReturnType}*){location};");
            }

            TypePrinter.PopContext();

            var ctx = new CSharpMarshalContext(Context, CurrentIndentation)
            {
                ArgName = var.Name,
                ReturnType = new QualifiedType(var.Type)
            };
            ctx.PushMarshalKind(MarshalKind.ReturnVariableArray);

            if (!isRefTypeArray && elementType == null)
                ctx.ReturnVarName = $"*{ptr}";
            else if (elementType == null || elementType.IsPrimitiveType() ||
                arrayType.SizeType == ArrayType.ArraySize.Constant)
                ctx.ReturnVarName = ptr;
            else
                ctx.ReturnVarName = $@"{elementType}.{
                    Helpers.CreateInstanceIdentifier}(new {
                        TypePrinter.IntPtrType}({ptr}))";

            var marshal = new CSharpMarshalNativeToManagedPrinter(ctx);
            var.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                Write(marshal.Context.Before);

            if (ctx.HasCodeBlock)
                Indent();

            WriteLine("return {0};", marshal.Context.Return);

            if (ctx.HasCodeBlock)
                UnindentAndWriteCloseBrace();
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
            GenerateFunctionInProperty(@class, actualProperty.GetMethod, actualProperty,
                property.QualifiedType);
        }

        private static Property GetActualProperty(Property property, Class c)
        {
            if (!(c is ClassTemplateSpecialization))
                return property;
            return c.Properties.SingleOrDefault(p => p.GetMethod != null &&
                p.GetMethod.InstantiatedFrom == property.GetMethod);
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
            var name = ((Class) field.Namespace).Layout.Fields.First(
                f => f.FieldPtr == field.OriginalPtr).Name;
            string returnVar;
            var arrayType = field.Type.Desugar() as ArrayType;
            if (@class.IsValueType)
            {
                if (arrayType != null)
                    returnVar = HandleValueArray(arrayType, field);
                else
                    returnVar = $"{Helpers.InstanceField}.{name}";
            }
            else
            {
                returnVar = $"(({TypePrinter.PrintNative(@class)}*) {Helpers.InstanceIdentifier})->{name}";
                // Class field getter should return a reference object instead of a copy. Wrapping `returnVar` in
                // IntPtr ensures that non-copying object constructor is invoked.
                Class typeClass;
                if (field.Type.TryGetClass(out typeClass) && !typeClass.IsValueType &&
                    !ASTUtils.IsMappedToPrimitive(Context.TypeMaps, field.Type))
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
            if (field.Type.IsPointer())
            {
                var final = field.Type.GetFinalPointee().Desugar(resolveTemplateSubstitution: false);
                var templateSubstitution = final as TemplateParameterSubstitutionType;
                if (templateSubstitution != null && returnType.Type.IsDependent)
                    Write($"({templateSubstitution.ReplacedParameter.Parameter.Name}) (object) ");
                if ((final.IsPrimitiveType() && !final.IsPrimitiveType(PrimitiveType.Void) &&
                    (!final.IsPrimitiveType(PrimitiveType.Char) &&
                     !final.IsPrimitiveType(PrimitiveType.WideChar) ||
                     (!Context.Options.MarshalCharAsManagedChar &&
                      !((PointerType) field.Type).QualifiedPointee.Qualifiers.IsConst)) &&
                    templateSubstitution == null) ||
                    (!((PointerType) field.Type).QualifiedPointee.Qualifiers.IsConst &&
                     final.IsPrimitiveType(PrimitiveType.WideChar)))
                    Write($"({field.Type.GetPointee().Desugar()}*) ");
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
                    WriteLine($@"{printedType} {
                        prop.ExplicitInterfaceImpl.Name}.{GetPropertyName(prop)}");
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
            TypePrinter.PushMarshalKind(MarshalKind.ReturnVariableArray);
            var variableType = variable.Type.Visit(TypePrinter);
            TypePrinter.PopMarshalKind();
            WriteLine($"public static {variableType} {variable.Name}");
            WriteOpenBraceAndIndent();

            GeneratePropertyGetter(variable, @class);

            if (!variable.QualifiedType.Qualifiers.IsConst &&
                !(variable.Type.Desugar() is ArrayType))
                GeneratePropertySetter(variable, @class);

            UnindentAndWriteCloseBrace();
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
                GenerateVTableMethodDelegates(containingClass, method.Namespace.IsDependent ?
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
            WriteOpenBraceAndIndent();

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
            WriteOpenBraceAndIndent();
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
            UnindentAndWriteCloseBrace();

            NewLine();

            if (hasVirtualDtor)
            {
                WriteLine("if ({0})", destructorOnly);
                WriteOpenBraceAndIndent();
                WriteLine("if (__ManagedVTablesDtorOnly == null)");
                WriteOpenBraceAndIndent();

                AllocateNewVTables(@class, wrappedEntries, true);

                UnindentAndWriteCloseBrace();
                WriteLine("else");
                WriteOpenBraceAndIndent();
            }
            WriteLine("if (__ManagedVTables == null)");
            WriteOpenBraceAndIndent();

            AllocateNewVTables(@class, wrappedEntries, false);

            if (hasVirtualDtor)
                UnindentAndWriteCloseBrace();

            UnindentAndWriteCloseBrace();
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

            UnindentAndWriteCloseBrace();
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

            UnindentAndWriteCloseBrace();
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
            var property = ((Class) method.Namespace).Properties.Find(
                p => p.GetMethod == method || p.SetMethod == method);
            bool isSetter = property != null && property.SetMethod == method;
            var hasReturn = !isVoid && !isSetter;

            if (hasReturn)
                Write(isPrimitive && !isSetter ? "return " : $"var {Helpers.ReturnIdentifier} = ");

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
            if (isPrimitive && !isSetter)
                return;

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
                    WriteLine($@"*({TypePrinter.PrintNative(method.OriginalReturnType)}*) {
                        retParam.Name} = {marshal.Context.ArgumentPrefix}{marshal.Context.Return};");
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
            WriteOpenBraceAndIndent();

            WriteLine($"if (!NativeToManagedMap.ContainsKey({Helpers.InstanceField}))");
            WriteLineIndent("throw new global::System.Exception(\"No managed instance was found\");");
            NewLine();

            var printedClass = @class.Visit(TypePrinter);
            WriteLine($@"var {Helpers.TargetIdentifier} = ({
                printedClass}) NativeToManagedMap[{Helpers.InstanceField}];");
            WriteLine("if ({0}.{1})", Helpers.TargetIdentifier, Helpers.OwnsNativeInstanceIdentifier);
            WriteLineIndent("{0}.SetupVTables();", Helpers.TargetIdentifier);
            GenerateVTableManagedCall(method);

            UnindentAndWriteCloseBrace();

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
            WriteOpenBraceAndIndent();
            WriteLine("Dispose(false);");
            UnindentAndWriteCloseBrace();

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
                WriteOpenBraceAndIndent();

                WriteLine("Dispose(disposing: true);");
                if (Options.GenerateFinalizers)
                    WriteLine("GC.SuppressFinalize(this);");

                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate Dispose(bool) method
            PushBlock(BlockKind.Method);
            Write("public ");
            if (!@class.IsValueType)
                Write(hasBaseClass ? "override " : "virtual ");

            WriteLine("void Dispose(bool disposing)");
            WriteOpenBraceAndIndent();

            if (@class.IsRefType)
            {
                var @base = @class.GetNonIgnoredRootBase();

                // Use interfaces if any - derived types with a this class as a secondary base, must be compatible with the map
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
                @class.HasNonTrivialDestructor && !@class.IsAbstract)
            {
                NativeLibrary library;
                if (!Options.CheckSymbols ||
                    Context.Symbols.FindLibraryBySymbol(dtor.Mangled, out library))
                {
                    WriteLine("if (disposing)");
                    if (@class.IsDependent || dtor.IsVirtual)
                        WriteOpenBraceAndIndent();
                    else
                        Indent();
                    if (dtor.IsVirtual)
                        this.GenerateMember(@class, c => GenerateDestructorCall(
                            c is ClassTemplateSpecialization ?
                                c.Methods.First(m => m.InstantiatedFrom == dtor) : dtor), true);
                    else
                        this.GenerateMember(@class, c => GenerateMethodBody(c, dtor), true);
                    if (@class.IsDependent || dtor.IsVirtual)
                        UnindentAndWriteCloseBrace();
                    else
                        Unindent();
                }
            }

            WriteLine("if ({0})", Helpers.OwnsNativeInstanceIdentifier);
            WriteLineIndent("Marshal.FreeHGlobal({0});", Helpers.InstanceIdentifier);

            WriteLine("{0} = IntPtr.Zero;", Helpers.InstanceIdentifier);
            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateDestructorCall(Method dtor)
        {
            var @class = (Class) dtor.Namespace;
            GenerateVirtualFunctionCall(dtor, true);
            if (@class.IsAbstract)
            {
                UnindentAndWriteCloseBrace();
                WriteLine("else");
                Indent();
                GenerateInternalFunctionCall(dtor);
                Unindent();
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
                WriteOpenBraceAndIndent();
                var suffix = @class.IsAbstract ? "Internal" : string.Empty;
                var ctorCall = $"{printedClass}{suffix}";
                WriteLine("return new {0}(native.ToPointer(), skipVTables);", ctorCall);
                UnindentAndWriteCloseBrace();
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

            WriteOpenBraceAndIndent();

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
                    Indent();
                }

                if (@class.IsAbstractImpl || hasVTables)
                    SaveOriginalVTablePointers(@class);

                if (setupVTables)
                {
                    Unindent();
                    WriteLine("else");
                    Indent();
                    GenerateVTableClassSetupCall(@class, destructorOnly: true);
                    Unindent();
                }
            }
            else
            {
                WriteLine($"{Helpers.InstanceField} = *({TypePrinter.PrintNative(@class)}*) native;");
            }

            UnindentAndWriteCloseBrace();
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
                WriteOpenBraceAndIndent();
                var suffix = @class.IsAbstract ? "Internal" : "";
                WriteLine($"return new {returnType}{suffix}(native, skipVTables);");
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (@class.IsRefType && !@class.IsAbstract)
            {
                PushBlock(BlockKind.Method);
                WriteLine($"private static void* __CopyValue({@internal} native)");
                WriteOpenBraceAndIndent();
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
                UnindentAndWriteCloseBrace();
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
                    WriteLine($"NativeToManagedMap[{Helpers.InstanceIdentifier}] = this;");
                }
                else
                {
                    WriteLine($"{Helpers.InstanceField} = native;");
                }
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        private void GenerateClassConstructorBase(Class @class, Method method)
        {
            var hasBase = @class.HasBaseClass;

            if (hasBase && !@class.IsValueType)
            {
                Indent();
                Write(": this(");

                Write(method != null ? "(void*) null" : "native");

                WriteLine(")");
                Unindent();
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
            WriteOpenBraceAndIndent();

            if (function.SynthKind == FunctionSynthKind.DefaultValueOverload)
                GenerateOverloadCall(function);
            else if (function.IsOperator)
                GenerateOperator(function, default(QualifiedType));
            else
                GenerateInternalFunctionCall(function);

            UnindentAndWriteCloseBrace();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodSpecifier(Method method, Class @class)
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
                WriteLine(";");
                PopBlock(NewLineKind.BeforeNextBlock);
                return;
            }
            NewLine();

            if (method.Kind == CXXMethodKind.Constructor &&
                method.SynthKind != FunctionSynthKind.DefaultValueOverload)
                GenerateClassConstructorBase(@class, method);

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
                    c, method, method.OriginalReturnType), isVoid);
            }

            SkipImpl:

            UnindentAndWriteCloseBrace();

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
                if (specializedMethod == null)
                {
                    WriteLine($@"throw new MissingMethodException(""Method {
                        method.Name} missing from explicit specialization {
                        @class.Visit(TypePrinter)}."");");
                    return;
                }
                if (specializedMethod.Ignore)
                {
                    WriteLine($@"throw new MissingMethodException(""Method {
                        method.Name} ignored in specialization {
                        @class.Visit(TypePrinter)}."");");
                    return;
                }

                method = specializedMethod;
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
                    GenerateVirtualFunctionCall(method);
                }
                else if (method.IsVirtual)
                {
                    GenerateVirtualFunctionCall(method);
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
            return p.Type.IsPointerToPrimitiveType() && p.Usage == ParameterUsage.InOut && p.HasDefaultValue
                ? "ref param" + index++
                : ExpressionPrinter.VisitParameter(p);
        }

        private void GenerateOverloadCall(Function function)
        {
            if (function.OriginalFunction.GenerationKind == GenerationKind.Internal)
            {
                var property = ((Class) function.Namespace).Properties.First(
                    p => p.SetMethod == function.OriginalFunction);
                WriteLine($@"{property.Name} = {ExpressionPrinter.VisitParameter(
                    function.Parameters.First(p => p.Kind == ParameterKind.Regular))};");
                return;
            }

            for (int i = 0, j = 0; i < function.Parameters.Count; i++)
            {
                var parameter = function.Parameters[i];
                PrimitiveType primitiveType;
                if (parameter.Kind == ParameterKind.Regular && parameter.Ignore &&
                    parameter.Type.IsPointerToPrimitiveType(out primitiveType) &&
                    parameter.Usage == ParameterUsage.InOut && parameter.HasDefaultValue)
                {
                    var pointeeType = ((PointerType) parameter.Type).Pointee.ToString();
                    WriteLine($@"{pointeeType} param{j++} = {
                        (primitiveType == PrimitiveType.Bool ? "false" : "0")};");
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
                WriteOpenBraceAndIndent();
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
                UnindentAndWriteCloseBrace();

                NewLine();

                WriteLine("public override int GetHashCode()");
                WriteOpenBraceAndIndent();
                if (@class.IsRefType)
                    this.GenerateMember(@class, GenerateGetHashCode);
                else
                    WriteLine($"return {Helpers.InstanceIdentifier}.GetHashCode();");
                UnindentAndWriteCloseBrace();
            }
        }

        private void GenerateGetHashCode(Class @class)
        {
            WriteLine($"if ({Helpers.InstanceIdentifier} == global::System.IntPtr.Zero)");
            WriteLineIndent("return global::System.IntPtr.Zero.GetHashCode();");
            WriteLine($@"return (*({TypePrinter.PrintNative(@class)}*) {
                Helpers.InstanceIdentifier}).GetHashCode();");
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
                !((Class) method.OriginalFunction.Namespace).IsInterface)
                @virtual = method.OriginalFunction;

            var i = VTables.GetVTableIndex(@virtual);
            int vtableIndex = 0;
            var @class = (Class) method.Namespace;
            var thisParam = method.Parameters.Find(
                p => p.Kind == ParameterKind.Extension);
            if (thisParam != null)
                @class = (Class) method.OriginalFunction.Namespace;

            if (Context.ParserOptions.IsMicrosoftAbi)
                vtableIndex = @class.Layout.VFTables.IndexOf(@class.Layout.VFTables.Where(
                    v => v.Layout.Components.Any(c => c.Method == @virtual)).First());

            WriteLine($@"var {Helpers.SlotIdentifier} = *(void**) ((IntPtr) {
                (thisParam != null ? $"{thisParam.Name}."
                : string.Empty)}__OriginalVTables[{vtableIndex}] + {i} * {
                Context.TargetInfo.PointerWidth / 8});");
            if (method.IsDestructor && @class.IsAbstract)
            {
                WriteLine("if ({0} != null)", Helpers.SlotIdentifier);
                WriteOpenBraceAndIndent();
            }

            var @delegate = GetVTableMethodDelegateName(@virtual);
            var delegateId = Generator.GeneratedIdentifier(@delegate);

            WriteLine("var {0} = ({1}) Marshal.GetDelegateForFunctionPointer(new IntPtr({2}), typeof({1}));",
                delegateId, method.FunctionType.ToString(),
                Helpers.SlotIdentifier);

            return delegateId;
        }

        private void GenerateOperator(Function function, QualifiedType returnType)
        {
            if (function.SynthKind == FunctionSynthKind.ComplementOperator)
            {
                if (function is Method method && method.Kind == CXXMethodKind.Conversion)
                {
                    // To avoid ambiguity when having the multiple inheritance pass enabled
                    var paramType = function.Parameters[0].Type.SkipPointerRefs().Desugar();
                    paramType = (paramType.GetPointee() ?? paramType).Desugar();
                    Class paramClass;
                    Class @interface = null;
                    if (paramType.TryGetClass(out paramClass))
                        @interface = paramClass.GetInterface();

                    var paramName = string.Format("{0}{1}",
                        function.Parameters[0].Type.IsPrimitiveTypeConvertibleToRef() ?
                        "ref *" : string.Empty,
                        function.Parameters[0].Name);
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
                    var @operator = Operators.GetOperatorOverloadPair(function.OperatorKind);

                    // handle operators for comparison which return int instead of bool
                    Type retType = function.OriginalReturnType.Type.Desugar();
                    bool regular = retType.IsPrimitiveType(PrimitiveType.Bool);
                    if (regular)
                    {
                        WriteLine($@"return !({function.Parameters[0].Name} {
                            @operator} {function.Parameters[1].Name});");
                    }
                    else
                    {
                        WriteLine($@"return global::System.Convert.ToInt32(({
                            function.Parameters[0].Name} {@operator} {
                            function.Parameters[1].Name}) == 0);");
                    }
                }
                return;
            }

            if (function.OperatorKind == CXXOperatorKind.EqualEqual ||
                function.OperatorKind == CXXOperatorKind.ExclaimEqual)
            {
                WriteLine("bool {0}Null = ReferenceEquals({0}, null);",
                    function.Parameters[0].Name);
                WriteLine("bool {0}Null = ReferenceEquals({0}, null);",
                    function.Parameters[1].Name);
                WriteLine("if ({0}Null || {1}Null)",
                    function.Parameters[0].Name, function.Parameters[1].Name);
                Type retType = function.OriginalReturnType.Type.Desugar();
                bool regular = retType.IsPrimitiveType(PrimitiveType.Bool);
                WriteLineIndent($@"return {(regular ? string.Empty : "global::System.Convert.ToInt32(")}{
                    (function.OperatorKind == CXXOperatorKind.EqualEqual ? string.Empty : "!(")}{
                    function.Parameters[0].Name}Null && {function.Parameters[1].Name}Null{
                    (function.OperatorKind == CXXOperatorKind.EqualEqual ? string.Empty : ")")}{
                    (regular ? string.Empty : ")")};");
            }

            GenerateInternalFunctionCall(function, returnType: returnType);
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
            QualifiedType returnType = default(QualifiedType))
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
            if (function.IsPure)
            {
                WriteLine("throw new System.NotImplementedException();");
                return;
            }

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
                var @class = (Class) method.Namespace;
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

                        WriteLine("{0} {1};", typeMap.CSharpSignatureType(typePrinterContext),
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
                ((Method) method.OriginalFunction).IsConstructor)
            {
                WriteLine($@"Marshal.AllocHGlobal({
                    ((Class) method.OriginalNamespace).Layout.GetSize()});");
                names.Insert(0, Helpers.ReturnIdentifier);
            }
            WriteLine("{0}({1});", functionName, string.Join(", ", names));

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
            for(var i = 0; i < numFixedBlocks; ++i)
                UnindentAndWriteCloseBrace();
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
            var function = (Function) param.Namespace;
            param.Name = param.Kind == ParameterKind.ImplicitDestructorParameter ? "0" :
                function.IsGenerated || function.OperatorKind == CXXOperatorKind.Subscript ?
                name : "value";

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
}
