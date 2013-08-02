﻿using System;
using System.Collections.Generic;
using System.IO;
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

            PushBlock(CLIBlockKind.IncludesForwardReferences);
            WriteLine("#include <{0}>", TranslationUnit.IncludePath);
            GenerateIncludeForwardRefs();
            PopBlock(NewLineKind.BeforeNextBlock);

            PopBlock(NewLineKind.Always);

            GenerateNamespace(TranslationUnit);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public void GenerateIncludeForwardRefs()
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase);
            typeReferenceCollector.Process(TranslationUnit);

            var includes = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            { 
                if (typeRef.Include.File == TranslationUnit.FileName)
                    continue;

                var include = typeRef.Include;
                if(!string.IsNullOrEmpty(include.File) && include.InHeader)
                    includes.Add(include.ToString());
            }

            foreach (var include in includes)
                WriteLine(include);
        }

        public void GenerateForwardRefs(Namespace @namespace)
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Driver.TypeDatabase);
            typeReferenceCollector.Process(@namespace);

            // Use a set to remove duplicate entries.
            var forwardRefs = new SortedSet<string>(StringComparer.InvariantCulture);

            foreach (var typeRef in typeReferenceCollector.TypeReferences)
            {
                var @ref = typeRef.FowardReference;
                if(!string.IsNullOrEmpty(@ref) && !typeRef.Include.InHeader)
                    forwardRefs.Add(@ref);
            }

            foreach (var forwardRef in forwardRefs)
                WriteLine(forwardRef);
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

            // Generate the forward references.
            PushBlock(CLIBlockKind.ForwardReferences);
            GenerateForwardRefs(@namespace);
            PopBlock(NewLineKind.BeforeNextBlock);

            // Generate all the enum declarations for the module.
            foreach (var @enum in @namespace.Enums)
            {
                if (@enum.Ignore || @enum.IsIncomplete)
                    continue;

                PushBlock(CLIBlockKind.Enum, @enum);
                GenerateEnum(@enum);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            // Generate all the typedef declarations for the module.
            GenerateTypedefs(@namespace);

            // Generate all the struct/class declarations for the module.
            foreach (var @class in @namespace.Classes)
            {
                if (@class.Ignore || @class.IsIncomplete)
                    continue;

                if (@class.IsOpaque)
                    continue;

                PushBlock(CLIBlockKind.Class, @class);
                GenerateClass(@class);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            if (@namespace.HasFunctions)
                GenerateFunctions(@namespace);

            foreach(var childNamespace in @namespace.Namespaces)
                GenerateNamespace(childNamespace);

            if (generateNamespace)
            {
                WriteCloseBraceIndent();
                PopBlock(NewLineKind.BeforeNextBlock);
            }
        }

        public void GenerateTypedefs(Namespace @namespace)
        {
            foreach (var typedef in @namespace.Typedefs)
            {
                if (typedef.Ignore)
                    continue;

                GenerateTypedef(typedef);
            }
        }

        public void GenerateFunctions(Namespace @namespace)
        {
            PushBlock(CLIBlockKind.FunctionsClass);

            WriteLine("public ref class {0}{1}", SafeIdentifier(Options.OutputNamespace),
                TranslationUnit.FileNameWithoutExtension);
            WriteLine("{");
            WriteLine("public:");
            PushIndent();

            // Generate all the function declarations for the module.
            foreach (var function in @namespace.Functions)
            {
                GenerateFunction(function);
            }

            PopIndent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateDeclarationCommon(Declaration T)
        {
            GenerateSummary(T.BriefComment);
            GenerateDebug(T);
        }

        public void GenerateClass(Class @class)
        {
            if (@class.Ignore || @class.IsIncomplete)
                return;

            GenerateDeclarationCommon(@class);

            if (@class.IsUnion)
            {
                // TODO: How to do wrapping of unions?
                //const string @namespace = "System::Runtime::InteropServices";
                //WriteLine("[{0}::StructLayout({0}::LayoutKind::Explicit)]",
                //    @namespace);
                //throw new NotImplementedException("Unions are not supported yet");
            }

            if (GenerateClassProlog(@class))
                return;

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
                    Console.WriteLine("Ignored base class of value type '{0}'",
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
                    WriteLine("[FieldOffset({0})]", field.Offset);
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
            return false;
        }

        public void GenerateClassProperties(Class @class)
        {
            PushIndent();
            foreach (var prop in @class.Properties)
            {
                if (prop.Ignore) continue;

                GenerateDeclarationCommon(prop);
                var isGetter = prop.GetMethod != null || prop.Field != null;
                var isSetter = prop.SetMethod != null || prop.Field != null;
                GenerateProperty(prop, isGetter, isSetter);
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
            PopBlock();
        }

        public void GenerateMethod(Method method)
        {
            if (ASTUtils.CheckIgnoreMethod(method)) return;

            PushBlock(CLIBlockKind.Method, method);

            GenerateDeclarationCommon(method);

            if (method.IsOverride)
                Write("virtual ");

            if (method.IsStatic)
                Write("static ");

            if (method.Kind == CXXMethodKind.Constructor || method.Kind == CXXMethodKind.Destructor)
                Write("{0}(", SafeIdentifier(method.Name));
            else
                Write("{0} {1}(", method.ReturnType, SafeIdentifier(method.Name));

            GenerateMethodParameters(method);

            Write(")");

            if (method.IsOverride)
                Write(" override");

            WriteLine(";");

            PopBlock();
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

                WriteLine("public {0};",
                    string.Format(TypePrinter.VisitDelegate(function),
                    SafeIdentifier(typedef.Name)));
                PopBlock(NewLineKind.BeforeNextBlock);

                return true;
            }
            else if (typedef.Type.IsEnumType())
            {
                // Already handled in the parser.
            }
            else
            {
                Console.WriteLine("Unhandled typedef type: {0}", typedef);
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

        public void GenerateDebug(Declaration decl)
        {
            if (Options.OutputDebug && !String.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
        }

        public void GenerateEnum(Enumeration @enum)
        {
            if (@enum.Ignore || @enum.IsIncomplete)
                return;

            GenerateDeclarationCommon(@enum);

            if (@enum.Modifiers.HasFlag(Enumeration.EnumModifiers.Flags))
                WriteLine("[System::Flags]");

            Write("public enum struct {0}", SafeIdentifier(@enum.Name));

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                new TypeQualifiers());

            if (@enum.BuiltinType.Type != PrimitiveType.Int32)
                WriteLine(" : {0}", typeName);
            else
                NewLine();

            WriteLine("{");

            PushIndent();
            for (int i = 0; i < @enum.Items.Count; ++i)
            {
                var item = @enum.Items[i];
                GenerateInlineSummary(item.Comment);
                if (item.ExplicitValue)
                    Write(String.Format("{0} = {1}", SafeIdentifier(item.Name),
                        @enum.GetItemValueAsString(item)));
                else
                    Write(String.Format("{0}", SafeIdentifier(item.Name)));

                if (i < @enum.Items.Count - 1)
                    WriteLine(",");
            }
            PopIndent();

            NewLine();
            WriteLine("};");
        }
    }
}
