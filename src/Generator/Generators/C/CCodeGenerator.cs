using CppSharp.AST;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.Generators.C
{
    public struct CInclude
    {
        public enum IncludeKind
        {
            Angled,
            Quoted
        }

        public string File;
        public TranslationUnit TranslationUnit;

        public IncludeKind Kind;
        public bool InHeader;

        public override string ToString()
        {
            return Kind == IncludeKind.Angled ?
                $"#include <{File}>" : $"#include \"{File}\"";
        }
    }

    public abstract class CCodeGenerator : CodeGenerator
    {
        public CCodeGenerator(BindingContext context,
            IEnumerable<TranslationUnit> units = null)
            : base(context, units)
        {
            VisitOptions.VisitPropertyAccessors = true;
            typePrinter = new CppTypePrinter(context);
        }

        public abstract override string FileExtension { get; }

        public abstract override void Process();

        public ISet<CInclude> Includes = new HashSet<CInclude>();

        public virtual string QualifiedName(Declaration decl)
        {
            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
                return decl.Name;

            return decl.QualifiedName;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (!string.IsNullOrEmpty(TranslationUnit.Module.OutputNamespace))
            {
                if (string.IsNullOrEmpty(decl.QualifiedName))
                    return $"{decl.TranslationUnit.Module.OutputNamespace}";

                return $"{decl.TranslationUnit.Module.OutputNamespace}::{decl.QualifiedName}";
            }

            return decl.QualifiedName;
        }

        protected CppTypePrinter typePrinter;
        public virtual CppTypePrinter CTypePrinter => typePrinter;

        public bool IsCLIGenerator => Context.Options.GeneratorKind == GeneratorKind.CLI;

        public virtual void WriteHeaders() { }

        public virtual void WriteInclude(CInclude include)
        {
            WriteLine(include.ToString());
        }

        public void WriteInclude(string file, CInclude.IncludeKind kind)
        {
            var include = new CInclude { File = file, Kind = kind };
            WriteInclude(include);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return decl.IsGenerated && !AlreadyVisited(decl);
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!VisitDeclaration(typedef))
                return false;

            PushBlock();

            var result = typedef.Type.Visit(CTypePrinter);
            result.Name = typedef.Name;
            WriteLine($"typedef {result};");

            var newlineKind = NewLineKind.BeforeNextBlock;

            var declarations = typedef.Namespace.Declarations.ToList();
            var newIndex = declarations.FindIndex(d => d == typedef) + 1;
            if (newIndex < declarations.Count)
            {
                if (declarations[newIndex] is TypedefDecl)
                    newlineKind = NewLineKind.Never;
            }

            PopBlock(newlineKind);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            PushBlock(BlockKind.Enum, @enum);

            GenerateDeclarationCommon(@enum);

            if (IsCLIGenerator)
            {
                if (@enum.Modifiers.HasFlag(Enumeration.EnumModifiers.Flags))
                    WriteLine("[System::Flags]");

                // A nested class cannot have an assembly access specifier as part
                // of its declaration.
                if (@enum.Namespace is Namespace)
                    Write("public ");
            }

            var enumKind = @enum.IsScoped || IsCLIGenerator ? "enum class" : "enum";
            var enumName = Options.GeneratorKind == GeneratorKind.C ?
                QualifiedName(@enum) : @enum.Name;

            var generateTypedef = Options.GeneratorKind == GeneratorKind.C;
            if (generateTypedef)
                Write($"typedef {enumKind} {enumName}");
            else
                Write($"{enumKind} {enumName}");

            if (Options.GeneratorKind == GeneratorKind.CPlusPlus ||
                Options.GeneratorKind == GeneratorKind.CLI)
            {
                var typeName = CTypePrinter.VisitPrimitiveType(
                    @enum.BuiltinType.Type, new TypeQualifiers());

                if (@enum.BuiltinType.Type != PrimitiveType.Int &&
                    @enum.BuiltinType.Type != PrimitiveType.Null)
                    Write($" : {typeName}");
            }

            NewLine();
            WriteOpenBraceAndIndent();

            GenerateEnumItems(@enum);

            Unindent();

            if (!string.IsNullOrWhiteSpace(enumName) && generateTypedef)
                WriteLine($"}} {enumName};");
            else
                WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (item.Comment != null)
                GenerateInlineSummary(item.Comment);

            var @enum = item.Namespace as Enumeration;
            var enumItemName = Options.GeneratorKind == GeneratorKind.C ?
                $"{@enum.QualifiedName}_{item.Name}" : item.Name;

            Write(enumItemName);

            if (item.ExplicitValue)
                Write($" = {@enum.GetItemValueAsString(item)}");

            return true;
        }

        public static string GetAccess(AccessSpecifier accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case AccessSpecifier.Private:
                    return "private";
                case AccessSpecifier.Internal:
                    return string.Empty;
                case AccessSpecifier.Protected:
                    return "protected";
                default:
                    return "public";
            }
        }

        public virtual List<string> GenerateExtraClassSpecifiers(Class @class)
            => new List<string>();

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();

            if (Options.GeneratorKind == GeneratorKind.CLI)
            {
                keywords.Add(Helpers.GetAccess(@class.Access));

                if (@class.IsAbstract)
                    keywords.Add("abstract");
            }

            if (@class.IsFinal)
                keywords.Add("final");

            if (@class.IsStatic)
                keywords.Add("static");

            if (Options.GeneratorKind == GeneratorKind.CLI)
                keywords.Add(@class.IsInterface ? "interface" : "class");
            else
                keywords.Add("class");

            keywords.AddRange(GenerateExtraClassSpecifiers(@class));

            keywords.Add(@class.Name);

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write("{0}", string.Join(" ", keywords));

            var bases = @class.Bases.Where(@base => @base.IsGenerated &&
                @base.IsClass && @base.Class.IsGenerated).ToList();

            if (bases.Count > 0 && !@class.IsStatic)
            {
                var classes = bases.Select(@base =>
                    $"{GetAccess(@base.Access)} {@base.Class.Visit(CTypePrinter)}");
                if (classes.Count() > 0)
                    Write(" : {0}", string.Join(", ", classes));
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            PushBlock();

            GenerateClassSpecifier(@class);
            NewLine();
            WriteOpenBraceAndIndent();

            GenerateClassBody(@class);

            Unindent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public virtual bool GenerateClassBody(Class @class)
        {
            return VisitDeclContext(@class);
        }

        public override bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            CTypePrinter.PushContext(TypePrinterContextKind.Native);
            var typeName = field.Type.Visit(CTypePrinter);
            CTypePrinter.PopContext();

            PushBlock(BlockKind.Field, field);

            WriteLine($"{typeName} {field.Name};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public virtual string GetMethodIdentifier(Function function,
            TypePrinterContextKind context = TypePrinterContextKind.Managed)
        {
            var method = function as Method;
            if (method != null)
            {
                if (method.OperatorKind == CXXOperatorKind.Star)
                {
                    CTypePrinter.PushContext(TypePrinterContextKind.Native);
                    method.ReturnType.Visit(CTypePrinter);
                    CTypePrinter.PopContext();
                }

                if (method.OperatorKind == CXXOperatorKind.Conversion ||
                    method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                {
                    CTypePrinter.PushContext(context);
                    var conversionType = method.ConversionType.Visit(CTypePrinter);
                    CTypePrinter.PopContext();

                    return "operator " + conversionType;
                }

                if (method.IsConstructor || method.IsDestructor)
                {
                    var @class = (Class)method.Namespace;
                    return @class.Name;
                }
            }

            return (context == TypePrinterContextKind.Managed) ?
                function.Name : function.OriginalName;
        }

        public override void GenerateMethodSpecifier(Method method,
            MethodSpecifierKind? kind = null)
        {
            bool isDeclaration;
            if (kind.HasValue)
                isDeclaration = kind == MethodSpecifierKind.Declaration;
            else
                isDeclaration = FileExtension == "h";

            if (isDeclaration)
            {
                if (method.IsVirtual || method.IsOverride)
                    Write("virtual ");

                if (method.IsStatic)
                    Write("static ");

                if (method.IsExplicit)
                    Write("explicit ");
            }

            if (method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion)
            {
                Write($"{GetMethodIdentifier(method)}(");
            }
            else
            {
                var returnType = method.ReturnType.Visit(CTypePrinter);
                Write($"{returnType} {GetMethodIdentifier(method)}(");
            }

            GenerateMethodParameters(method);

            Write(")");

            if (method.IsOverride && isDeclaration)
                Write(" override");
        }

        public virtual void GenerateMethodParameters(Function function)
        {
            Write(CTypePrinter.VisitParameters(function.Parameters)); 
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock(BlockKind.Method, method);

            GenerateMethodSpecifier(method);
            Write(";");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            if (!(property.HasGetter || property.HasSetter))
                return false;

            if (property.Field != null)
                return false;

            if (property.HasGetter)
                GeneratePropertyGetter(property.GetMethod);

            if (property.HasSetter)
                GeneratePropertySetter(property.SetMethod);

            //if (Options.GenerateMSDeclspecProperties)
                //GenerateMSDeclspecProperty(property);

            return true;
        }

        public virtual void GeneratePropertyAccessorSpecifier(Method method)
        {
            GenerateMethodSpecifier(method);
        }

        public virtual void GeneratePropertyGetter(Method method)
        {
            PushBlock(BlockKind.Method, method);

            GeneratePropertyAccessorSpecifier(method);
            WriteLine(";");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public virtual void GeneratePropertySetter(Method method)
        {
            PushBlock(BlockKind.Method, method);

            GeneratePropertyAccessorSpecifier(method);
            WriteLine(";");

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        private void GenerateMSDeclspecProperty(Property property)
        {
            PushBlock(BlockKind.Property, property);

            if (property.IsStatic)
                Write("static ");

            if (property.IsIndexer)
            {
                //GenerateIndexer(property);
                //throw new System.NotImplementedException();
            }
            else
            {
                var blocks = new List<string>();

                if (property.HasGetter)
                    blocks.Add($"get = {property.GetMethod.Name}");

                if (property.HasSetter)
                    blocks.Add($"put = {property.SetMethod.Name}");

                var getterSetter = string.Join(",", blocks);

                var type = property.QualifiedType.Visit(CTypePrinter);
                WriteLine($"__declspec(property({getterSetter})) {type} {property.Name};");
            }

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        static readonly List<string> CReservedKeywords = new List<string> {
            // C99 6.4.1: Keywords.
            "auto", "break", "case", "char", "const", "continue", "default",
            "do", "double", "else", "enum", "extern", "float", "for", "goto",
            "if", "inline", "int", "long", "register", "restrict", "return",
            "short", "signed", "sizeof", "static", "struct", "switch",
            "typedef", "union", "unsigned", "void", "volatile", "while",
            "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex",
            "_Generic", "_Imaginary", "_Noreturn", "_Static_assert",
             "_Thread_local", "__func__", "__objc_yes", "__objc_no",
        };

        public static bool IsReservedKeywordC(string id) => CReservedKeywords.Contains(id);

        static readonly List<string> CppReservedKeywords = new List<string> {
             // C++ 2.11p1: Keywords.
             "asm", "bool", "catch", "class", "const_cast", "delete",
             "dynamic_cast", "explicit", "export", "false", "friend",
             "mutable", "namespace", "new", "operator", "private",
             "protected", "public", "reinterpret_cast", "static_cast",
             "template", "this", "throw", "true", "try", "typename",
             "typeid", "using", "virtual", "wchar_t",

             // C++11 Keywords
             "alignas", "alignof", "char16_t", "char32_t", "constexpr",
             "decltype", "noexcept", "nullptr", "static_assert",
             "thread_local"
        };

        public static bool IsReservedKeywordCpp(string id) => CppReservedKeywords.Contains(id);

        public static bool IsReservedKeyword(string id) => IsReservedKeywordC(id) || IsReservedKeywordCpp(id);
    }
}
