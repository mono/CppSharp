using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Types;

namespace CppSharp.Passes
{
    public class HandleDefaultParamValuesPass : TranslationUnitPass
    {
        private static readonly Regex regexFunctionParams = new Regex(@"\(?(.+)\)?", RegexOptions.Compiled);
        private static readonly Regex regexDoubleColon = new Regex(@"\w+::", RegexOptions.Compiled);
        private static readonly Regex regexName = new Regex(@"(\w+)", RegexOptions.Compiled);

        private readonly Dictionary<DeclarationContext, List<Function>> overloads =
            new Dictionary<DeclarationContext, List<Function>>();

        public HandleDefaultParamValuesPass()
        {
            VisitOptions.VisitFunctionParameters = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsGenerated)
                return false;
            var result = base.VisitTranslationUnit(unit);
            foreach (var overload in overloads)
                overload.Key.Declarations.AddRange(overload.Value);
            overloads.Clear();
            return result;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || function.Ignore)
                return false;

            var overloadIndices = new List<int>(function.Parameters.Count);
            foreach (var parameter in function.Parameters.Where(p => p.DefaultArgument != null))
            {
                Type type = parameter.Type.Desugar(resolveTemplateSubstitution: false);
                type = (type.GetFinalPointee() ?? type).Desugar(
                    resolveTemplateSubstitution: false);
                if (type is TemplateParameterSubstitutionType)
                {
                    parameter.DefaultArgument = null;
                    continue;
                }

                var result = parameter.DefaultArgument.String;
                if (PrintExpression(function, parameter.Type,
                        parameter.OriginalDefaultArgument, ref result) == null)
                    overloadIndices.Add(function.Parameters.IndexOf(parameter));
                if (string.IsNullOrEmpty(result))
                {
                    parameter.DefaultArgument = null;
                    foreach (var p in function.Parameters.TakeWhile(p => p != parameter))
                        p.DefaultArgument = null;
                }
                else
                    parameter.DefaultArgument.String = result;
            }

            GenerateOverloads(function, overloadIndices);

