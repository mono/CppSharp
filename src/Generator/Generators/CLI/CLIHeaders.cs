using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI header files.
    /// </summary>
    public class CLIHeaders : CLITemplate
    {
        public CLIHeaders(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override string FileExtension => "h";

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            WriteLine("#pragma once");
            NewLine();

            if (Options.OutputInteropIncludes)
                WriteLine("#include \"CppSharp.h\"");

            // Generate #include forward references.
            PushBlock(BlockKind.IncludesForwardReferences);
            WriteLine("#include <{0}>", TranslationUnit.IncludePath);
            GenerateIncludeForwardRefs();
            PopBlock(NewLineKind.BeforeNextBlock);
            PopBlock(NewLineKind.Always);

            // Generate namespace for forward references.
            PushBlock(BlockKind.ForwardReferences);
            GenerateForwardRefs();
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateNamespace(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateIncludeForwardRefs()
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Context.TypeMaps,
                Context.Options);
            typeReferenceCollector.Process(TranslationUnit, filterNamespaces: false);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                if (typeRef.Include.TranslationUnit == TranslationUnit)
                    continue;

                var filename = Context.Options.GenerateName != null ? $"{Context.Options.GenerateName(TranslationUnit)}{Path.GetExtension(TranslationUnit.FileName)}" : TranslationUnit.FileName;
                if (typeRef.Include.File == filename)
                    continue;

                var include = typeRef.Include;
                var unit = include.TranslationUnit;

                if (unit != null && !unit.IsDeclared)
                    continue;

                if (!string.IsNullOrEmpty(include.File) && include.InHeader)
                    includes.Add(include.ToString());
            }

            foreach (var include in includes)
                WriteLine(include);
        }

        private Namespace FindCreateNamespace(Namespace @namespace, Declaration decl)
        {
            if (decl.Namespace is TranslationUnit)
                return @namespace;

            var childNamespaces = decl.Namespace.GatherParentNamespaces();
            var currentNamespace = @namespace;

            foreach (var child in childNamespaces)
                currentNamespace = currentNamespace.FindCreateNamespace(child.Name);

            return currentNamespace;
        }

        public Namespace ConvertForwardReferencesToNamespaces(
            IEnumerable<CLITypeReference> typeReferences)
        {
            // Create a new tree of namespaces out of the type references found.
            var rootNamespace = new TranslationUnit();
            rootNamespace.Module = TranslationUnit.Module;

            var sortedRefs = typeReferences.ToList();
            sortedRefs.Sort((ref1, ref2) =>
                string.CompareOrdinal(ref1.FowardReference, ref2.FowardReference));

            var forwardRefs = new SortedSet<string>();

            foreach (var typeRef in sortedRefs)
            {
                if (string.IsNullOrWhiteSpace(typeRef.FowardReference))
                    continue;

                var declaration = typeRef.Declaration;
                if (!(declaration.Namespace is Namespace))
                    continue;

                if (!forwardRefs.Add(typeRef.FowardReference))
                    continue;

                if (typeRef.Include.InHeader)
                    continue;

                var @namespace = FindCreateNamespace(rootNamespace, declaration);
                @namespace.TypeReferences.Add(typeRef);
            }

            return rootNamespace;
        }

        public void GenerateForwardRefs()
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Context.TypeMaps,
                Context.Options);
            typeReferenceCollector.Process(TranslationUnit);

            var typeReferences = typeReferenceCollector.TypeReferences;
            var @namespace = ConvertForwardReferencesToNamespaces(typeReferences);

            GenerateNamespace(@namespace);
        }

        public void GenerateDeclContext(DeclarationContext decl)
        {
            // Generate all the type references for the module.
            foreach (var typeRef in decl.TypeReferences)
            {
                WriteLine(typeRef.FowardReference);
            }

            // Generate all the enum declarations for the module.
            foreach (var @enum in decl.Enums)
            {
                if (!@enum.IsGenerated || @enum.IsIncomplete)
                    continue;

                @enum.Visit(this);
            }

            // Generate all the typedef declarations for the module.
            GenerateTypedefs(decl);

            // Generate all the struct/class declarations for the module.
            foreach (var @class in decl.Classes)
            {
                if (!@class.IsGenerated || @class.IsIncomplete || @class.IsDependent)
                    continue;

                if (@class.IsOpaque)
                    continue;

                PushBlock(BlockKind.Class, @class);
                GenerateClass(@class);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (decl.Functions.Any(f => f.IsGenerated))
                GenerateFunctions(decl);

            foreach (var childNamespace in decl.Namespaces)
                GenerateNamespace(childNamespace);
        }

        public void GenerateNamespace(Namespace @namespace)
        {
            var isTopLevel = @namespace is TranslationUnit;
            var generateNamespace = !isTopLevel ||
                !string.IsNullOrEmpty(@namespace.TranslationUnit.Module.OutputNamespace);

            if (generateNamespace)
            {
                PushBlock(BlockKind.Namespace, @namespace);
                WriteLine("namespace {0}", isTopLevel
                                               ? @namespace.TranslationUnit.Module.OutputNamespace
                                               : @namespace.Name);
                WriteOpenBraceAndIndent();
            }

            GenerateDeclContext(@namespace);

            if (generateNamespace)
            {
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateTypedefs(DeclarationContext decl)
        {
            foreach (var typedef in decl.Typedefs)
            {
                if (!typedef.IsGenerated)
                    continue;

                GenerateTypedef(typedef);
            }
        }

        public void GenerateFunctions(DeclarationContext decl)
        {
            PushBlock(BlockKind.FunctionsClass, decl);

            WriteLine("public ref class {0}", Options.GenerateFreeStandingFunctionsClassName(TranslationUnit));
            WriteLine("{");
            WriteLine("public:");
            Indent();

            // Generate all the function declarations for the module.
            foreach (var function in decl.Functions)
            {
                GenerateFunction(function);
            }

            Unindent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClass(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete)
                return;

            GenerateDeclarationCommon(@class);

            GenerateClassSpecifier(@class);

            if (@class.IsOpaque)
            {
                WriteLine(";");
                return;
            }

            NewLine();
            WriteLine("{");
            WriteLine("public:");
            NewLine();

            // Process the nested types.
            Indent();
            GenerateDeclContext(@class);
            Unindent();

            string nativeType = $"{typePrinter.PrintTag(@class)}::{@class.QualifiedOriginalName}*";

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
                GenerateClassNativeField(nativeType);

            GenerateClassConstructors(@class, nativeType);

            GenerateClassProperties(@class);

            GenerateClassEvents(@class);
            GenerateClassMethods(@class.Methods);

            if (Options.GenerateFunctionTemplates)
                GenerateClassGenericMethods(@class);

            GenerateClassVariables(@class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                PushBlock(BlockKind.AccessSpecifier);
                WriteLine("protected:");
                PopBlock(NewLineKind.IfNotEmpty);

                PushBlock(BlockKind.Fields);
                WriteLineIndent("bool {0};", Helpers.OwnsNativeInstanceIdentifier);
                PopBlock();
            }

            PushBlock(BlockKind.AccessSpecifier);
            WriteLine("private:");
            var accBlock = PopBlock(NewLineKind.IfNotEmpty);

            PushBlock(BlockKind.Fields);
            GenerateClassFields(@class);
            var fieldsBlock = PopBlock();

            accBlock.CheckGenerate = () => !fieldsBlock.IsEmpty;

            WriteLine("};");
        }

        public void GenerateClassNativeField(string nativeType)
        {
            WriteLineIndent("property {0} NativePtr;", nativeType);

            Indent();
            WriteLine("property ::System::IntPtr {0}", Helpers.InstanceIdentifier);
            WriteOpenBraceAndIndent();
            WriteLine("virtual ::System::IntPtr get();");
            WriteLine("virtual void set(::System::IntPtr instance);");
            UnindentAndWriteCloseBrace();
            NewLine();

            Unindent();
        }

        public void GenerateClassGenericMethods(Class @class)
        {
            Indent();
            foreach (var template in @class.Templates)
            {
                if (!template.IsGenerated) continue;

                var functionTemplate = template as FunctionTemplate;
                if (functionTemplate == null) continue;

                PushBlock(BlockKind.Template);

                var function = functionTemplate.TemplatedFunction;

                var typePrinter = new CLITypePrinter(Context);
                typePrinter.PushContext(TypePrinterContextKind.Template);

                var retType = function.ReturnType.Visit(typePrinter);

                var typeNames = "";
                var paramNames = template.Parameters.Select(param => param.Name).ToList();
                if (paramNames.Any())
                    typeNames = "typename " + string.Join(", typename ", paramNames);

                Write("generic<{0}>", typeNames);

                // Process the generic type constraints
                var constraints = new List<string>();
                foreach (var param in template.Parameters.OfType<TypeTemplateParameter>())
                {
                    if (string.IsNullOrWhiteSpace(param.Constraint))
                        continue;
                    constraints.Add(string.Format("{0} : {1}", param.Name,
                        param.Constraint));
                }

                if (constraints.Any())
                    Write(" where {0}", string.Join(", ", constraints));

                NewLine();

                WriteLine("{0} {1}({2});", retType, function.Name,
                    GenerateParametersList(function.Parameters));

                PopBlock(NewLineKind.BeforeNextBlock);
            }
            Unindent();
        }

        public void GenerateClassConstructors(Class @class, string nativeType)
        {
            if (@class.IsStatic)
                return;

            Indent();

            // Output a default constructor that takes the native pointer.
            WriteLine("{0}({1} native);", @class.Name, nativeType);
            WriteLine("{0}({1} native, bool ownNativeInstance);", @class.Name, nativeType);
            WriteLine("static {0}^ {1}(::System::IntPtr native);",
                @class.Name, Helpers.CreateInstanceIdentifier);

            WriteLine($"static {@class.Name}^ {Helpers.CreateInstanceIdentifier}(::System::IntPtr native, bool {Helpers.OwnsNativeInstanceIdentifier});");

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor) || FunctionIgnored(ctor))
                    continue;

                // C++/CLI does not allow special member funtions for value types.
                if (@class.IsValueType && ctor.IsCopyConstructor)
                    continue;

                GenerateMethod(ctor);
            }

            if (@class.IsRefType)
            {
                var destructor = @class.Destructors
                    .FirstOrDefault(d => d.Parameters.Count == 0 && d.Access == AccessSpecifier.Public);
                if (destructor != null)
                {
                    GenerateClassDestructor(@class);
                    if (Options.GenerateFinalizerFor(@class))
                        GenerateClassFinalizer(@class);
                }
            }

            Unindent();
        }

        private void GenerateClassDestructor(Class @class)
        {
            PushBlock(BlockKind.Destructor);
            WriteLine("~{0}();", @class.Name);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassFinalizer(Class @class)
        {
            PushBlock(BlockKind.Finalizer);
            WriteLine("!{0}();", @class.Name);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClassFields(Class @class)
        {
            // Handle the case of struct (value-type) inheritance by adding the base
            // properties to the managed value subtypes.
            if (@class.IsValueType)
            {
                foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
                {
                    GenerateClassFields(@base.Class);
                }
            }

            Indent();
            // check for value types because some of the ignored fields may back properties;
            // not the case for ref types because the NativePtr pattern is used there
            foreach (var field in @class.Fields.Where(f => !ASTUtils.CheckIgnoreField(f)))
            {
                var property = @class.Properties.FirstOrDefault(p => p.Field == field);
                if (property != null && !property.IsInRefTypeAndBackedByValueClassField())
                {
                    GenerateField(@class, field);
                }
            }
            Unindent();
        }

        private void GenerateField(Class @class, Field field)
        {
            PushBlock(BlockKind.Field, field);

            GenerateDeclarationCommon(field);
            if (@class.IsUnion)
                WriteLine("[::System::Runtime::InteropServices::FieldOffset({0})]",
                    @class.Layout.Fields.Single(f => f.FieldPtr == field.OriginalPtr).Offset);
            WriteLine("{0} {1};", field.Type, field.Name);

            PopBlock();
        }

        public override void GenerateClassEvents(Class @class)
        {
            foreach (var @event in @class.Events)
            {
                if (!@event.IsGenerated) continue;

                typePrinter.PushContext(TypePrinterContextKind.Native);
                var cppArgs = typePrinter.VisitParameters(@event.Parameters, hasNames: true);
                typePrinter.PopContext();

                WriteLine("private:");
                Indent();

                var delegateName = string.Format("_{0}Delegate", @event.Name);
                WriteLine("delegate void {0}({1});", delegateName, cppArgs);
                WriteLine("{0}^ {0}Instance;", delegateName);

                WriteLine("void _{0}Raise({1});", @event.Name, cppArgs);
                WriteLine("{0} _{1};", @event.Type, @event.Name);

                Unindent();
                WriteLine("public:");
                Indent();

                WriteLine("event {0} {1}", @event.Type, @event.Name);
                WriteOpenBraceAndIndent();

                WriteLine("void add({0} evt);", @event.Type);
                WriteLine("void remove({0} evt);", @event.Type);

                var cliTypePrinter = new CLITypePrinter(Context);
                var cliArgs = cliTypePrinter.VisitParameters(@event.Parameters, hasNames: true);

                WriteLine("void raise({0});", cliArgs);
                UnindentAndWriteCloseBrace();
                Unindent();
            }
        }

        public void GenerateClassMethods(List<Method> methods)
        {
            if (methods.Count == 0)
                return;

            Indent();

            var @class = (Class)methods[0].Namespace;

            if (@class.IsValueType)
                foreach (var @base in @class.Bases.Where(b => b.IsClass && !b.Class.Ignore))
                    GenerateClassMethods(@base.Class.Methods.Where(m => !m.IsOperator).ToList());

            var staticMethods = new List<Method>();
            foreach (var method in methods)
            {
                if (ASTUtils.CheckIgnoreMethod(method) || FunctionIgnored(method))
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

            foreach (var method in staticMethods)
                GenerateMethod(method);

            Unindent();
        }

        public void GenerateClassVariables(Class @class)
        {
            Indent();

            foreach (var variable in @class.Variables)
            {
                if (!variable.IsGenerated) continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                var type = variable.Type;

                PushBlock(BlockKind.Variable);

                WriteLine("static property {0} {1}", type, variable.Name);

                WriteOpenBraceAndIndent();

                WriteLine("{0} get();", type);

                var arrayType = type as ArrayType;
                var qualifiedType = arrayType != null ? arrayType.QualifiedType : variable.QualifiedType;
                if (!qualifiedType.Qualifiers.IsConst)
                    WriteLine("void set({0});", type);

                UnindentAndWriteCloseBrace();

                PopBlock(NewLineKind.BeforeNextBlock);
            }

            Unindent();
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            if (@class.IsUnion)
                WriteLine("[::System::Runtime::InteropServices::StructLayout({0})]",
                          "::System::Runtime::InteropServices::LayoutKind::Explicit");

            // Nested types cannot have visibility modifiers in C++/CLI.
            var isTopLevel = @class.Namespace is TranslationUnit ||
                @class.Namespace is Namespace;
            if (isTopLevel)
                Write("public ");

            Write(@class.IsValueType ? "value struct " : "ref class ");

            Write("{0}", @class.Name);

            if (@class.IsStatic)
                Write(" abstract sealed");

            if (!@class.IsStatic)
            {
                if (@class.HasRefBase())
                    Write(" : {0}", QualifiedIdentifier(@class.Bases[0].Class));
                else if (@class.IsRefType)
                    Write(" : ICppInstance");
            }
        }

        public void GenerateClassProperties(Class @class)
        {
            // Handle the case of struct (value-type) inheritance by adding the base
            // properties to the managed value subtypes.
            if (@class.IsValueType)
            {
                foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class.IsDeclared))
                {
                    GenerateClassProperties(@base.Class);
                }
            }

            Indent();
            foreach (var prop in @class.Properties.Where(
                prop => !ASTUtils.CheckIgnoreProperty(prop) && !TypeIgnored(prop.Type)))
            {
                if (prop.IsInRefTypeAndBackedByValueClassField())
                {
                    GenerateField(@class, prop.Field);
                    continue;
                }

                GenerateDeclarationCommon(prop);
                GenerateProperty(prop);
            }
            Unindent();
        }

        public void GenerateIndexer(Property property)
        {
            var type = property.QualifiedType.Visit(CTypePrinter);
            var getter = property.GetMethod;
            var indexParameter = getter.Parameters[0];
            var indexParameterType = indexParameter.QualifiedType.Visit(CTypePrinter);

            WriteLine("property {0} default[{1}]", type, indexParameterType);
            WriteOpenBraceAndIndent();

            if (property.HasGetter)
                WriteLine("{0} get({1} {2});", type, indexParameterType, indexParameter.Name);

            if (property.HasSetter)
                WriteLine("void set({1} {2}, {0} value);", type, indexParameterType, indexParameter.Name);

            UnindentAndWriteCloseBrace();
        }

        public void GenerateProperty(Property property)
        {
            if (!(property.HasGetter || property.HasSetter) || TypeIgnored(property.Type))
                return;

            PushBlock(BlockKind.Property, property);
            var type = property.QualifiedType.Visit(CTypePrinter);

            if (property.IsStatic)
                Write("static ");

            if (property.IsIndexer)
            {
                GenerateIndexer(property);
            }
            else
            {
                WriteLine("property {0} {1}", type, property.Name);
                WriteOpenBraceAndIndent();

                if (property.HasGetter)
                    WriteLine("{0} get();", type);

                if (property.HasSetter)
                    WriteLine("void set({0});", type);

                UnindentAndWriteCloseBrace();
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override void GenerateMethodSpecifier(Method method,
            MethodSpecifierKind? kind = null)
        {
            if ((method.IsVirtual || method.IsOverride) && !method.IsOperator)
                Write("virtual ");

            var isBuiltinOperator = method.IsOperator &&
                Operators.IsBuiltinOperator(method.OperatorKind);

            if (method.IsStatic || isBuiltinOperator)
                Write("static ");

            if (method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                Write("explicit ");

            if (method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                Write("{0}(", GetMethodName(method));
            else
                Write("{0} {1}(", method.ReturnType, method.Name);

            GenerateMethodParameters(method);

            Write(")");

            if (method.IsGeneratedOverride())
                Write(" override");
        }

        public void GenerateMethod(Method method)
        {
            if (ASTUtils.CheckIgnoreMethod(method) || FunctionIgnored(method)) return;

            PushBlock(BlockKind.Method, method);
            GenerateDeclarationCommon(method);

            GenerateMethodSpecifier(method);
            WriteLine(";");

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                GenerateEquals(method, (Class)method.Namespace);
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
                WriteLine("virtual bool Equals(::System::Object^ obj) override;");
            }
        }

        public bool GenerateTypedef(TypedefNameDecl typedef)
        {
            if (!typedef.IsGenerated)
                return false;

            var functionType = typedef.Type as FunctionType;
            if (functionType != null || typedef.Type.IsPointerTo(out functionType))
            {
                PushBlock(BlockKind.Typedef, typedef);
                GenerateDeclarationCommon(typedef);

                var insideClass = typedef.Namespace is Class;

                var attributedType = typedef.Type.GetPointee().Desugar();
                var callingConvention = attributedType == null && functionType != null
                    ? functionType.CallingConvention
                    : ((FunctionType)attributedType).CallingConvention;
                var interopCallConv = callingConvention.ToInteropCallConv();
                if (interopCallConv != System.Runtime.InteropServices.CallingConvention.Winapi)
                    WriteLine("[::System::Runtime::InteropServices::UnmanagedFunctionPointer" +
                        "(::System::Runtime::InteropServices::CallingConvention::{0})] ",
                        interopCallConv);

                var visibility = !insideClass ? "public " : string.Empty;
                var result = CTypePrinter.VisitDelegate(functionType);
                result.Name = typedef.Name;
                WriteLine($"{visibility}{result};");

                PopBlock(NewLineKind.BeforeNextBlock);

                return true;
            }

            return false;
        }

        public void GenerateFunction(Function function)
        {
            if (!function.IsGenerated || FunctionIgnored(function))
                return;

            PushBlock(BlockKind.Function, function);

            GenerateDeclarationCommon(function);

            var retType = function.ReturnType.ToString();
            Write("static {0} {1}(", retType, function.Name);

            Write(GenerateParametersList(function.Parameters));

            WriteLine(");");

            PopBlock();
        }

        public static bool FunctionIgnored(Function function)
        {
            return TypeIgnored(function.ReturnType.Type) ||
                function.Parameters.Any(param => TypeIgnored(param.Type)) ||
                function is Method { IsConstructor: true, Parameters: { Count: 0 }, Namespace: Class { IsValueType: true } };
        }

        public static bool TypeIgnored(CppSharp.AST.Type type)
        {
            var desugared = type.Desugar();
            var finalType = (desugared.GetFinalPointee() ?? desugared).Desugar();
            Class @class;
            return finalType.TryGetClass(out @class) && @class.IsIncomplete;
        }
    }
}
