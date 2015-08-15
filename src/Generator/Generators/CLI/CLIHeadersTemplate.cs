using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI header files.
    /// </summary>
    public class CLIHeadersTemplate : CLITextTemplate
    {
        public override string FileExtension { get { return "h"; } }

        public CLIHeadersTemplate(Driver driver, IEnumerable<TranslationUnit> units)
            : base(driver, units)
        {
        }

        public override void Process()
        {
            PushBlock(BlockKind.Header);
            PopBlock();

            PushBlock(CLIBlockKind.Includes);
            WriteLine("#pragma once");
            NewLine();

            if (Options.OutputInteropIncludes)
                WriteLine("#include \"CppSharp.h\"");

            // Generate #include forward references.
            PushBlock(CLIBlockKind.IncludesForwardReferences);
            WriteLine("#include <{0}>", TranslationUnit.IncludePath);
            GenerateIncludeForwardRefs();
            PopBlock(NewLineKind.BeforeNextBlock);
            PopBlock(NewLineKind.Always);

            // Generate namespace for forward references.
            PushBlock(CLIBlockKind.ForwardReferences);
            GenerateForwardRefs();
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateNamespace(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateIncludeForwardRefs()
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase,
                Driver.Options);
            typeReferenceCollector.Process(TranslationUnit, filterNamespaces: false);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                if (typeRef.Include.TranslationUnit == TranslationUnit)
                    continue;

                if (typeRef.Include.File == TranslationUnit.FileName)
                    continue;

                var include = typeRef.Include;
                var unit = include.TranslationUnit;

                if (unit != null && !unit.IsDeclared)
                    continue;

                if(!string.IsNullOrEmpty(include.File) && include.InHeader)
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
            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase,
                Driver.Options);
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

                PushBlock(CLIBlockKind.Enum, @enum);
                GenerateEnum(@enum);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate all the typedef declarations for the module.
            GenerateTypedefs(decl);

            // Generate all the struct/class declarations for the module.
            foreach (var @class in decl.Classes)
            {
                if (!@class.IsGenerated || @class.IsIncomplete)
                    continue;

                if (@class.IsOpaque)
                    continue;

                PushBlock(CLIBlockKind.Class, @class);
                GenerateClass(@class);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (decl.HasFunctions)
                GenerateFunctions(decl);

            foreach (var childNamespace in decl.Namespaces)
                GenerateNamespace(childNamespace);
        }

        public void GenerateNamespace(Namespace @namespace)
        {
            var isTopLevel = @namespace is TranslationUnit;
            var generateNamespace = !isTopLevel || Options.GenerateLibraryNamespace;

            if (generateNamespace)
            {
                PushBlock(CLIBlockKind.Namespace, @namespace);
                WriteLine("namespace {0}", isTopLevel
                                               ? Options.OutputNamespace
                                               : @namespace.Name);
                WriteStartBraceIndent();
            }

            GenerateDeclContext(@namespace);

            if (generateNamespace)
            {
                WriteCloseBraceIndent();
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
            PushBlock(CLIBlockKind.FunctionsClass);

            WriteLine("public ref class {0}", TranslationUnit.FileNameWithoutExtension);
            WriteLine("{");
            WriteLine("public:");
            PushIndent();

            // Generate all the function declarations for the module.
            foreach (var function in decl.Functions)
            {
                GenerateFunction(function);
            }

            PopIndent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClass(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete)
                return;

            GenerateDeclarationCommon(@class);

            if (GenerateClassProlog(@class))
                return;

            // Process the nested types.
            PushIndent();
            GenerateDeclContext(@class);
            PopIndent();

            var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
                GenerateClassNativeField(@class, nativeType);

            GenerateClassConstructors(@class, nativeType);

            GenerateClassProperties(@class);

            GenerateClassEvents(@class);
            GenerateClassMethods(@class.Methods);

            if (Options.GenerateFunctionTemplates)
                GenerateClassGenericMethods(@class);

            GenerateClassVariables(@class);

            if (CLIGenerator.ShouldGenerateClassNativeField(@class))
            {
                PushBlock(CLIBlockKind.AccessSpecifier);
                WriteLine("protected:");
                PopBlock(NewLineKind.IfNotEmpty);

                PushBlock(CLIBlockKind.Fields);
                WriteLineIndent("bool {0};", Helpers.OwnsNativeInstanceIdentifier);
                PopBlock();
            }

            PushBlock(CLIBlockKind.AccessSpecifier);
            WriteLine("private:");
            var accBlock = PopBlock(NewLineKind.IfNotEmpty);

            PushBlock(CLIBlockKind.Fields);
            GenerateClassFields(@class);
            var fieldsBlock = PopBlock();

            accBlock.CheckGenerate = () => !fieldsBlock.IsEmpty;

            WriteLine("};");
        }

        public void GenerateClassNativeField(Class @class, string nativeType)
        {
            WriteLineIndent("property {0} NativePtr;", nativeType);

            PushIndent();
            WriteLine("property System::IntPtr {0}", Helpers.InstanceIdentifier);
            WriteStartBraceIndent();
            WriteLine("virtual System::IntPtr get();");
            WriteLine("virtual void set(System::IntPtr instance);");
            WriteCloseBraceIndent();
            NewLine();

            PopIndent();
        }

        public void GenerateClassGenericMethods(Class @class)
        {
            var printer = TypePrinter;
            var oldCtx = printer.Context;

            PushIndent();
            foreach (var template in @class.Templates)
            {
                if (!template.IsGenerated) continue;

                var functionTemplate = template as FunctionTemplate;
                if (functionTemplate == null) continue;

                PushBlock(CLIBlockKind.Template);

                var function = functionTemplate.TemplatedFunction;

                var typeCtx = new CLITypePrinterContext()
                    {
                        Kind = TypePrinterContextKind.Template,
                        Declaration = template
                    };

                printer.Context = typeCtx;

                var typePrinter = new CLITypePrinter(Driver, typeCtx);
                var retType = function.ReturnType.Type.Visit(typePrinter,
                    function.ReturnType.Qualifiers);

                var typeNames = "";
                var paramNames = template.Parameters.Select(param => param.Name).ToList();
                if (paramNames.Any())
                    typeNames = "typename " + string.Join(", typename ", paramNames);

                Write("generic<{0}>", typeNames);

                // Process the generic type constraints
                var constraints = new List<string>();
                foreach (var param in template.Parameters)
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
            PopIndent();

            printer.Context = oldCtx;
        }

        public void GenerateClassConstructors(Class @class, string nativeType)
        {
            if (@class.IsStatic)
                return;

            PushIndent();

            // Output a default constructor that takes the native pointer.
            WriteLine("{0}({1} native);", @class.Name, nativeType);
            WriteLine("static {0}^ {1}(::System::IntPtr native);",
                @class.Name, Helpers.CreateInstanceIdentifier);
            if (@class.IsRefType)
                WriteLine("static {0}^ {1}(::System::IntPtr native, bool {2});",
                    @class.Name, Helpers.CreateInstanceIdentifier, Helpers.OwnsNativeInstanceIdentifier);

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor, Options))
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
                    if (Options.GenerateFinalizers)
                        GenerateClassFinalizer(@class);
                }
            }

            PopIndent();
        }

        private void GenerateClassDestructor(Class @class)
        {
            PushBlock(CLIBlockKind.Destructor);
            WriteLine("~{0}();", @class.Name);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateClassFinalizer(Class @class)
        {
            PushBlock(CLIBlockKind.Finalizer);
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

            PushIndent();
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
            PopIndent();
        }

        private void GenerateField(Class @class, Field field)
        {
            PushBlock(CLIBlockKind.Field, field);

            GenerateDeclarationCommon(field);
            if (@class.IsUnion)
                WriteLine("[System::Runtime::InteropServices::FieldOffset({0})]",
                    field.Offset);
            WriteLine("{0} {1};", field.Type, field.Name);

            PopBlock();
        }

        public void GenerateClassEvents(Class @class)
        {
            foreach (var @event in @class.Events)
            {
                if (!@event.IsGenerated) continue;

                var cppTypePrinter = new CppTypePrinter(Driver.TypeDatabase);
                var cppArgs = cppTypePrinter.VisitParameters(@event.Parameters, hasNames: true);

                WriteLine("private:");
                PushIndent();

                var delegateName = string.Format("_{0}Delegate", @event.Name);
                WriteLine("delegate void {0}({1});", delegateName, cppArgs);
                WriteLine("{0}^ {0}Instance;", delegateName);

                WriteLine("void _{0}Raise({1});", @event.Name, cppArgs);
                WriteLine("{0} _{1};", @event.Type, @event.Name);

                PopIndent();
                WriteLine("public:");
                PushIndent();

                WriteLine("event {0} {1}", @event.Type, @event.Name);
                WriteStartBraceIndent();

                WriteLine("void add({0} evt);", @event.Type);
                WriteLine("void remove({0} evt);", @event.Type);

                var cliTypePrinter = new CLITypePrinter(Driver);
                var cliArgs = cliTypePrinter.VisitParameters(@event.Parameters, hasNames: true);

                WriteLine("void raise({0});", cliArgs);
                WriteCloseBraceIndent();
                PopIndent();
            }
        }

        public void GenerateClassMethods(List<Method> methods)
        {
            if (methods.Count == 0)
                return;

            PushIndent();

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

                GenerateMethod(method);
            }

            foreach(var method in staticMethods)
                GenerateMethod(method);

            PopIndent();
        }

        public void GenerateClassVariables(Class @class)
        {
            PushIndent();

            foreach(var variable in @class.Variables)
            {
                if (!variable.IsGenerated) continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                var type = variable.Type;

                PushBlock(CLIBlockKind.Variable);

                WriteLine("static property {0} {1}", type, variable.Name);

                WriteStartBraceIndent();

                WriteLine("{0} get();", type);

                if (!variable.QualifiedType.Qualifiers.IsConst)
                    WriteLine("void set({0});", type);

                WriteCloseBraceIndent();

                PopBlock(NewLineKind.BeforeNextBlock);
            }

            PopIndent();
        }

        public bool GenerateClassProlog(Class @class)
        {
            if (@class.IsUnion)
                WriteLine("[System::Runtime::InteropServices::StructLayout({0})]",
                          "System::Runtime::InteropServices::LayoutKind::Explicit");

            // Nested types cannot have visibility modifiers in C++/CLI.
            var isTopLevel = @class.Namespace is TranslationUnit ||
                @class.Namespace is Namespace;
            if (isTopLevel)
                Write("public ");

            Write(@class.IsValueType ? "value struct " : "ref class ");

            Write("{0}", @class.Name);

            if (@class.IsStatic)
                Write(" abstract sealed");

            if (@class.IsOpaque)
            {
                WriteLine(";");
                return true;
            }

            if (!@class.IsStatic)
            {
                if (@class.HasRefBase())
                    Write(" : {0}", QualifiedIdentifier(@class.Bases[0].Class));
                else if (@class.IsRefType)
                    Write(" : ICppInstance");
            }

            NewLine();
            WriteLine("{");
            WriteLine("public:");
            NewLine();

            return false;
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

            PushIndent();
            foreach (var prop in @class.Properties.Where(prop => !ASTUtils.CheckIgnoreProperty(prop)))
            {
                if (prop.IsInRefTypeAndBackedByValueClassField())
                {
                    GenerateField(@class, prop.Field);
                    continue;
                }

                GenerateDeclarationCommon(prop);
                GenerateProperty(prop);
            }
            PopIndent();
        }

        public void GenerateIndexer(Property property)
        {
            var type = property.QualifiedType.Visit(TypePrinter);
            var getter = property.GetMethod;
            var indexParameter = getter.Parameters[0];
            var indexParameterType = indexParameter.QualifiedType.Visit(TypePrinter);

            WriteLine("property {0} default[{1}]", type, indexParameterType);
            WriteStartBraceIndent();

            if (property.HasGetter)
                WriteLine("{0} get({1} {2});", type, indexParameterType, indexParameter.Name);

            if (property.HasSetter)
                WriteLine("void set({1} {2}, {0} value);", type, indexParameterType, indexParameter.Name);

            WriteCloseBraceIndent();
        }

        public void GenerateProperty(Property property)
        {
            if (!(property.HasGetter || property.HasSetter))
                return;

            PushBlock(CLIBlockKind.Property, property);
            var type = property.QualifiedType.Visit(TypePrinter);

            if (property.IsStatic)
                Write("static ");

            if (property.IsIndexer)
            {
                GenerateIndexer(property);
            }
            else
            {
                WriteLine("property {0} {1}", type, property.Name);
                WriteStartBraceIndent();

                if (property.HasGetter)
                    WriteLine("{0} get();", type);

                if (property.HasSetter)
                    WriteLine("void set({0});", type);

                WriteCloseBraceIndent();
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethod(Method method)
        {
            if (ASTUtils.CheckIgnoreMethod(method, Options)) return;

            PushBlock(CLIBlockKind.Method, method);

            GenerateDeclarationCommon(method);

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

            if (method.IsOverride)
            {
                if (method.Access == AccessSpecifier.Private)
                    Write(" sealed");
                Write(" override");
            }

            WriteLine(";");

            if (method.OperatorKind == CXXOperatorKind.EqualEqual)
            {
                GenerateEquals(method, (Class) method.Namespace);
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

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (!typedef.IsGenerated)
                return false;

            FunctionType function;
            if (typedef.Type.IsPointerTo(out function))
            {
                PushBlock(CLIBlockKind.Typedef, typedef);
                GenerateDeclarationCommon(typedef);

                var insideClass = typedef.Namespace is Class;

                var attributedType = typedef.Type.GetPointee() as AttributedType;
                if (attributedType != null)
                {
                    var equivalentFunctionType = attributedType.Equivalent.Type as FunctionType;
                    var callingConvention = equivalentFunctionType.CallingConvention.ToInteropCallConv();
                    if (callingConvention != System.Runtime.InteropServices.CallingConvention.Winapi)
                    {
                        WriteLine("[{0}({1}::{2})] ",
                            "System::Runtime::InteropServices::UnmanagedFunctionPointer",
                            "System::Runtime::InteropServices::CallingConvention",
                            callingConvention);
                    }
                }

                WriteLine("{0}{1};",
                    !insideClass ? "public " : "",
                    string.Format(TypePrinter.VisitDelegate(function),
                    typedef.Name));
                PopBlock(NewLineKind.BeforeNextBlock);

                return true;
            }

            return false;
        }

        public void GenerateFunction(Function function)
        {
            if (!function.IsGenerated)
                return;

            PushBlock(CLIBlockKind.Function, function);

            GenerateDeclarationCommon(function);

            var retType = function.ReturnType.ToString();
            Write("static {0} {1}(", retType, function.Name);

            Write(GenerateParametersList(function.Parameters));

            WriteLine(");");

            PopBlock();
        }

        public void GenerateEnum(Enumeration @enum)
        {
            if (!@enum.IsGenerated || @enum.IsIncomplete)
                return;

            PushBlock(CLIBlockKind.Enum, @enum);

            GenerateDeclarationCommon(@enum);

            if (@enum.Modifiers.HasFlag(Enumeration.EnumModifiers.Flags))
                WriteLine("[System::Flags]");

            // A nested class cannot have an assembly access specifier as part
            // of its declaration.
            if (@enum.Namespace is Namespace)
                Write("public ");

            Write("enum struct {0}", @enum.Name);

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int)
                WriteLine(" : {0}", typeName);
            else
                NewLine();

            WriteLine("{");

            PushIndent();
            foreach (var item in @enum.Items)
            {
                PushBlock(CLIBlockKind.EnumItem);

                GenerateInlineSummary(item.Comment);
                if (item.ExplicitValue)
                    Write(String.Format("{0} = {1}", item.Name,
                        @enum.GetItemValueAsString(item)));
                else
                    Write(String.Format("{0}", item.Name));

                if (item != @enum.Items.Last())
                    WriteLine(",");

                PopBlock(NewLineKind.Never);
            }
            PopIndent();

            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }
    }
}
