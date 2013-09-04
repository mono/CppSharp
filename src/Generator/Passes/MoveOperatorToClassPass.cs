using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MoveOperatorToClassPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            // Ignore methods as they are not relevant for this pass.
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.Ignore || !function.IsOperator)
                return false;

            var param = function.Parameters[0];

            Class @class;
            if (!FunctionToInstanceMethodPass.GetClassParameter(param, out @class))
                return false;

            function.ExplicityIgnored = true;

            // Create a new fake method so it acts as a static method.
            var method = new Method()
            {
                Namespace = @class,
                OriginalNamespace = @class,
                Name = function.Name,
                OriginalName = function.OriginalName,
                Mangled = function.Mangled,
                Access = AccessSpecifier.Public,
                Kind = CXXMethodKind.Operator,
                ReturnType = function.ReturnType,
                Parameters = new List<Parameter>(function.Parameters).Skip(1).ToList(),
                CallingConvention = function.CallingConvention,
                IsVariadic = function.IsVariadic,
                IsInline = function.IsInline,
                OperatorKind = function.OperatorKind,
                SynthKind = FunctionSynthKind.NonMemberOperator
            };

            @class.Methods.Add(method);

            Driver.Diagnostics.Debug("Function converted to operator: {0}::{1}",
                @class.Name, function.Name);

            return true;
        }
    }
}
