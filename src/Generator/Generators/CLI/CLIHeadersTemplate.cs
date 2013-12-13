using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// Generates C++/CLI header files.
    /// </summary>
    public class CLIHeadersTemplate : CLITextTemplate
    {
        public override string FileExtension { get { return "h"; } }

        public CLIHeadersTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
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
            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase);
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

                if (unit != null && unit.Ignore)
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

            foreach (var typeRef in typeReferences)
            {
                if (string.IsNullOrWhiteSpace(typeRef.FowardReference))
                    continue;

                var declaration = typeRef.Declaration;
                if (!(declaration.Namespace is Namespace))
                    continue;

                var @namespace = FindCreateNamespace(rootNamespace, declaration);
                @namespace.TypeReferences.Add(typeRef);
            }

            return rootNamespace;
        }

        public void GenerateForwardRefs()
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase);
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
                if (@enum.Ignore || @enum.IsIncomplete)
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
                if (@class.Ignore || @class.IsIncomplete)
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
                                               : SafeIdentifier(@namespace.Name));
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
                if (typedef.Ignore)
                    continue;

                GenerateTypedef(typedef);
            }
        }

        public void GenerateFunctions(DeclarationContext decl)
        {
            PushBlock(CLIBlockKind.FunctionsClass);

            WriteLine("public ref class {0}{1}", SafeIdentifier(Options.OutputNamespace),
                TranslationUnit.FileNameWithoutExtension);
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
            if (@class.Ignore || @class.IsIncomplete)
                return;

            GenerateDeclarationCommon(@class);

            if (GenerateClassProlog(@class))
                return;

            // Process the nested types.
            PushIndent();
            GenerateDeclContext(@class);
            PopIndent();

            var nativeType = string.Format("::{0}*", @class.QualifiedOriginalName);

            if (@class.IsRefType)
            {
                GenerateClassNativeField(@class, nativeType);
            }

            GenerateClassConstructors(@class, nativeType);

            GenerateClassFields(@class);

            GenerateClassProperties(@class);

            GenerateClassEvents(@class);
            GenerateClassMethods(@class);

            if (Options.GenerateFunctionTemplates)
                GenerateClassGenericMethods(@class);

            GenerateClassVariables(@class);

            WriteLine("};");
        }

        internal static bool HasRefBase(Class @class)
        {
            Class baseClass = null;

            if (@class.HasBaseClass)
                baseClass = @class.Bases[0].Class;

            var hasRefBase = baseClass != null && baseClass.IsRefType
                             && !baseClass.Ignore;

            return hasRefBase;
        }

        public void GenerateClassNativeField(Class @class, string nativeType)
        {
            if (HasRefBase(@class)) return;

            WriteLineIndent("property {0} NativePtr;", nativeType);

            PushIndent();
            WriteLine("property System::IntPtr Instance");
            WriteStartBraceIndent();
            WriteLine("virtual System::IntPtr get();");
            WriteLine("virtual void set(System::IntPtr instance);");
            WriteCloseBraceIndent();
            NewLine();

            PopIndent();
        }

        public void GenerateClassGenericMethods(Class @class)
        {
            var printer = TypePrinter as CLITypePrinter;
            var oldCtx = printer.Context;

            PushIndent();
            foreach (var template in @class.Templates)
            {
                if (template.Ignore) continue;

                var functionTemplate = template as FunctionTemplate;
                if (functionTemplate == null) continue;

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

                var typeNamesStr = "";
                var paramNames = template.Parameters.Select(param => param.Name).ToList();
                if (paramNames.Any())
                    typeNamesStr = "typename " + string.Join(", typename ", paramNames);

                WriteLine("generic<{0}>", typeNamesStr);
                WriteLine("{0} {1}({2});", retType, SafeIdentifier(function.Name),
                    GenerateParametersList(function.Parameters));
            }
            PopIndent();

            printer.Context = oldCtx;
        }

        public void GenerateClassConstructors(Class @class, string nativeType)
        {
            PushIndent();

            // Output a default constructor that takes the native pointer.
            WriteLine("{0}({1} native);", SafeIdentifier(@class.Name), nativeType);
            WriteLine("{0}({1} native);", SafeIdentifier(@class.Name), "System::IntPtr");

            foreach (var ctor in @class.Constructors)
            {
                if (ctor.IsCopyConstructor || ctor.IsMoveConstructor)
                    continue;

                // Default constructors are not supported in .NET value types.
                if (ctor.Parameters.Count == 0 && @class.IsValueType)
                    continue;

                GenerateMethod(ctor);
            }

            PopIndent();
        }

        public void GenerateClassFields(Class @class)
        {
            if (!@class.IsValueType)
                return;

            // Handle the case of struct (value-type) inheritance by adding the base
            // fields to the managed value subtypes.
            foreach (var @base in @class.Bases)
            {
                Class baseClass;
                if (!@base.Type.IsTagDecl(out baseClass))
                    continue;

                if (!baseClass.IsValueType || baseClass.Ignore)
                {
                    Log.EmitMessage("Ignored base class of value type '{0}'",
                        baseClass.Name);
                    continue;
                }

                GenerateClassFields(baseClass);
            }

            PushIndent();
            foreach (var field in @class.Fields)
            {
                if (ASTUtils.CheckIgnoreField(field)) continue;

                GenerateDeclarationCommon(field);
                if (@class.IsUnion)
                    WriteLine("[System::Runtime::InteropServices::FieldOffset({0})]",
                        field.Offset);
                WriteLine("{0} {1};", field.Type, SafeIdentifier(field.Name));
            }
            PopIndent();
        }

        public void GenerateClassEvents(Class @class)
        {
            foreach (var @event in @class.Events)
            {
                if (@event.Ignore) continue;

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

        public void GenerateClassMethods(Class @class)
        {
            PushIndent();

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
                if (variable.Ignore) continue;

                if (variable.Access != AccessSpecifier.Public)
                    continue;

                var type = variable.Type;

                WriteLine("static property {0} {1}", type, variable.Name);

                WriteStartBraceIndent();

                WriteLine("{0} get();", type);

                if (!variable.QualifiedType.Qualifiers.IsConst)
                    WriteLine("void set({0});", type);

                WriteCloseBraceIndent();
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

            Write("{0}", SafeIdentifier(@class.Name));

            if (@class.IsOpaque)
            {
                WriteLine(";");
                return true;
            }

            if (HasRefBase(@class))
                Write(" : {0}", QualifiedIdentifier(@class.Bases[0].Class));
            else if (@class.IsRefType)
                Write(" : ICppInstance");

            NewLine();
            WriteLine("{");
            WriteLine("public:");
            NewLine();

            return false;
        }

        public void GenerateClassProperties(Class @class)
        {
            PushIndent();
            foreach (var prop in @class.Properties)
            {
                if (prop.Ignore) continue;

                GenerateDeclarationCommon(prop);
                GenerateProperty(prop, prop.HasGetter, prop.HasSetter);
            }
            PopIndent();
        }

        public void GenerateProperty<T>(T decl, bool isGetter = true, bool isSetter = true)
            where T : Declaration, ITypedDecl
        {
            if (!(isGetter || isSetter))
                return;

            PushBlock(CLIBlockKind.Property, decl);
            var type = decl.Type.Visit(TypePrinter, decl.QualifiedType.Qualifiers);

            WriteLine("property {0} {1}", type, decl.Name);
            WriteStartBraceIndent();

            if(isGetter) WriteLine("{0} get();", type);
            if(isSetter) WriteLine("void set({0});", type);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethod(Method method)
        {
            if (ASTUtils.CheckIgnoreMethod(method)) return;

            PushBlock(CLIBlockKind.Method, method);

            GenerateDeclarationCommon(method);

            if (method.IsVirtual || method.IsOverride)
                Write("virtual ");

            var isBuiltinOperator = method.IsOperator &&
                Operators.IsBuiltinOperator(method.OperatorKind);

            if (method.IsStatic || isBuiltinOperator)
                Write("static ");

            if (method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion)
                Write("{0}(", GetMethodName(method));
            else
                Write("{0} {1}(", method.ReturnType, SafeIdentifier(method.Name));

            GenerateMethodParameters(method);

            Write(")");

            if (method.IsOverride)
                Write(" override");

            WriteLine(";");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public bool GenerateTypedef(TypedefDecl typedef)
        {
            if (typedef.Ignore)
                return false;

            FunctionType function;
            if (typedef.Type.IsPointerTo<FunctionType>(out function))
            {
                PushBlock(CLIBlockKind.Typedef, typedef);
                GenerateDeclarationCommon(typedef);

                WriteLine("{0};",
                    string.Format(TypePrinter.VisitDelegate(function),
                    SafeIdentifier(typedef.Name)));
                PopBlock(NewLineKind.BeforeNextBlock);

                return true;
            }

            return false;
        }

        public void GenerateFunction(Function function)
        {
            if (function.Ignore)
                return;

            PushBlock(CLIBlockKind.Function, function);

            GenerateDeclarationCommon(function);

            var retType = function.ReturnType.ToString();
            Write("static {0} {1}(", retType, SafeIdentifier(function.Name));

            Write(GenerateParametersList(function.Parameters));

            WriteLine(");");

            PopBlock();
        }

        public void GenerateEnum(Enumeration @enum)
        {
            if (@enum.Ignore || @enum.IsIncomplete)
                return;

            PushBlock(CLIBlockKind.Enum, @enum);

            GenerateDeclarationCommon(@enum);

            if (@enum.Modifiers.HasFlag(Enumeration.EnumModifiers.Flags))
                WriteLine("[System::Flags]");

            // A nested class cannot have an assembly access specifier as part
            // of its declaration.
            if (@enum.Namespace is Namespace)
                Write("public ");

            Write("enum struct {0}", SafeIdentifier(@enum.Name));

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int32)
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
                    Write(String.Format("{0} = {1}", SafeIdentifier(item.Name),
                        @enum.GetItemValueAsString(item)));
                else
                    Write(String.Format("{0}", SafeIdentifier(item.Name)));

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