            return true;
        }

        private bool? PrintExpression(Function function, Type type, ExpressionObsolete expression, ref string result)
        {
            var desugared = type.Desugar();

            if (desugared.IsAddress() && !desugared.IsPrimitiveTypeConvertibleToRef() &&
                (expression.String == "0" || expression.String == "nullptr"))
            {
                result = desugared.GetPointee()?.Desugar() is FunctionType ?
                    "null" : $"default({desugared})";
                return true;
            }

            // constants are obtained through dynamic calls at present so they are not compile-time values in target languages
            if (expression.Declaration is Variable ||
                (!Options.MarshalCharAsManagedChar &&
                 desugared.IsPrimitiveType(PrimitiveType.UChar)))
                return null;

            if (desugared.IsPrimitiveTypeConvertibleToRef())
            {
                var method = function as Method;
                if (method != null && method.IsConstructor)
                {
                    result = string.Empty;
                    return false;
                }
                return null;
            }

            if (CheckForDefaultPointer(desugared, ref result))
                return true;

            if (expression.Class == StatementClass.Call)
            {
                if (expression.Declaration.Ignore)
                {
                    result = null;
                    return false;
                }
                return null;
            }

            var defaultConstruct = CheckForDefaultConstruct(desugared, expression, ref result);
            if (defaultConstruct != false)
                return defaultConstruct;

            return CheckForSimpleExpressions(expression, ref result, desugared);
        }

        private bool CheckForSimpleExpressions(ExpressionObsolete expression, ref string result, Type desugared)
        {
            return CheckFloatSyntax(desugared, expression, ref result) ||
                CheckForEnumValue(desugared, expression, ref result) ||
                CheckForDefaultChar(desugared, ref result);
        }

        private bool CheckForDefaultPointer(Type desugared, ref string result)
        {
            if (!desugared.IsPointer())
                return false;

            if (desugared.IsPrimitiveTypeConvertibleToRef())
                return false;

            Class @class;
            if (desugared.GetFinalPointee().TryGetClass(out @class) && @class.IsValueType)
            {
                result = $"new {@class.Visit(new CSharpTypePrinter(Context))}()";
                return true;
            }

            return false;
        }

        private bool? CheckForDefaultConstruct(Type desugared, ExpressionObsolete expression,
            ref string result)
        {
            var type = desugared.GetFinalPointee() ?? desugared;

            Class decl;
            if (!type.TryGetClass(out decl))
                return false;

            var ctor = expression as CXXConstructExprObsolete;

            var typePrinter = new CSharpTypePrinter(Context);
            typePrinter.PushMarshalKind(MarshalKind.DefaultExpression);

            var typePrinterResult = type.Visit(typePrinter).Type;

            TypeMap typeMap;
            if (TypeMaps.FindTypeMap(type, out typeMap))
            {
                var typePrinterContext = new TypePrinterContext()
                {
                    Kind = typePrinter.Kind,
                    MarshalKind = typePrinter.MarshalKind,
                    Type = type
                };

                var typeInSignature = typeMap.CSharpSignatureType(typePrinterContext)
                    .SkipPointerRefs().Desugar();

                Enumeration @enum;
                if (typeInSignature.TryGetEnum(out @enum))
                {
                    if (ctor != null &&
                        (ctor.Arguments.Count == 0 ||
                         HasSingleZeroArgExpression((Function) ctor.Declaration)))
                    {
                        result = "0";
                        return true;
                    }
                    return false;
                }

                if (ctor != null && typePrinterResult == "string" && ctor.Arguments.Count == 0)
                {
                    result = "\"\"";
                    return true;
                }
            }

            if (ctor == null)
            {
                CheckForSimpleExpressions(expression, ref result, desugared);
                return decl.IsValueType ? (bool?) false : null;
            }

            var method = (Method) expression.Declaration;
            var expressionSupported = decl.IsValueType && method.Parameters.Count == 0;

            if (expression.String.Contains('(') || expression.String.StartsWith("{"))
            {
                var argsBuilder = new StringBuilder("new ");
                argsBuilder.Append(typePrinterResult);
                argsBuilder.Append('(');
                for (var i = 0; i < ctor.Arguments.Count; i++)
                {
                    var argument = ctor.Arguments[i];
                    var argResult = argument.String;
                    expressionSupported &= PrintExpression(method,
                        method.Parameters[i].Type.Desugar(), argument, ref argResult) ?? false;
                    argsBuilder.Append(argResult);
                    if (i < ctor.Arguments.Count - 1)
                        argsBuilder.Append(", ");
                }
                argsBuilder.Append(')');
                result = argsBuilder.ToString();
            }
            return expressionSupported ? true : (bool?) null;
        }

        private static bool CheckFloatSyntax(Type desugared, Statement statement, ref string result)
        {
            var builtin = desugared as BuiltinType;
            if (builtin != null)
            {
                switch (builtin.Type)
                {
                    case PrimitiveType.Float:
                        if (statement.String.EndsWith(".F", System.StringComparison.Ordinal))
                        {
                            result = statement.String.Replace(".F", ".0F");
                            return true;
                        }
                        break;
                    case PrimitiveType.Double:
                        if (statement.String.EndsWith(".", System.StringComparison.Ordinal))
                        {
                            result = statement.String + '0';
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        private bool CheckForEnumValue(Type desugared, Statement statement,
            ref string result)
        {
            var enumItem = statement.Declaration as Enumeration.Item;
            if (enumItem != null)
                return true;

            var call = statement.Declaration as Function;
            if (call != null && statement.String != "0")
            {
                var @params = regexFunctionParams.Match(statement.String).Groups[1].Value;
                result = TranslateEnumExpression(desugared, @params);
                return true;
            }

            if (desugared.TryGetEnum(out Enumeration @enum) &&
                int.TryParse(statement.String, out int value))
            {
                var typePrinter = new CSharpTypePrinter(Context);
                var printedEnum = @enum.Visit(typePrinter);
                if (value < 0)
                    switch (@enum.BuiltinType.Type)
                    {
                        case PrimitiveType.UShort:
                        case PrimitiveType.UInt:
                        case PrimitiveType.ULong:
                        case PrimitiveType.ULongLong:
                        case PrimitiveType.UInt128:
                            result = $@"({printedEnum}) unchecked(({
                                @enum.BuiltinType.Visit(typePrinter)}) {
                                statement.String})";
                            break;
                        default:
                            result = $"({printedEnum}) ({statement.String})";
                            break;
                    }
                else
                    result = $"({printedEnum}) {statement.String}";
                return true;
            }

            return false;
        }

        private string TranslateEnumExpression(Type desugared, string @params)
        {
            if (@params.Contains("::"))
                return regexDoubleColon.Replace(@params, desugared + ".");

            return regexName.Replace(@params, desugared + ".$1");
        }

        private static bool HasSingleZeroArgExpression(Function function)
        {
            if (function.Parameters.Count != 1)
                return false;

            var defaultArgument = function.Parameters[0].OriginalDefaultArgument;
            return defaultArgument is BuiltinTypeExpressionObsolete &&
                ((BuiltinTypeExpressionObsolete) defaultArgument).Value == 0;
        }

        private bool CheckForDefaultChar(Type desugared, ref string result)
        {
            int value;
            if (int.TryParse(result, out value) &&
                ((Options.MarshalCharAsManagedChar &&
                 desugared.IsPrimitiveType(PrimitiveType.Char)) ||
                 desugared.IsPrimitiveType(PrimitiveType.WideChar)))
            {
                result = value == 0 ? "'\\0'" : ("(char) " + result);
                return true;
            }

            return false;
        }

        private void GenerateOverloads(Function function, List<int> overloadIndices)
        {
            foreach (var overloadIndex in overloadIndices)
            {
                var method = function as Method;
                Function overload = method != null ? new Method(method) : new Function(function);
                overload.OriginalFunction = function;
                overload.SynthKind = FunctionSynthKind.DefaultValueOverload;
                for (int i = overloadIndex; i < function.Parameters.Count; ++i)
                    overload.Parameters[i].GenerationKind = GenerationKind.None;

                var indices = overloadIndices.Where(i => i < overloadIndex).ToList();
                if (indices.Any())
                    for (int i = 0; i <= indices.Last(); i++)
                        if (i != overloadIndex)
                            overload.Parameters[i].DefaultArgument = null;

                if (method != null)
                    ((Class) function.Namespace).Methods.Add((Method) overload);
                else
                {
                    List<Function> functions;
                    if (overloads.ContainsKey(function.Namespace))
                        functions = overloads[function.Namespace];
                    else
                        overloads.Add(function.Namespace, functions = new List<Function>());
                    functions.Add(overload);
                }

                for (int i = 0; i <= overloadIndex; i++)
                    function.Parameters[i].DefaultArgument = null;
            }
        }
    }
}
