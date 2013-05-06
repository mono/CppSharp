using System.Collections.Generic;
using System.Linq;
using CppSharp.Generators;
using CppSharp.Types;

namespace CppSharp.Passes
{
    struct OverloadSignature
    {
        public string Return;
        public List<string> Parameters;
        public Function Function;

        public OverloadSignature(Function function)
        {
            Function = function;

            Return = function.ReturnType.ToString();
            Parameters = new List<string>();

            foreach (var param in function.Parameters)
            {
                var paramType = param.Type.ToString();
                Parameters.Add(paramType);
            }
        }

        public static bool IsAmbiguous(OverloadSignature overload1,
            OverloadSignature overload2, Class @class)
        {
            if (overload1.Function == overload2.Function)
                return false;

            if (TextTemplate.CheckIgnoreFunction(@class, overload1.Function))
                return false;

            if (TextTemplate.CheckIgnoreFunction(@class, overload2.Function))
                return false;

            // TODO: Default parameters?
            if (overload1.Parameters.Count != overload2.Parameters.Count)
                return false;

            if (overload1.Parameters.Count == 0)
                return true;

            for (var i = 0; i < overload1.Parameters.Count; i++)
            {
                if (overload1.Parameters[i] != overload2.Parameters[i])
                    return false;
            }

            return true;
        }
    };

    public class CheckAmbiguousOverloads : TranslationUnitPass
    {
        private Class currentClass;
        private readonly ISet<Function> visited;

        public CheckAmbiguousOverloads()
        {
            visited = new HashSet<Function>();
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitNamespaceTypedefs = false;

            Options.VisitClassBases = false;
            Options.VisitClassFields = false;
            Options.VisitClassProperties = false;
            Options.VisitClassEvents = false;
            Options.VisitClassVariables = false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            visited.Clear();

            currentClass = @class;
            return base.VisitClassDecl(@class);
        }

        public override bool VisitMethodDecl(Method method)
        {
            CheckOverloads(method);
            return false;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            CheckOverloads(function);
            return false;
        }

        private bool CheckOverloads(Function function)
        {
            if (visited.Contains(function))
                return false;

            var overloads = AST.Utils.GetFunctionOverloads(function, currentClass);
            var signatures = overloads.Select(fn => new OverloadSignature(fn)).ToList();

            foreach (var sig1 in signatures)
            {
                visited.Add(sig1.Function);

                foreach (var sig2 in signatures)
                {
                    if (!OverloadSignature.IsAmbiguous(sig1, sig2, currentClass))
                        continue;

                    Driver.Diagnostics.EmitWarning(DiagnosticId.AmbiguousOverload,
                        "Overload {0}::{1} is ambiguous, renaming automatically",
                        currentClass.OriginalName, sig1.Function.QualifiedOriginalName);

                    RenameOverload(sig1.Function);
                    return false;
                }
            }

            return true;
        }

        public void RenameOverload(Function function)
        {
            function.Name = function.Name + "0";
        }
    }

    public static class CheckAmbiguousOverloadsExtensions
    {
        public static void CheckAmbiguousOverloads(this PassBuilder builder)
        {
            var pass = new CheckAmbiguousOverloads();
            builder.AddPass(pass);
        }
    }
}
