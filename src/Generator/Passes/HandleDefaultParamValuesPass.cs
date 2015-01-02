using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Passes
{
    public class HandleDefaultParamValuesPass : TranslationUnitPass
    {
        private static readonly Regex regexFunctionParams = new Regex(@"\(?(.+)\)?", RegexOptions.Compiled);
        private static readonly Regex regexDoubleColon = new Regex(@"\w+::", RegexOptions.Compiled);
        private static readonly Regex regexName = new Regex(@"(\w+)", RegexOptions.Compiled);
        private static readonly Regex regexCtor = new Regex(@"^(\w+)\s*\(\w*\)$", RegexOptions.Compiled);

        public override bool VisitFunctionDecl(Function function)
        {
            bool result = base.VisitFunctionDecl(function);

            var overloadIndices = new List<int>(function.Parameters.Count);
            foreach (var parameter in function.Parameters.Where(p => p.DefaultArgument != null))
            {
                Type desugared = parameter.Type.Desugar();

                if (CheckForDefaultPointer(desugared, parameter))
                    continue;

                CheckFloatSyntax(desugared, parameter);

                bool? defaultConstruct = CheckForDefaultConstruct(desugared, parameter.DefaultArgument);
                if (defaultConstruct == null ||
                    (!Driver.Options.MarshalCharAsManagedChar &&
                     parameter.Type.Desugar().IsPrimitiveType(PrimitiveType.UChar)))
                {
                    overloadIndices.Add(function.Parameters.IndexOf(parameter));
                    continue;
                }
                if (defaultConstruct == true)
                    continue;

                if (CheckForEnumValue(parameter.DefaultArgument, desugared))
                    continue;

                CheckForDefaultEmptyChar(parameter, desugared);
            }

            GenerateOverloads(function, overloadIndices);

            return result;
        }

        private void CheckFloatSyntax(Type desugared, Parameter parameter)
        {
            var builtin = desugared as BuiltinType;
            if (builtin != null && builtin.Type == AST.PrimitiveType.Float && parameter.DefaultArgument.String.EndsWith(".F"))
            {
                parameter.DefaultArgument.String = parameter.DefaultArgument.String.Replace(".F", ".0F");
            }
        }

        private static bool CheckForDefaultPointer(Type desugared, Parameter parameter)
        {
            if (desugared.IsPointer())
            {
                // IntPtr.Zero is not a constant
                parameter.DefaultArgument.String = desugared.IsPointerToPrimitiveType(PrimitiveType.Void) ?
                    "new global::System.IntPtr()" : "null";
                return true;
            }
            return false;
        }

        private bool? CheckForDefaultConstruct(Type desugared, Expression arg)
        {
            // Unwrapping the underlying type behind a possible pointer/reference
            Type type;
            desugared.IsPointerTo(out type);
            type = type ?? desugared;

            Class decl;
            if (!type.TryGetClass(out decl))
                return false;

            var ctor = arg.Declaration as Method;

            TypeMap typeMap;
            if (Driver.TypeDatabase.FindTypeMap(decl, type, out typeMap))
            {
                Type typeInSignature;
                string mappedTo;
                if (Driver.Options.IsCSharpGenerator)
                {
                    var typePrinterContext = new CSharpTypePrinterContext
                    {
                        CSharpKind = CSharpTypePrinterContextKind.Managed,
                        Type = type
                    };
                    typeInSignature = typeMap.CSharpSignatureType(typePrinterContext).SkipPointerRefs();
                    mappedTo = typeMap.CSharpSignature(typePrinterContext);
                }
                else
                {
                    var typePrinterContext = new CLITypePrinterContext
                    {
                        Type = type
                    };
                    typeInSignature = typeMap.CLISignatureType(typePrinterContext).SkipPointerRefs();
                    mappedTo = typeMap.CLISignature(typePrinterContext);
                }
                Enumeration @enum;
                if (typeInSignature.TryGetEnum(out @enum))
                {
                    return false;
                }

                if (ctor == null || !ctor.IsConstructor)
                    return false;
                if (mappedTo == "string" && ctor.Parameters.Count == 0)
                {
                    arg.String = "\"\"";
                    return true;
                }
            }

            if (regexCtor.IsMatch(arg.String))
            {
                arg.String = string.Format("new {0}", arg.String);
                if (ctor != null && ctor.Parameters.Count > 0 && ctor.Parameters[0].Type.IsAddress())
                {
                    arg.String = arg.String.Replace("(0)", "()");
                    return decl.IsValueType ? true : (bool?) null;
                }
            }
            else
            {
                if (ctor != null && ctor.Parameters.Count > 0)
                {
                    var finalPointee = ctor.Parameters[0].Type.SkipPointerRefs().Desugar();
                    Enumeration @enum;
                    if (finalPointee.TryGetEnum(out @enum))
                        TranslateEnumExpression(arg, finalPointee, arg.String);
                }
            }

            return decl.IsValueType ? true : (bool?) null;
        }

        private static bool CheckForEnumValue(Expression arg, Type desugared)
        {
            var enumItem = arg.Declaration as Enumeration.Item;
            if (enumItem != null)
            {
                arg.String = string.Format("{0}{1}{2}.{3}",
                    desugared.IsPrimitiveType() ? "(int) " : string.Empty,
                    string.IsNullOrEmpty(enumItem.Namespace.Namespace.Name)
                        ? string.Empty
                        : enumItem.Namespace.Namespace.Name + ".", enumItem.Namespace.Name, enumItem.Name);
                return true;
            }

            var call = arg.Declaration as Function;
            if ((call != null || arg.Class == StatementClass.BinaryOperator) && arg.String != "0")
            {
                string @params = regexFunctionParams.Match(arg.String).Groups[1].Value;
                TranslateEnumExpression(arg, desugared, @params);
                return true;
            }
            return false;
        }

        private static void TranslateEnumExpression(Expression arg, Type desugared, string @params)
        {
            if (@params.Contains("::"))
                arg.String = regexDoubleColon.Replace(@params, desugared + ".");
            else
                arg.String = regexName.Replace(@params, desugared + ".$1");
        }

        private void CheckForDefaultEmptyChar(Parameter parameter, Type desugared)
        {
            if (parameter.DefaultArgument.String == "0" && Driver.Options.MarshalCharAsManagedChar &&
                desugared.IsPrimitiveType(PrimitiveType.Char))
            {
                parameter.DefaultArgument.String = "'\\0'";
            }
        }

        private static void GenerateOverloads(Function function, List<int> overloadIndices)
        {
            foreach (var overloadIndex in overloadIndices)
            {
                var method = function as Method;
                Function overload = method != null ? new Method(method) : new Function(function);
                overload.OriginalFunction = function;
                overload.SynthKind = FunctionSynthKind.DefaultValueOverload;
                overload.Parameters[overloadIndex].GenerationKind = GenerationKind.None;

                var indices = overloadIndices.Where(i => i != overloadIndex).ToList();
                if (indices.Any())
                    for (int i = 0; i <= indices.Last(); i++)
                        if (i != overloadIndex)
                            overload.Parameters[i].DefaultArgument = null;

                if (method != null)
                    ((Class) function.Namespace).Methods.Add((Method) overload);
                else
                    function.Namespace.Functions.Add(overload);

                for (int i = 0; i <= overloadIndex; i++)
                    function.Parameters[i].DefaultArgument = null;
            }
        }
    }
}
