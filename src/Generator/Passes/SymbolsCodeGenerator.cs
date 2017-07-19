using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Parser;

namespace CppSharp.Passes
{
    public class SymbolsCodeGenerator : CodeGenerator
    {
        public override string FileExtension => "cpp";

        public SymbolsCodeGenerator(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void Process()
        {
            if (TranslationUnit.Module == Options.SystemModule)
                WriteLine("#include <string>");
            else
                foreach (var header in TranslationUnit.Module.Headers)
                    WriteLine($"#include <{header}>");
            NewLine();
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (method.Namespace is ClassTemplateSpecialization)
            {
                var exporting = string.Empty;
                if (Context.ParserOptions.IsMicrosoftAbi)
                    exporting = "__declspec(dllexport) ";
                else if (TargetTriple.IsMacOS(Context.ParserOptions.TargetTriple))
                    exporting = "__attribute__((visibility(\"default\"))) ";
                WriteLine($"template {exporting}{method.Visit(cppTypePrinter)};");
                return true;
            }
            if (method.IsConstructor)
            {
                WrapConstructor(method);
                return true;
            }
            if (method.IsDestructor)
            {
                WrapDestructor(method);
                return true;
            }
            return this.VisitFunctionDecl(method);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            TakeFunctionAddress(function);
            return true;
        }

        private string GetWrapper(Module module)
        {
            var symbolsLibraryName = new StringBuilder(module.SymbolsLibraryName);
            for (int i = 0; i < symbolsLibraryName.Length; i++)
                if (!char.IsLetterOrDigit(symbolsLibraryName[i]))
                    symbolsLibraryName[i] = '_';
            return $"{symbolsLibraryName}{++functionCount}";
        }

        private static string GetDerivedType(string @namespace, string wrapper)
        {
            return $@"class {wrapper}{@namespace} : public {@namespace} {{ public: ";
        }

        private void WrapConstructor(Method method)
        {
            string wrapper = GetWrapper(method.TranslationUnit.Module);
            if (Options.CheckSymbols)
            {
                method.Mangled = wrapper;
                method.CallingConvention = CallingConvention.C;
            }

            int i = 0;
            foreach (var param in method.Parameters.Where(
                p => string.IsNullOrEmpty(p.OriginalName)))
                param.Name = "_" + i++;
            var @params = string.Join(", ", method.Parameters.Select(CastIfRVReference));
            var signature = string.Join(", ", method.GatherInternalParams(
                Context.ParserOptions.IsItaniumLikeAbi).Select(
                    p => cppTypePrinter.VisitParameter(p)));

            string @namespace = method.Namespace.Visit(cppTypePrinter);
            if (method.Access == AccessSpecifier.Protected)
            {
                Write(GetDerivedType(@namespace, wrapper));
                Write($"{wrapper}{@namespace}");
                Write($@"({(string.Join(", ", method.Parameters.Select(
                    p => cppTypePrinter.VisitParameter(p))))})");
                WriteLine($": {@namespace}({@params}) {{}} }};");
                Write($"extern \"C\" {{ void {wrapper}({signature}) ");
                WriteLine($"{{ new (instance) {wrapper}{@namespace}({@params}); }} }}");
            }
            else
            {
                Write($"extern \"C\" {{ void {wrapper}({signature}) ");
                WriteLine($"{{ new (instance) {@namespace}({@params}); }} }}");
            }

            foreach (var param in method.Parameters.Where(p =>
                string.IsNullOrEmpty(p.OriginalName)))
                param.Name = param.OriginalName;
        }

        private string CastIfRVReference(Parameter p)
        {
            var pointer = p.Type.Desugar() as PointerType;
            if (pointer == null ||
                pointer.Modifier != PointerType.TypeModifier.RVReference)
                return p.Name;

            return $@"({pointer.Visit(
                cppTypePrinter, p.QualifiedType.Qualifiers)}) {p.Name}";
        }

        private void WrapDestructor(Method method)
        {
            string wrapper = GetWrapper(method.TranslationUnit.Module);
            if (Options.CheckSymbols)
            {
                method.Mangled = wrapper;
                method.CallingConvention = CallingConvention.C;
            }

            bool isProtected = method.Access == AccessSpecifier.Protected;
            string @namespace = method.Namespace.Visit(cppTypePrinter);
            if (isProtected)
                Write($"{GetDerivedType(@namespace, wrapper)}");
            else
                Write("extern \"C\" { ");
            Write($"void {wrapper}");
            if (isProtected)
                Write("protected");
            WriteLine($@"({@namespace}* instance) {{ instance->~{
                method.Namespace.OriginalName}(); }} }}");
            if (isProtected)
                WriteLine($@"void {wrapper}({@namespace} instance) {{ {
                    wrapper}{@namespace}::{wrapper}protected(instance); }}");
        }

        private void TakeFunctionAddress(Function function)
        {
            string wrapper = GetWrapper(function.TranslationUnit.Module);
            string @namespace = function.Namespace.Visit(cppTypePrinter);
            if (function.Access == AccessSpecifier.Protected)
            {
                Write(GetDerivedType(@namespace, wrapper));
                Write("static constexpr ");
            }

            string returnType = function.OriginalReturnType.Visit(cppTypePrinter);
            bool ambiguity = function.Namespace is TranslationUnit ||
                function.Namespace.GetOverloads(function).Count() > 1 ||
                function.FriendKind != FriendKind.None;
            string signature = ambiguity ? GetSignature(function) : string.Empty;

            string functionName = GetFunctionName(function, @namespace);
            if (function.FriendKind != FriendKind.None)
                WriteRedeclaration(function, returnType, signature, functionName);

            var method = function as Method;
            if (ambiguity)
                Write($@"{returnType} ({(method != null && !method.IsStatic ?
                    (@namespace + "::") : string.Empty)}*{wrapper}){signature}");
            else
                Write($@"auto {wrapper}");
            Write($@" = &{functionName};");
            if (function.Access == AccessSpecifier.Protected)
            {
                WriteLine(" };");
                Write($"auto {wrapper}protected = {wrapper}{@namespace}::{wrapper};");
            }
            NewLine();
        }

        private string GetSignature(Function function)
        {
            var method = function as Method;

            var paramTypes = string.Join(", ", function.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).Select(
                p => cppTypePrinter.VisitParameterDecl(p)));

            var variadicType = function.IsVariadic ?
                (function.Parameters.Where(
                    p => p.Kind == ParameterKind.Regular).Any() ? ", ..." : "...") :
                string.Empty;

            var @const = method != null && method.IsConst ? " const" : string.Empty;

            var refQualifier = method == null || method.RefQualifier == RefQualifier.None ?
                string.Empty : (method.RefQualifier == RefQualifier.LValue ? " &" : " &&");

            return $@"({paramTypes}{variadicType}){@const}{refQualifier}";
        }

        private string GetFunctionName(Function function, string @namespace)
        {
            return $@"{(function.Access == AccessSpecifier.Protected ||
                string.IsNullOrEmpty(@namespace) ?
                string.Empty : (@namespace + "::"))}{function.OriginalName}{
                (function.SpecializationInfo == null ? string.Empty : $@"<{
                    string.Join(", ", function.SpecializationInfo.Arguments.Select(
                        a => a.Type.Visit(cppTypePrinter)))}>")}";
        }

        private void WriteRedeclaration(Function function, string returnType,
            string paramTypes, string functionName)
        {
            Stack<string> parentsOpen = GenerateNamespace(function);
            var functionType = (FunctionType) function.FunctionType.Type;
            Write($@"{string.Join(string.Empty, parentsOpen)}");
            if (function.IsConstExpr)
                Write("constexpr ");
            Write(returnType);
            Write(" ");
            Write(parentsOpen.Any() ? function.OriginalName : functionName);
            Write(paramTypes);
            if (functionType.ExceptionSpecType == ExceptionSpecType.BasicNoexcept)
                Write(" noexcept");
            WriteLine($";{string.Join(string.Empty, parentsOpen.Select(p => " }"))}");
        }

        private static Stack<string> GenerateNamespace(Function function)
        {
            var declarationContextsInSignature = new List<DeclarationContext> { function.Namespace };
            var typesInSignature = new List<Type> { function.OriginalReturnType.Type };
            typesInSignature.AddRange(function.Parameters.Select(p => p.Type));
            foreach (var type in typesInSignature)
            {
                Declaration declaration;
                var finalType = (type.Desugar().GetFinalPointee() ?? type).Desugar();
                if (finalType.TryGetDeclaration(out declaration))
                    declarationContextsInSignature.Add(declaration.Namespace);
            }
            var nestedNamespace = declarationContextsInSignature.FirstOrDefault(d =>
                d.Namespace is Namespace && !(d.Namespace is TranslationUnit));
            var parentsOpen = new Stack<string>();
            if (nestedNamespace != null)
            {
                var @namespace = nestedNamespace;
                while (!(@namespace is Namespace))
                    @namespace = @namespace.Namespace;
                while (!(@namespace is TranslationUnit))
                {
                    parentsOpen.Push($"namespace {@namespace.OriginalName} {{ ");
                    @namespace = @namespace.Namespace;
                }
            }
            return parentsOpen;
        }

        private CppTypePrinter cppTypePrinter = new CppTypePrinter
        {
            PrintScopeKind = TypePrintScopeKind.Qualified
        };
        private int functionCount;
    }
}
