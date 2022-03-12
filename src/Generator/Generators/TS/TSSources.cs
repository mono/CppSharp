using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;

namespace CppSharp.Generators.TS
{
    /// <summary>
    /// Generates TypeScript interface files.
    /// </summary>
    public class TSSources : CCodeGenerator
    {
        public TSSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            typePrinter = new TSTypePrinter(Context);
        }

        public override string FileExtension => "d.ts";

        public override bool GenerateSemicolonAsDeclarationTerminator => false;

        public virtual bool GenerateNamespaces => true;

        public virtual bool GenerateSelectiveImports => false;

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            if (GenerateSelectiveImports)
                GenerateImports();
            else
                GenerateWildcardImports();

            GenerateMain();
        }

        public virtual void GenerateMain()
        {
            VisitTranslationUnit(TranslationUnit);
        }

        public virtual Dictionary<TranslationUnit, List<Declaration>> ComputeExternalReferences()
        {
            var typeReferenceCollector = new CLITypeReferenceCollector(Context.TypeMaps,
                Context.Options);
            typeReferenceCollector.Process(TranslationUnit);

            var typeReferences = typeReferenceCollector.TypeReferences;
            var imports = new Dictionary<TranslationUnit, List<Declaration>>();

            foreach (var typeRef in typeReferences)
            {
                if (typeRef.Include.TranslationUnit == TranslationUnit)
                    continue;

                if (typeRef.Include.File == TranslationUnit.FileName)
                    continue;

                var include = typeRef.Include;
                var typeRefUnit = include.TranslationUnit;

                if (typeRefUnit != null && !typeRefUnit.IsDeclared)
                    continue;

                if (!imports.ContainsKey(typeRefUnit))
                    imports[typeRefUnit] = new List<Declaration>();

                imports[typeRefUnit].Add(typeRef.Declaration);
            }

            return imports;
        }

        public virtual bool NeedsExternalImports()
        {
            var imports = ComputeExternalReferences();
            return imports.Keys.Count > 0;
        }

        public virtual void GenerateWildcardImports()
        {
            if (!NeedsExternalImports())
                return;

            foreach (var module in Options.Modules)
            {
                if (module == Options.SystemModule)
                    continue;

                WriteLine($"import * as {module.LibraryName} from \"{module.LibraryName}\";");
            }
        }

        public virtual void GenerateImports()
        {
            PushBlock(BlockKind.ForwardReferences);

            var imports = ComputeExternalReferences();

            foreach (var unit in imports)
            {
                string unitName = unit.Key.FileNameWithoutExtension;
                if (Options.GenerateName != null)
                    unitName = Options.GenerateName(unit.Key);

                var names = string.Join(", ", unit.Value.Select(d => d.Name));
                WriteLine($"import {{{names}}} from \"{unitName}\";");
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            var isTopLevel = @namespace is TranslationUnit;
            var generateNamespace = GenerateNamespaces;

            if (generateNamespace)
            {
                PushBlock(BlockKind.Namespace, @namespace);
                WriteLine(isTopLevel
                    ? $"declare module \"{@namespace.TranslationUnit.Module.LibraryName}\""
                    : $"namespace {@namespace.Name}");
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

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated || @class.IsIncomplete)
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
            NewLine();

            // Process the nested types.
            Indent();
            VisitDeclContext(@class);
            Unindent();

            GenerateClassConstructors(@class);
            GenerateClassProperties(@class);
            GenerateClassEvents(@class);
            GenerateClassMethods(@class.Methods);

            GenerateClassVariables(@class);

            PushBlock(BlockKind.Fields);
            GenerateClassFields(@class);
            PopBlock();

            WriteLine("}");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public void GenerateClassConstructors(Class @class)
        {
            if (@class.IsStatic)
                return;

            Indent();

            var classNativeName = @class.Visit(CTypePrinter);

            foreach (var ctor in @class.Constructors)
            {
                if (ASTUtils.CheckIgnoreMethod(ctor) || FunctionIgnored(ctor))
                    continue;

                ctor.Visit(this);
            }

            Unindent();
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

                if (method.IsOperator)
                    continue;

                if (method.IsStatic)
                {
                    staticMethods.Add(method);
                    continue;
                }

                method.Visit(this);
            }

            foreach (var method in staticMethods)
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

        public static string GetClassTemplateParameters(Class @class)
        {
            var @params = @class.TemplateParameters.OfType<TypeTemplateParameter>().Select(
                p => !string.IsNullOrEmpty(p.Constraint) ? $"{p.Name} extends {p.Constraint}" : p.Name);

            return $"<{string.Join(", ", @params)}>";
        }

        public string GetBaseClassTemplateParameters(BaseClassSpecifier baseClassSpec)
        {
            if (!(baseClassSpec.Type is TemplateSpecializationType templateSpecType))
                throw new NotSupportedException();

            var args = templateSpecType.Arguments.Select(arg =>
            {
                arg.Type.Type.TryGetClass(out var @class);
                return @class;
            });

            return $"<{string.Join(", ", args.Select(c => c.Name))}>";
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            if (@class.IsAbstract)
                Write("abstract ");

            Write(@class.IsInterface ? "interface" : "class");
            Write($" {@class.Name}");
            if (@class.IsDependent)
                Write(GetClassTemplateParameters(@class));

            if (!@class.IsStatic && @class.HasNonIgnoredBase)
            {
                var baseClassSpec = @class.Bases.First(bs => bs.Class == @class.BaseClass);
                var baseClass = baseClassSpec.Class;
                var needsQualifiedName = baseClass.TranslationUnit != @class.TranslationUnit;
                var baseClassName = needsQualifiedName ? QualifiedIdentifier(baseClass) : baseClass.Name;

                Write($" extends {baseClassName}");
                if (baseClass.IsDependent)
                    Write($"{GetBaseClassTemplateParameters(baseClassSpec)}");
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

        public override string GenerateEnumSpecifier(Enumeration @enum)
        {
            Write($"enum {@enum.Name}");
            return @enum.Name;
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

        public override bool VisitEvent(Event @event)
        {
            PushBlock(BlockKind.Event, @event);

            GenerateDeclarationCommon(@event);

            //WriteLine($"{@event.Name}: CppSharp.Signal;");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
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

        public override void GenerateMethodSpecifier(Method method, MethodSpecifierKind? kind = null)
        {
            if (method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion)
            {
                Write($"{GetMethodIdentifier(method)}(");
                GenerateMethodParameters(method);
                Write($")");
                return;
            }

            Write($"{GetMethodIdentifier(method)}(");
            GenerateMethodParameters(method);

            var returnType = method.ReturnType.Visit(CTypePrinter);
            Write($"): {returnType}");
        }

        public override string GetMethodIdentifier(Function function,
            TypePrinterContextKind context = TypePrinterContextKind.Managed)
        {
            if (function is Method method)
            {
                if (method.IsConstructor)
                    return "constructor";
            }

            return base.GetMethodIdentifier(function, context);
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
            Write($"export function {function.Name}(");

            GenerateMethodParameters(function);
            WriteLine($"): {retType};");

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
            return finalType.TryGetClass(out @class) && (@class.CompleteDeclaration == null && @class.IsIncomplete);
        }
    }
}
