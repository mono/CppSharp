using System.Collections.Generic;
using System.Globalization;
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
        private static readonly Regex regexFunctionParams = new Regex(@"\((.+)\)", RegexOptions.Compiled);
        private static readonly Regex regexDoubleColon = new Regex(@"\w+::", RegexOptions.Compiled);
        private static readonly Regex regexName = new Regex(@"(\w+)", RegexOptions.Compiled);

        public override bool VisitFunctionDecl(Function function)
        {
            bool result = base.VisitFunctionDecl(function);

            var overloadIndices = new List<int>(function.Parameters.Count);
            foreach (var parameter in function.Parameters.Where(p => p.DefaultArgument != null))
            {
                Type desugared = parameter.Type.Desugar();

                if (CheckForDefaultPointer(desugared, parameter))
                    continue;

                bool? defaultConstruct = CheckForDefaultConstruct(desugared, parameter);
                if (defaultConstruct == null ||
                    (!Driver.Options.MarshalCharAsManagedChar &&
                     parameter.Type.Desugar().IsPrimitiveType(PrimitiveType.UChar)))
                {
                    overloadIndices.Add(function.Parameters.IndexOf(parameter));
                    continue;
                }
                if (defaultConstruct == true)
                    continue;

                if (CheckForEnumValue(parameter, desugared))
                    continue;

                CheckForULongValue(parameter, desugared);
            }

            GenerateOverloads(function, overloadIndices);

            return result;
        }

        private static bool CheckForDefaultPointer(Type desugared, Parameter parameter)
        {
            if (desugared.IsPointer())
            {
                parameter.DefaultArgument.String = "null";
                return true;
            }
            return false;
        }

        private bool? CheckForDefaultConstruct(Type desugared, Parameter parameter)
        {
            Method ctor = parameter.DefaultArgument.Declaration as Method;
            if (ctor == null || !ctor.IsConstructor)
                return false;

            Type type;
            desugared.IsPointerTo(out type);
            type = type ?? desugared;
            Class decl;
            if (!type.TryGetClass(out decl))
                return false;
            TypeMap typeMap;

            if (Driver.TypeDatabase.FindTypeMap(decl, type, out typeMap))
            {
                string mappedTo;
                if (Driver.Options.IsCSharpGenerator)
                {
                    var typePrinterContext = new CSharpTypePrinterContext
                    {
                        CSharpKind = CSharpTypePrinterContextKind.Managed,
                        Type = type
                    };
                    mappedTo = typeMap.CSharpSignature(typePrinterContext);
                }
                else
                {
                    var typePrinterContext = new CLITypePrinterContext
                    {
                        Type = type
                    };
                    mappedTo = typeMap.CLISignature(typePrinterContext);
                }
                if (mappedTo == "string" && ctor.Parameters.Count == 0)
                {
                    parameter.DefaultArgument.String = "\"\"";
                    return true;
                }
            }

            parameter.DefaultArgument.String = string.Format("new {0}", parameter.DefaultArgument.String);
            if (ctor.Parameters.Count > 0 && ctor.Parameters[0].OriginalName == "_0")
                parameter.DefaultArgument.String = parameter.DefaultArgument.String.Replace("(0)", "()");

            return decl.IsValueType ? true : (bool?) null;
        }

        private static bool CheckForEnumValue(Parameter parameter, Type desugared)
        {
            var enumItem = parameter.DefaultArgument.Declaration as Enumeration.Item;
            if (enumItem != null)
            {
                parameter.DefaultArgument.String = string.Format("{0}{1}.{2}.{3}",
                    desugared.IsPrimitiveType() ? "(int) " : string.Empty,
                    enumItem.Namespace.Namespace.Name, enumItem.Namespace.Name, enumItem.Name);
                return true;
            }

            var call = parameter.DefaultArgument.Declaration as Method;
            if (call != null && call.IsConstructor)
            {
                string @params =
                    regexFunctionParams.Match(parameter.DefaultArgument.String).Groups[1].Value;
                if (@params.Contains("::"))
                    parameter.DefaultArgument.String = regexDoubleColon.Replace(@params, desugared + ".");
                else
                    parameter.DefaultArgument.String = regexName.Replace(@params, desugared + ".$1");
                return true;
            }
            return false;
        }

        private static void CheckForULongValue(Parameter parameter, Type desugared)
        {
            ulong value;
            string @default = parameter.DefaultArgument.String;
            // HACK: .NET's Parse/TryParse have a bug preventing them from parsing UL-suffixed ulongs
            if (desugared.IsPrimitiveType() && @default.EndsWith("UL"))
                @default = @default.Substring(0, @default.Length - 2);
            if (ulong.TryParse(@default, out value))
                parameter.DefaultArgument.String = value.ToString(CultureInfo.InvariantCulture);
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
