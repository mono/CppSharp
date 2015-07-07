using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using CppSharp.Types;

namespace CppSharp.Passes
{
    public class HandleDefaultParamValuesPass : TranslationUnitPass
    {
        private static readonly Regex regexFunctionParams = new Regex(@"\(?(.+)\)?", RegexOptions.Compiled);
        private static readonly Regex regexDoubleColon = new Regex(@"\w+::", RegexOptions.Compiled);
        private static readonly Regex regexName = new Regex(@"(\w+)", RegexOptions.Compiled);
        private static readonly Regex regexCtor = new Regex(@"^([\w<,>:]+)\s*(\([\w, ]*\))$", RegexOptions.Compiled);

        private readonly Dictionary<DeclarationContext, List<Function>> overloads = new Dictionary<DeclarationContext, List<Function>>(); 

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
                Type desugared = parameter.Type.Desugar();

                if (CheckForDefaultPointer(desugared, parameter))
                    continue;

                CheckFloatSyntax(desugared, parameter);

                bool? defaultConstruct = CheckForDefaultConstruct(desugared, parameter.DefaultArgument,
                    parameter.QualifiedType.Qualifiers);
                if (defaultConstruct == null ||
                    (!Driver.Options.MarshalCharAsManagedChar &&
                     parameter.Type.Desugar().IsPrimitiveType(PrimitiveType.UChar)))
                {
                    overloadIndices.Add(function.Parameters.IndexOf(parameter));
                    continue;
                }
                if (defaultConstruct == true)
                    continue;

                if (CheckForBinaryOperator(parameter.DefaultArgument, desugared))
                    continue;

                if (CheckForEnumValue(parameter.DefaultArgument, desugared))
                    continue;

                CheckForDefaultEmptyChar(parameter, desugared);
            }

            GenerateOverloads(function, overloadIndices);

            return true;
        }

        private void CheckFloatSyntax(Type desugared, Parameter parameter)
        {
            var builtin = desugared as BuiltinType;
            if (builtin != null)
            {
                switch (builtin.Type)
                {
                    case PrimitiveType.Float:
                        if (parameter.DefaultArgument.String.EndsWith(".F"))
                            parameter.DefaultArgument.String = parameter.DefaultArgument.String.Replace(".F", ".0F");
                        break;
                    case PrimitiveType.Double:
                        if (parameter.DefaultArgument.String.EndsWith("."))
                            parameter.DefaultArgument.String += '0';
                        break;
                }
            }
        }

        private bool CheckForDefaultPointer(Type desugared, Parameter parameter)
        {
            if (desugared.IsPointer())
            {
                // IntPtr.Zero is not a constant
                if (desugared.IsPointerToPrimitiveType(PrimitiveType.Void))
                {
                    parameter.DefaultArgument.String = "new global::System.IntPtr()";
                    return true;
                }
                Class @class;
                if (desugared.GetFinalPointee().TryGetClass(out @class) && @class.IsValueType)
                {
                    parameter.DefaultArgument.String = string.Format("new {0}()",
                        new CSharpTypePrinter(Driver).VisitClassDecl(@class));
                    return true;
                }
                parameter.DefaultArgument.String = "null";
                return true;
            }
            return false;
        }

        private bool? CheckForDefaultConstruct(Type desugared, Expression arg, TypeQualifiers qualifiers)
        {
            // Unwrapping the underlying type behind a possible pointer/reference
            Type type = desugared.GetFinalPointee() ?? desugared;

            Class decl;
            if (!type.TryGetClass(out decl))
                return false;

            var ctor = arg.Declaration as Method;

            TypeMap typeMap;
            var typePrinterContext = new CSharpTypePrinterContext
            {
                CSharpKind = CSharpTypePrinterContextKind.DefaultExpression,
                Type = type
            };

            string typePrinterResult = null;
            if (Driver.TypeDatabase.FindTypeMap(decl, type, out typeMap))
            {
                var typeInSignature = typeMap.CSharpSignatureType(typePrinterContext).SkipPointerRefs();
                Enumeration @enum;
                if (typeInSignature.TryGetEnum(out @enum))
                    return false;

                if (ctor == null || !ctor.IsConstructor)
                    return false;

                typePrinterResult = typeMap.CSharpSignature(typePrinterContext);
                if (typePrinterResult == "string" && ctor.Parameters.Count == 0)
                {
                    arg.String = "\"\"";
                    return true;
                }
            }

            var match = regexCtor.Match(arg.String);
            if (match.Success)
            {
                if (ctor != null)
                {
                    var templateSpecializationType = type as TemplateSpecializationType;
                    var typePrinter = new CSharpTypePrinter(Driver);
                    typePrinterResult = typePrinterResult ?? (templateSpecializationType != null
                        ? typePrinter.VisitTemplateSpecializationType(templateSpecializationType, qualifiers)
                        : typePrinter.VisitClassDecl((Class) ctor.Namespace)).Type;

                    arg.String = string.Format("new {0}{1}", typePrinterResult, match.Groups[2].Value);
                    if (ctor.Parameters.Count > 0 && ctor.Parameters[0].Type.IsAddress())
                        arg.String = arg.String.Replace("(0)", "()");
                }
                else
                    arg.String = string.Format("new {0}", arg.String);
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

        private bool CheckForBinaryOperator(Expression arg, Type desugared)
        {
            if (arg.Class != StatementClass.BinaryOperator) return false;

            var binaryOperator = (BinaryOperator) arg;
            CheckForEnumValue(binaryOperator.LHS, desugared);
            CheckForEnumValue(binaryOperator.RHS, desugared);
            arg.String = string.Format("{0} {1} {2}", binaryOperator.LHS.String,
                binaryOperator.OpcodeStr, binaryOperator.RHS.String);
            return true;
        }

        private bool CheckForEnumValue(Expression arg, Type desugared)
        {
            var enumItem = arg.Declaration as Enumeration.Item;
            if (enumItem != null)
            {
                arg.String = string.Format("{0}{1}.{2}",
                    desugared.IsPrimitiveType() ? "(int) " : string.Empty,
                    new CSharpTypePrinter(Driver).VisitEnumDecl((Enumeration) enumItem.Namespace), enumItem.Name);
                return true;
            }

            var call = arg.Declaration as Function;
            if (call != null && arg.String != "0")
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

        private void GenerateOverloads(Function function, List<int> overloadIndices)
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
