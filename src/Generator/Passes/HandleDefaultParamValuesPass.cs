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
            Options.VisitFunctionParameters = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsGenerated)
                return false;
            var result = base.VisitTranslationUnit(unit);
            foreach (var overload in overloads)
                overload.Key.Functions.AddRange(overload.Value);
            overloads.Clear();
            return result;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            var overloadIndices = new List<int>(function.Parameters.Count);
            foreach (var parameter in function.Parameters.Where(p => p.DefaultArgument != null))
            {
                var result = parameter.DefaultArgument.String;
                if (PrintExpression(parameter.Type, parameter.DefaultArgument, ref result) == null)
                    overloadIndices.Add(function.Parameters.IndexOf(parameter));
                parameter.DefaultArgument.String = result;
            }

            GenerateOverloads(function, overloadIndices);

            return true;
        }

        private bool? PrintExpression(Type type, Expression expression, ref string result)
        {
            var desugared = type.Desugar();

            if ((!Driver.Options.MarshalCharAsManagedChar &&
                 desugared.IsPrimitiveType(PrimitiveType.UChar)) ||
                type.IsPrimitiveTypeConvertibleToRef())
                return null;

            if (CheckForDefaultPointer(desugared, ref result))
                return true;

            var defaultConstruct = CheckForDefaultConstruct(desugared, expression, ref result);
            if (defaultConstruct != false)
                return defaultConstruct;

            return CheckFloatSyntax(desugared, expression, ref result) ||
                CheckForBinaryOperator(desugared, expression, ref result) ||
                CheckForEnumValue(desugared, expression, ref result) ||
                CheckForDefaultEmptyChar(desugared, expression, ref result);
        }

        private bool CheckForDefaultPointer(Type desugared, ref string result)
        {
            if (!desugared.IsPointer())
                return false;

            // IntPtr.Zero is not a constant
            if (desugared.IsPointerToPrimitiveType(PrimitiveType.Void))
            {
                result = "new global::System.IntPtr()";
                return true;
            }

            if (desugared.IsPrimitiveTypeConvertibleToRef())
                return false;

            Class @class;
            if (desugared.GetFinalPointee().TryGetClass(out @class) && @class.IsValueType)
            {
                result = string.Format("new {0}()",
                    new CSharpTypePrinter(Driver).VisitClassDecl(@class));
                return true;
            }

            result = "null";
            return true;
        }

        private bool? CheckForDefaultConstruct(Type desugared, Statement statement,
            ref string result)
        {
            var type = desugared.GetFinalPointee() ?? desugared;

            Class decl;
            if (!type.TryGetClass(out decl))
                return false;

            var ctor = statement as CXXConstructExpr;

            TypeMap typeMap;

            var typePrinter = new CSharpTypePrinter(Driver);
            typePrinter.PushMarshalKind(CSharpMarshalKind.DefaultExpression);
            var typePrinterResult = type.Visit(typePrinter).Type;
            if (Driver.TypeDatabase.FindTypeMap(decl, type, out typeMap))
            {
                var typeInSignature = typeMap.CSharpSignatureType(
                    typePrinter.Context).SkipPointerRefs().Desugar();
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
                return decl.IsValueType ? (bool?) false : null;

            var method = (Method) statement.Declaration;
            var expressionSupported = decl.IsValueType && method.Parameters.Count == 0;

            if (statement.String.Contains('('))
            {
                var argsBuilder = new StringBuilder("new ");
                argsBuilder.Append(typePrinterResult);
                argsBuilder.Append('(');
                for (var i = 0; i < ctor.Arguments.Count; i++)
                {
                    var argument = ctor.Arguments[i];
                    var argResult = argument.String;
                    expressionSupported &= PrintExpression(method.Parameters[i].Type.Desugar(),
                        argument, ref argResult) ?? false;
                    argsBuilder.Append(argResult);
                    if (i < ctor.Arguments.Count - 1)
                        argsBuilder.Append(", ");
                }
                argsBuilder.Append(')');
                result = argsBuilder.ToString();
            }
            else
            {
                if (method.Parameters.Count > 0)
                {
                    var paramType = method.Parameters[0].Type.SkipPointerRefs().Desugar();
                    Enumeration @enum;
                    if (paramType.TryGetEnum(out @enum))
                        result = TranslateEnumExpression(method, paramType, statement.String);
                }
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
                        if (statement.String.EndsWith(".F"))
                        {
                            result = statement.String.Replace(".F", ".0F");
                            return true;
                        }
                        break;
                    case PrimitiveType.Double:
                        if (statement.String.EndsWith("."))
                        {
                            result = statement.String + '0';
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        private bool CheckForBinaryOperator(Type desugared, Expression expression,
            ref string result)
        {
            if (expression.Class != StatementClass.BinaryOperator)
                return false;

            var binaryOperator = (BinaryOperator) expression;

            var lhsResult = binaryOperator.LHS.String;
            CheckForEnumValue(desugared, binaryOperator.LHS, ref lhsResult);

            var rhsResult = binaryOperator.RHS.String;
            CheckForEnumValue(desugared, binaryOperator.RHS, ref rhsResult);

            result = string.Format("{0} {1} {2}", lhsResult,
                binaryOperator.OpcodeStr, rhsResult);
            return true;
        }

        private bool CheckForEnumValue(Type desugared, Statement statement,
            ref string result)
        {
            var enumItem = statement.Declaration as Enumeration.Item;
            if (enumItem != null)
            {
                result = string.Format("{0}{1}.{2}",
                    desugared.IsPrimitiveType() ? "(int) " : string.Empty,
                    new CSharpTypePrinter(Driver).VisitEnumDecl(
                        (Enumeration) enumItem.Namespace), enumItem.Name);
                return true;
            }

            var call = statement.Declaration as Function;
            if (call != null && statement.String != "0")
            {
                var @params = regexFunctionParams.Match(statement.String).Groups[1].Value;
                result = TranslateEnumExpression(call, desugared, @params);
                return true;
            }

            return false;
        }

        private string TranslateEnumExpression(Function function,
            Type desugared, string @params)
        {
            TypeMap typeMap;
            if ((function.Parameters.Count == 0 ||
                 HasSingleZeroArgExpression(function)) &&
                Driver.TypeDatabase.FindTypeMap(desugared, out typeMap))
            {
                var typeInSignature = typeMap.CSharpSignatureType(new CSharpTypePrinterContext
                {
                    MarshalKind = CSharpMarshalKind.DefaultExpression,
                    Type = desugared
                }).SkipPointerRefs().Desugar();
                Enumeration @enum;
                if (typeInSignature.TryGetEnum(out @enum))
                    return "0";
            }

            if (@params.Contains("::"))
                return regexDoubleColon.Replace(@params, desugared + ".");

            return regexName.Replace(@params, desugared + ".$1");
        }

        private static bool HasSingleZeroArgExpression(Function function)
        {
            if (function.Parameters.Count != 1)
                return false;

            var defaultArgument = function.Parameters[0].DefaultArgument;
            return defaultArgument is BuiltinTypeExpression &&
                ((BuiltinTypeExpression) defaultArgument).Value == 0;
        }

        private bool CheckForDefaultEmptyChar(Type desugared, Statement statement,
            ref string result)
        {
            if (statement.String == "0" &&
                Driver.Options.MarshalCharAsManagedChar &&
                desugared.IsPrimitiveType(PrimitiveType.Char))
            {
                result = "'\\0'";
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
