using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Types;

namespace CppSharp.Internal
{
    internal class ExpressionHelper
    {
        private static readonly Regex regexFunctionParams = new Regex(@"\(?(.+)\)?", RegexOptions.Compiled);
        private static readonly Regex regexDoubleColon = new Regex(@"\w+::", RegexOptions.Compiled);
        private static readonly Regex regexName = new Regex(@"(\w+)", RegexOptions.Compiled);

        public static bool? PrintExpression(BindingContext context, Function function, Type type, ExpressionObsolete expression, bool allowDefaultLiteral, ref string result)
        {
            var desugared = type.Desugar();

            if (desugared.IsAddress() && !desugared.IsPrimitiveTypeConvertibleToRef() &&
                (expression.String == "0" || expression.String == "nullptr"))
            {
                result = desugared.GetPointee()?.Desugar() is FunctionType ?
                    "null" : (allowDefaultLiteral ? "default" : $"default({desugared})");
                return true;
            }

            // constants are obtained through dynamic calls at present so they are not compile-time values in target languages
            if (expression.Declaration is Variable ||
                (!context.Options.MarshalCharAsManagedChar &&
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

            if (CheckForDefaultPointer(context, desugared, ref result))
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

            var defaultConstruct = CheckForDefaultConstruct(context, desugared, expression, ref result);
            if (defaultConstruct != false)
                return defaultConstruct;

            return CheckForSimpleExpressions(context, expression, ref result, desugared);
        }

        private static bool CheckForSimpleExpressions(BindingContext context, ExpressionObsolete expression, ref string result, Type desugared)
        {
            return CheckFloatSyntax(desugared, expression, ref result) ||
                CheckForEnumValue(context, desugared, expression, ref result) ||
                CheckForDefaultChar(context, desugared, ref result);
        }

        private static bool CheckForDefaultPointer(BindingContext context, Type desugared, ref string result)
        {
            if (!desugared.IsPointer())
                return false;

            if (desugared.IsPrimitiveTypeConvertibleToRef())
                return false;

            Class @class;
            if (desugared.GetFinalPointee().TryGetClass(out @class) && @class.IsValueType)
            {
                result = $"new {@class.Visit(new CSharpTypePrinter(context))}()";
                return true;
            }

            return false;
        }

        private static bool? CheckForDefaultConstruct(BindingContext context, Type desugared, ExpressionObsolete expression,
            ref string result)
        {
            var type = desugared.GetFinalPointee() ?? desugared;

            Class decl;
            if (!type.TryGetClass(out decl))
                return false;

            var ctor = expression as CXXConstructExprObsolete;

            var typePrinter = new CSharpTypePrinter(context);
            typePrinter.PushMarshalKind(MarshalKind.DefaultExpression);

            var typePrinterResult = type.Visit(typePrinter).Type;

            TypeMap typeMap;
            if (context.TypeMaps.FindTypeMap(type, out typeMap))
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
                         HasSingleZeroArgExpression((Function)ctor.Declaration)))
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
                CheckForSimpleExpressions(context, expression, ref result, desugared);
                return decl.IsValueType ? (bool?)false : null;
            }

            var method = (Method)expression.Declaration;
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

                    expressionSupported &= PrintExpression(context, method,
                        method.Parameters[i].Type.Desugar(), argument, allowDefaultLiteral: false, ref argResult) ?? false;
                    argsBuilder.Append(argResult);
                    if (i < ctor.Arguments.Count - 1)
                        argsBuilder.Append(", ");
                }
                argsBuilder.Append(')');
                result = argsBuilder.ToString();
            }
            return expressionSupported ? true : (bool?)null;
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

        private static bool CheckForEnumValue(BindingContext context, Type desugared, Statement statement,
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
                var typePrinter = new CSharpTypePrinter(context);
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

        private static string TranslateEnumExpression(Type desugared, string @params)
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
                ((BuiltinTypeExpressionObsolete)defaultArgument).Value == 0;
        }

        private static bool CheckForDefaultChar(BindingContext context, Type desugared, ref string result)
        {
            int value;
            if (int.TryParse(result, out value) &&
                ((context.Options.MarshalCharAsManagedChar &&
                 desugared.IsPrimitiveType(PrimitiveType.Char)) ||
                 desugared.IsPrimitiveType(PrimitiveType.WideChar)))
            {
                result = value == 0 ? "'\\0'" : ("(char) " + result);
                return true;
            }

            return false;
        }
    }
}