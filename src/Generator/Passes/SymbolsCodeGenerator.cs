using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;

namespace CppSharp.Passes
{
    public class SymbolsCodeGenerator : CodeGenerator
    {
        public override string FileExtension => "cpp";

        public SymbolsCodeGenerator(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            cppTypePrinter = new CppTypePrinter(Context)
            {
                ScopeKind = TypePrintScopeKind.Qualified,
                ResolveTypedefs = true,
                ResolveTypeMaps = false
            };

            cppTypePrinter.PushContext(TypePrinterContextKind.Native);
        }

        public override void Process()
        {
            WriteLine("#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS");
            WriteLine("#define _LIBCPP_HIDE_FROM_ABI");
            NewLine();

            if (TranslationUnit.Module == Options.SystemModule)
                WriteLine("#include <string>");
            else
                foreach (var header in TranslationUnit.Module.Headers)
                    WriteLine($"#include <{header}>");
            WriteLine("#include <new>");
            NewLine();
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (method.IsDestructor &&
                (!((Class) method.Namespace).HasNonTrivialDestructor ||
                 method.Access == AccessSpecifier.Private))
                return false;

            if (method.Namespace is ClassTemplateSpecialization &&
                (method.TranslationUnit.IsSystemHeader ||
                 ((method.IsConstructor || method.IsDestructor) &&
                  !method.IsImplicit && !method.IsDefaulted && !method.IsPure &&
                  string.IsNullOrEmpty(method.Body))))
            {
                WriteLine($"template {GetExporting()}{method.Visit(cppTypePrinter)};");
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

        private string GetExporting()
        {
            return Context.ParserOptions.IsMicrosoftAbi ?
                "__declspec(dllexport) " : string.Empty;
        }

        private string GetWrapper(Function function)
        {
            if (function is Method method && (method.IsConstructor || method.IsDestructor))
            {
                var nameBuilder = new StringBuilder(method.USR);
                for (int i = 0; i < nameBuilder.Length; i++)
                    if (!char.IsLetterOrDigit(nameBuilder[i]) &&
                        nameBuilder[i] != '_')
                    {
                        if (nameBuilder[i] == '&')
                            nameBuilder.Insert(i + 1, '_');
                        nameBuilder[i] = '_';
                    }
                string @class = function.Namespace.Name;
                nameBuilder.Replace("c__S_", string.Empty).Replace(
                    $"{@class}_F_", $"{@class}_").TrimUnderscores();
                if (function.IsOperator)
                    nameBuilder.Append(function.OperatorKind);
                return nameBuilder.ToString();
            }
            return $"_{functionCount++}";
        }

        private static string GetDerivedType(string @namespace, string wrapper)
        {
            return $"class {wrapper}{@namespace} : public {@namespace} {{ public: ";
        }

        private void WrapConstructor(Method method)
        {
            string wrapper = GetWrapper(method);
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
                Write(wrapper + @namespace);
                Write($@"({string.Join(", ", method.Parameters.Select(
                    p => cppTypePrinter.VisitParameter(p)))})");
                WriteLine($": {@namespace}({@params}) {{}} }};");
                Write($"extern \"C\" {{ void {wrapper}({signature}) ");
                WriteLine($"{{ new ({Helpers.InstanceField}) {wrapper}{@namespace}({@params}); }} }}");
            }
            else
            {
                Write("extern \"C\" ");
                if (method.Namespace.Access == AccessSpecifier.Protected)
                    Write($@"{{ class {wrapper}{method.Namespace.Namespace.Name} : public {
                        method.Namespace.Namespace.Visit(cppTypePrinter)} ");
                Write($"{{ void {wrapper}({signature}) ");
                Write($"{{ new ({Helpers.InstanceField}) {@namespace}({@params}); }} }}");
                if (method.Namespace.Access == AccessSpecifier.Protected)
                    Write("; }");
                NewLine();
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
            string wrapper = GetWrapper(method);
            if (Options.CheckSymbols)
            {
                method.Mangled = wrapper;
                method.CallingConvention = CallingConvention.C;
            }

            bool isProtected = method.Access == AccessSpecifier.Protected;
            string @namespace = method.Namespace.Visit(cppTypePrinter);
            if (isProtected)
                Write($"class {wrapper} : public {@namespace} {{ public: ");
            else
                Write("extern \"C\" { ");
            if (method.Namespace.Access == AccessSpecifier.Protected)
                Write($@"class {wrapper}{method.Namespace.Namespace.Name} : public {
                    method.Namespace.Namespace.Visit(cppTypePrinter)} {{ ");
            Write($"void {wrapper}");
            if (isProtected)
                Write("Protected");

            string instance = Helpers.InstanceField;
            Write($@"({(isProtected ? wrapper : @namespace)}* {
                instance}) {{ delete {instance}; }} }};");
            if (isProtected)
            {
                NewLine();
                Write($@"extern ""C"" {{ void {wrapper}({wrapper}* {instance}) {{ {
                    instance}->{wrapper}Protected({instance}); }} }}");
            }
            if (method.Namespace.Access == AccessSpecifier.Protected)
                Write("; }");
            NewLine();
        }

        private void TakeFunctionAddress(Function function)
        {
            string wrapper = GetWrapper(function);
            string @namespace = function.OriginalNamespace.Visit(cppTypePrinter);
            if (function.Access == AccessSpecifier.Protected)
            {
                Write($"class {wrapper}{function.Namespace.Name} : public {@namespace} {{ public: ");
                Write("static constexpr ");
            }
            TypePrinterResult returnType = function.OriginalReturnType.Visit(cppTypePrinter);
            string signature = GetSignature(function);

            string functionName = GetFunctionName(function, @namespace);
            if (function.FriendKind != FriendKind.None)
                WriteRedeclaration(function, returnType, signature, functionName);

            var method = function as Method;
            if (function.Namespace.Access == AccessSpecifier.Protected)
                Write($@"class {wrapper}{function.Namespace.Name} : public {
                    function.Namespace.Namespace.Visit(cppTypePrinter)} {{ ");

            string variable = $@"({(method?.IsStatic == false ?
                (@namespace + "::") : string.Empty)}*{wrapper}){signature}";
            returnType.NameSuffix.Append(' ').Append(variable);
            Write(returnType);

            if (function.Access == AccessSpecifier.Protected)
            {
                Write($" = &{wrapper}{function.Namespace.Name}::{functionName};");
                WriteLine(" };");
                Write($"auto {wrapper}Protected = {wrapper}{function.Namespace.Name}::{wrapper};");
            }
            else
            {
                Write($" = &{functionName};");
            }
            if (function.Namespace.Access == AccessSpecifier.Protected)
                Write(" };");
            NewLine();
        }

        private string GetSignature(Function function)
        {
            var method = function as Method;

            var paramTypes = string.Join(", ", function.Parameters.Where(
                p => p.Kind == ParameterKind.Regular).Select(
                cppTypePrinter.VisitParameterDecl));

            var variadicType = function.IsVariadic ?
                (function.Parameters.Any(
                    p => p.Kind == ParameterKind.Regular) ? ", ..." : "...") :
                string.Empty;

            var @const = method?.IsConst == true ? " const" : string.Empty;

            var refQualifier = method == null || method.RefQualifier == RefQualifier.None ?
                string.Empty : (method.RefQualifier == RefQualifier.LValue ? " &" : " &&");

            return $"({paramTypes}{variadicType}){@const}{refQualifier}";
        }

        private string GetFunctionName(Function function, string @namespace)
        {
            var nameBuilder = new StringBuilder();
            if (function.Access != AccessSpecifier.Protected &&
                !string.IsNullOrEmpty(@namespace))
                nameBuilder.Append(@namespace).Append("::");

            bool isConversionToSpecialization =
                (function.OperatorKind == CXXOperatorKind.Conversion ||
                 function.OperatorKind == CXXOperatorKind.ExplicitConversion) &&
                function.OriginalReturnType.Type.Desugar(
                    ).TryGetDeclaration(out ClassTemplateSpecialization specialization);

            nameBuilder.Append(isConversionToSpecialization ?
                "operator " : function.OriginalName);

            if (function.SpecializationInfo != null)
                nameBuilder.Append('<').Append(string.Join(", ",
                    GetTemplateArguments(function.SpecializationInfo.Arguments))).Append('>');
            else if (isConversionToSpecialization)
                nameBuilder.Append(function.OriginalReturnType.Visit(cppTypePrinter));

            return nameBuilder.ToString();
        }

        private IEnumerable<string> GetTemplateArguments(
            IEnumerable<TemplateArgument> templateArguments)
        {
            return templateArguments.Select(
                a =>
                {
                    switch (a.Kind)
                    {
                        case TemplateArgument.ArgumentKind.Type:
                            return a.Type.Visit(cppTypePrinter).Type;
                        case TemplateArgument.ArgumentKind.Declaration:
                            return a.Declaration.Visit(cppTypePrinter).Type;
                        case TemplateArgument.ArgumentKind.Integral:
                            return a.Integral.ToString(CultureInfo.InvariantCulture);
                    }
                    throw new System.ArgumentOutOfRangeException(
                        nameof(a.Kind), a.Kind, "Unsupported kind of template argument.");
                });
        }

        private void WriteRedeclaration(Function function, string returnType,
            string paramTypes, string functionName)
        {
            Stack<string> parentsOpen = GenerateNamespace(function);
            var functionType = (FunctionType) function.FunctionType.Type;
            Write($"{string.Concat(parentsOpen)}");
            if (function.IsConstExpr)
                Write("constexpr ");
            Write(returnType);
            Write(" ");
            Write(parentsOpen.Count > 0 ? function.OriginalName : functionName);
            Write(paramTypes);
            if (functionType.ExceptionSpecType == ExceptionSpecType.BasicNoexcept)
                Write(" noexcept");
            WriteLine($";{string.Concat(parentsOpen.Select(_ => " }"))}");
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
            var nestedNamespace = declarationContextsInSignature.Find(d =>
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

        private CppTypePrinter cppTypePrinter;

        private int functionCount;
    }
}
