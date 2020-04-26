using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;

namespace CppSharp.Generators.Cpp
{
    /// <summary>
    /// Generates C/C++ header files.
    /// </summary>
    public class CppHeaders : CCodeGenerator
    {
        public CppHeaders(BindingContext context, IEnumerable<TranslationUnit> units)
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

            VisitNamespace(TranslationUnit);

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

                var isIncomplete = declaration.IsIncomplete && declaration.CompleteDeclaration == null;
                if (!declaration.IsGenerated || isIncomplete)
                    continue;

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

            @namespace.Visit(this);
        }

        public override bool VisitDeclContext(DeclarationContext decl)
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
                @class.Visit(this);
            }

            if (decl.Functions.Any(f => f.IsGenerated))
                GenerateFunctions(decl);

            foreach (var childNamespace in decl.Namespaces)
                childNamespace.Visit(this);

            return true;
        }

        public override bool VisitNamespace(Namespace @namespace)
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

            VisitDeclContext(@namespace);

            if (generateNamespace)
            {
                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            return true;
        }

        public virtual void GenerateTypedefs(DeclarationContext decl)
        {
            foreach (var typedef in decl.Typedefs)
            {
                if (!typedef.IsGenerated)
                    continue;

                typedef.Visit(this);
            }
        }

        public virtual void GenerateFunctions(DeclarationContext decl)
        {
            PushBlock(BlockKind.FunctionsClass);

            // Generate all the function declarations for the module.
            foreach (var function in decl.Functions)
            {
                if (!function.IsGenerated)
                    continue;

                function.Visit(this);
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete || @class.IsDependent)
                return false;

            //if (@class.IsOpaque)
             //   return false;

            PushBlock(BlockKind.Class, @class);

            GenerateDeclarationCommon(@class);

            GenerateClassSpecifier(@class);

            if (@class.IsOpaque)
            {
                WriteLine(";");
                return false;
            }

            NewLine();
            WriteLine("{");
            WriteLine("public:");
            NewLine();

            // Process the nested types.
            Indent();
            VisitDeclContext(@class);
            Unindent();

            if (CppGenerator.ShouldGenerateClassNativeField(@class))
                GenerateClassNativeField(@class);

            GenerateClassConstructors(@class);
            GenerateClassProperties(@class);
            GenerateClassEvents(@class);
            GenerateClassMethods(@class.Methods);

            if (Options.GenerateFunctionTemplates)
                GenerateClassGenericMethods(@class);

            GenerateClassVariables(@class);

            if (CppGenerator.ShouldGenerateClassNativeField(@class))
            {
                PushBlock(BlockKind.AccessSpecifier);
                WriteLine("protected:");
                PopBlock(NewLineKind.IfNotEmpty);

                PushBlock(BlockKind.Fields);
                WriteLineIndent($"bool {Helpers.OwnsNativeInstanceIdentifier};");
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

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public void GenerateClassNativeField(Class @class)
        {
            PushBlock();

            var nativeInstanceField = new Field()
            {
                Name = Helpers.InstanceIdentifier,
                QualifiedType = new QualifiedType(new PointerType(new QualifiedType(new TagType(@class)))),
                Namespace = @class
            };

            Indent();
            CTypePrinter.PushContext(TypePrinterContextKind.Native);
            nativeInstanceField.Visit(this);
            CTypePrinter.PopContext();
            Unindent();

            PopBlock(NewLineKind.BeforeNextBlock);

            /*var nativeInstanceProperty = new Property()
            {
                Name = Helpers.InstanceIdentifier,
                QualifiedType =
            };

            nativeInstanceProperty.Visit(this);*/
        }

        public virtual void GenerateClassGenericMethods(Class @class)
        {
        }

        public void GenerateClassConstructors(Class @class)
        {
            if (@class.IsStatic)
                return;

            Indent();

            CTypePrinter.PushContext(TypePrinterContextKind.Native);
            var classNativeName = @class.Visit(CTypePrinter);
            CTypePrinter.PopContext();

            WriteLine($"{@class.Name}({classNativeName}* instance);");
            NewLine();

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor) || FunctionIgnored(ctor))
                    continue;

                ctor.Visit(this);
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

            Unindent();
        }

        public virtual void GenerateClassDestructor(Class @class)
        {
            PushBlock(BlockKind.Destructor);
            WriteLine($"~{@class.Name}();");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClassFinalizer(Class @class)
        {
            PushBlock(BlockKind.Finalizer);
            WriteLine($"!{@class.Name}();");
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
                    field.Visit(this);
                }
            }
            Unindent();
        }

        public override bool VisitFieldDecl(Field field)
        {
            PushBlock(BlockKind.Field, field);

            GenerateDeclarationCommon(field);

            var fieldType = field.Type.Visit(CTypePrinter);
            WriteLine($"{fieldType} {field.Name};");

            PopBlock();

            return true;
        }

        public void GenerateClassEvents(Class @class)
        {
            foreach (var @event in @class.Events)
            {
                if (!@event.IsGenerated) continue;
                @event.Visit(this);
            }
        }

        public override bool VisitEvent(Event @event)
        {
            return true;
        }

        public void GenerateClassMethods(List<Method> methods)
        {
            if (methods.Count == 0)
                return;

            Indent();

            var @class = (Class) methods[0].Namespace;

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

                method.Visit(this);
            }

            foreach(var method in staticMethods)
                method.Visit(this);

            Unindent();
        }

        public void GenerateClassVariables(Class @class)
        {
            foreach (var variable in @class.Variables)
            {
                if (!variable.IsGenerated) continue;
                variable.Visit(this);
            }
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            Write(@class.IsValueType ? "struct " : "class ");
            Write($"{@class.Name}");

            if (@class.IsStatic)
                Write(" abstract sealed");

            if (!@class.IsStatic && @class.HasRefBase())
                Write($" : public {QualifiedIdentifier(@class.Bases[0].Class)}");
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
                    prop.Field.Visit(this);
                    continue;
                }

                prop.Visit(this);
            }
            Unindent();
        }

        public virtual void GenerateIndexer(Property property)
        {
            throw new System.NotImplementedException();
        }

        public override bool VisitProperty(Property property)
        {
            GenerateDeclarationCommon(property);

            return base.VisitProperty(property);
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (ASTUtils.CheckIgnoreMethod(method) || FunctionIgnored(method))
                return false;

            PushBlock(BlockKind.Method, method);
            GenerateDeclarationCommon(method);

            GenerateMethodSpecifier(method);
            WriteLine(";");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            if (!typedef.IsGenerated)
                return false;

            var functionType = typedef.Type as FunctionType;
            if (functionType != null || typedef.Type.IsPointerTo(out functionType))
            {
                PushBlock(BlockKind.Typedef, typedef);
                GenerateDeclarationCommon(typedef);

                var @delegate = string.Format(CTypePrinter.VisitDelegate(functionType), typedef.Name);
                WriteLine($"{@delegate};");
                    
                PopBlock(NewLineKind.BeforeNextBlock);

                return true;
            }

            return false;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated || FunctionIgnored(function))
                return false;

            PushBlock(BlockKind.Function, function);

            GenerateDeclarationCommon(function);

            var retType = function.ReturnType.Visit(CTypePrinter);
            Write($"{retType} {function.Name}(");

            GenerateMethodParameters(function);
            WriteLine(");");

            PopBlock();

            return true;
        }

        public static bool FunctionIgnored(Function function)
        {
            return TypeIgnored(function.ReturnType.Type) ||
                function.Parameters.Any(param => TypeIgnored(param.Type));
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
