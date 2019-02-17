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
            return string.Format(Kind == IncludeKind.Angled ?
                "#include <{0}>" : "#include \"{0}\"", File);
        }
    }

    public abstract class CCodeGenerator : CodeGenerator
    {
        public CCodeGenerator(BindingContext context)
            : base(context)
        {
            VisitOptions.VisitPropertyAccessors = true;
        }

        public virtual string QualifiedName(Declaration decl)
        {
            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
                return decl.Name;

            return decl.QualifiedName;
        }

        public override string GeneratedIdentifier(string id)
        {
            return "__" + id.Replace('-', '_');
        }

        private CppTypePrinter typePrinter = new CppTypePrinter();
        public virtual CppTypePrinter CTypePrinter => typePrinter;

        public virtual void WriteHeaders() { }

        public virtual void WriteInclude(CInclude include)
        {
            WriteLine(include.ToString());
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return decl.IsGenerated && !AlreadyVisited(decl);
        }

        public virtual string GetMethodIdentifier(Method method) => method.Name;

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            var retType = method.ReturnType.Visit(CTypePrinter);
            Write($"{retType} {GetMethodIdentifier(method)}(");
            Write(CTypePrinter.VisitParameters(method.Parameters));
            Write(")");
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!VisitDeclaration(typedef))
                return false;

            PushBlock();

            var typeName = typedef.Type.Visit(CTypePrinter);
            WriteLine($"typedef {typeName} {typedef};");

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

            PushBlock();

            var useTypedefEnum = Options.GeneratorKind == GeneratorKind.C;
            var enumName = Options.GeneratorKind != GeneratorKind.CPlusPlus ?
                QualifiedName(@enum) : @enum.Name;

            var enumKind = @enum.IsScoped ? "enum class" : "enum";

            if (useTypedefEnum)
                Write($"typedef {enumKind} {enumName}");
            else
                Write($"{enumKind} {enumName}");

            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
            {
                var typeName = CTypePrinter.VisitPrimitiveType(
                    @enum.BuiltinType.Type, new TypeQualifiers());

                if (@enum.BuiltinType.Type != PrimitiveType.Int)
                    Write($" : {typeName}");
            }

            NewLine();
            WriteOpenBraceAndIndent();

            foreach (var item in @enum.Items)
            {
                if (!item.IsGenerated)
                    continue;

                var enumItemName = Options.GeneratorKind != GeneratorKind.CPlusPlus ?
                    $"{@enum.QualifiedName}_{item.Name}" : item.Name;

                Write(enumItemName);

                if (item.ExplicitValue)
                    Write($" = {@enum.GetItemValueAsString(item)}");

                if (item != @enum.Items.Last())
                    WriteLine(",");
            }

            NewLine();
            Unindent();

            if (!string.IsNullOrWhiteSpace(enumName) && useTypedefEnum)
                WriteLine($"}} {enumName};");
            else
                WriteLine($"}};");

            PopBlock(NewLineKind.BeforeNextBlock);

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

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();

            if (Options.GeneratorKind == GeneratorKind.CLI)
            {
                keywords.Add(AccessIdentifier(@class.Access));

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

            VisitDeclContext(@class);

            Unindent();
            WriteLine("};");

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
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
            return true;
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

        public bool IsReservedKeywordC(string id) => CReservedKeywords.Contains(id);

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

        public bool IsReservedKeywordCpp(string id) => CppReservedKeywords.Contains(id);

        public bool IsReservedKeyword(string id) => IsReservedKeywordC(id) || IsReservedKeywordCpp(id);
    }
}
