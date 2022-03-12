using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Internal;

namespace CppSharp.Passes
{
    public class HandleDefaultParamValuesPass : TranslationUnitPass
    {
        private readonly Dictionary<DeclarationContext, List<Function>> overloads =
            new Dictionary<DeclarationContext, List<Function>>();

        public HandleDefaultParamValuesPass()
            => VisitOptions.ResetFlags(VisitFlags.ClassMethods | VisitFlags.ClassTemplateSpecializations);

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
                if (type is TemplateParameterSubstitutionType || type.IsDependent)
                {
                    parameter.DefaultArgument = null;
                    continue;
                }

                var result = parameter.DefaultArgument.String;

                if (ExpressionHelper.PrintExpression(Context, function, parameter.Type,
                        parameter.OriginalDefaultArgument, allowDefaultLiteral: true, ref result) == null)
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
                    ((Class)function.Namespace).Methods.Add((Method)overload);
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
