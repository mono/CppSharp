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
                ResolveTypedefs = true,
                ResolveTypeMaps = false
            };

            cppTypePrinter.PushContext(TypePrinterContextKind.Native);
            cppTypePrinter.PushScope(TypePrintScopeKind.Qualified);
        }

        public override void Process()
        {
            if (TranslationUnit.Module == Options.SystemModule)
            {
                WriteLine("#define _LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS");
                WriteLine("#define _LIBCPP_HIDE_FROM_ABI");
                NewLine();
                WriteLine("#include <string>");
                WriteLine("#include <optional>");
            }
            else
                foreach (var header in TranslationUnit.Module.Headers)
                    WriteLine($"#include <{header}>");
            WriteLine("#include <new>");
            NewLine();
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (method.IsDestructor &&
                (!((Class)method.Namespace).HasNonTrivialDestructor ||
                 method.Access == AccessSpecifier.Private))
                return false;

            if (method.Namespace is ClassTemplateSpecialization &&
                (method.TranslationUnit.IsSystemHeader ||
                 (!method.IsImplicit && !method.IsDefaulted && !method.IsPure &&
                  string.IsNullOrEmpty(method.Body))))
            {
                WriteLine($"template {GetExporting()}{method.Visit(cppTypePrinter)};");
                return true;
            }

            Class @class = (Class)method.Namespace;
            bool needSubclass = (method.Access == AccessSpecifier.Protected ||
                @class.IsAbstract) && (method.IsConstructor || method.IsDestructor);
            string wrapper = GetWrapper(method);
            int i = 0;
            foreach (var param in method.Parameters.Where(
                p => string.IsNullOrEmpty(p.OriginalName)))
                param.Name = "_" + i++;
            var @params = string.Join(", ", method.Parameters.Select(CastIfRVReference));
            if (needSubclass)
            {
                string className = @class.Visit(cppTypePrinter);
                Write($"class {wrapper}{method.Namespace.Name} : public {className} {{ public: ");
                Write(wrapper + method.Namespace.Name);
                Write($@"({string.Join(", ", method.Parameters.Select(
                    p => cppTypePrinter.VisitParameter(p)))})");
                Write($": {className}({@params}) {{}}; ");

                ImplementIfAbstract(@class);
            }
            if (method.IsConstructor)
            {
                WrapConstructor(method, wrapper, @params);
                return true;
            }
            if (method.IsDestructor)
            {
                WrapDestructor(method, wrapper);
                return true;
            }
            TakeFunctionAddress(method, wrapper);
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            TakeFunctionAddress(function, GetWrapper(function));
            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!(variable.Namespace is ClassTemplateSpecialization specialization) ||
                specialization.SpecializationKind == TemplateSpecializationKind.ExplicitSpecialization)
                return false;

            WriteLine($"template {GetExporting()}{variable.Visit(cppTypePrinter)};");
            return true;
        }

        private string GetExporting()
        {
            return Context.ParserOptions.IsMicrosoftAbi ||
                (Context.ParserOptions.TargetTriple.IsWindows() &&
                 TranslationUnit.Module == Options.SystemModule) ?
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

        private void WrapConstructor(Method method, string wrapper, string @params)
        {
            if (Options.CheckSymbols)
            {
                method.Mangled = wrapper;
                method.CallingConvention = CallingConvention.C;
            }

            int i = 0;
            foreach (var param in method.Parameters.Where(
                p => string.IsNullOrEmpty(p.OriginalName)))
                param.Name = "_" + i++;
            var signature = string.Join(", ", method.GatherInternalParams(
                Context.ParserOptions.IsItaniumLikeAbi).Select(
                    p => cppTypePrinter.VisitParameter(p)));

            if (method.Access != AccessSpecifier.Protected)
                Write("extern \"C\" ");
            Write($"{GetExporting()}void {wrapper}({signature}) ");

            bool isAbstract = ((Class)method.Namespace).IsAbstract;
            if (method.Access == AccessSpecifier.Protected || isAbstract)
            {
                Write($@"{{ ::new ({Helpers.InstanceField}) {
                    wrapper}{method.Namespace.Name}({@params}); }}");
                WriteLine(!isAbstract ? " };" : string.Empty);
            }
            else
            {
                string @namespace = method.Namespace.Visit(cppTypePrinter);
                WriteLine($"{{ ::new ({Helpers.InstanceField}) {@namespace}({@params}); }}");
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

        private void WrapDestructor(Method method, string wrapper)
        {
            if (Options.CheckSymbols)
            {
                method.Mangled = wrapper;
                method.CallingConvention = CallingConvention.C;
            }

            bool needSubclass = method.Access == AccessSpecifier.Protected ||
                ((Class)method.Namespace).IsAbstract;
            string @namespace = method.Namespace.Visit(cppTypePrinter);
            if (!needSubclass)
                Write("extern \"C\" ");
            Write($"{GetExporting()}void {wrapper}");
            if (needSubclass)
                Write("Protected");

            string instance = Helpers.InstanceField;
            if (needSubclass)
            {
                string @class = wrapper + method.Namespace.Name;
                WriteLine($"() {{ this->~{@class}(); }} }};");
                Write($@"extern ""C"" {GetExporting()}void {wrapper}({
                    @class}* {instance}) {{ {instance}->{wrapper}Protected");
            }
            else
                Write($@"({$"{@namespace}*{instance}"}) {{ {
                    instance}->~{method.Namespace.Name}");
            WriteLine("(); }");
        }

        private void TakeFunctionAddress(Function function, string wrapper)
        {
            string @namespace = function.OriginalNamespace.Visit(cppTypePrinter);
            if (function.Access == AccessSpecifier.Protected)
            {
                Write($"class {wrapper}{function.Namespace.Name} : public {@namespace} {{ public: ");
                Write("static constexpr ");
            }
            cppTypePrinter.PrintTags = true;
            TypePrinterResult returnType = function.OriginalReturnType.Visit(cppTypePrinter);
            string signature = GetSignature(function);
            cppTypePrinter.PrintTags = false;

            string functionName = GetFunctionName(function);
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
                Write($" = &{wrapper}{function.Namespace.Name}::{function.OriginalName};");
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

        private string GetFunctionName(Function function)
        {
            var nameBuilder = new StringBuilder();
            nameBuilder.Append(function.Visit(cppTypePrinter).Name);

            if (function.SpecializationInfo != null)
                nameBuilder.Append('<').Append(string.Join(", ",
                    GetTemplateArguments(function.SpecializationInfo.Arguments))).Append('>');

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
                            return a.Type.Visit(cppTypePrinter).ToString();
                        case TemplateArgument.ArgumentKind.Declaration:
                            return a.Declaration.Visit(cppTypePrinter).ToString();
                        case TemplateArgument.ArgumentKind.Integral:
                            return ("(" + a.Type.Visit(cppTypePrinter) + ")") + a.Integral.ToString(CultureInfo.InvariantCulture);
                    }
                    throw new System.ArgumentOutOfRangeException(
                        nameof(a.Kind), a.Kind, "Unsupported kind of template argument.");
                });
        }

        private void WriteRedeclaration(Function function, string returnType,
            string paramTypes, string functionName)
        {
            Stack<string> parentsOpen = GenerateNamespace(function);
            var functionType = (FunctionType)function.FunctionType.Type;
            Write($"{string.Concat(parentsOpen)}");
            if (function.IsConstExpr)
                Write("constexpr ");
            Write($"{returnType} {function.OriginalName}{paramTypes}");
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

        private void ImplementIfAbstract(Class @class)
        {
            if (!@class.IsAbstract)
                return;

            Method abstractDtor = null;
            cppTypePrinter.MethodScopeKind = TypePrintScopeKind.Local;
            foreach (Method @abstract in @class.GetAbstractMethods())
            {
                if (@abstract.IsDestructor)
                {
                    abstractDtor = @abstract;
                    continue;
                }
                Write(@abstract.Visit(cppTypePrinter) + " {");
                Type returnType = @abstract.OriginalReturnType.Type.Desugar();
                if (returnType.IsReference())
                {
                    var pointee = returnType.GetPointee().Desugar().Visit(cppTypePrinter);
                    Write($" static bool __value = false; return ({pointee}&) __value; ");
                }
                else if (!returnType.IsPrimitiveType(PrimitiveType.Void))
                    Write(" return {}; ");
                Write("} ");
            }
            cppTypePrinter.MethodScopeKind = TypePrintScopeKind.Qualified;

            WriteLine(" };");
            if (abstractDtor != null && !implementedDtors.Contains(abstractDtor))
            {
                if (string.IsNullOrEmpty(abstractDtor.Body))
                {
                    WriteLine($"{abstractDtor.Namespace.Name}::{abstractDtor.Name}() {{}}");
                }
                implementedDtors.Add(abstractDtor);
            }
        }

        private CppTypePrinter cppTypePrinter;

        private int functionCount;
        private HashSet<Method> implementedDtors = new HashSet<Method>();
    }
}
