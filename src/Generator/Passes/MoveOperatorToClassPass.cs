using System;
using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MoveOperatorToClassPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.Ignore || !function.Name.StartsWith("operator"))
                return false;

            string type = function.Parameters[0].Type.Desugar().ToString();
            if (string.IsNullOrEmpty(type))
                return false;

            var @class = Library.FindCompleteClass(type);
            if (@class == null)
                return false;

            function.ExplicityIgnored = true;

            // Create a new fake method so it acts as a static method.
            var method = new Method()
            {
                Namespace = @class,
                OriginalNamespace = function.Namespace,
                Name = function.Name,
                OriginalName = function.OriginalName,
                Mangled = function.Mangled,
                Access = AccessSpecifier.Public,
                Kind = CXXMethodKind.Operator,
                ReturnType = function.ReturnType,
                Parameters = new List<Parameter>(function.Parameters),
                CallingConvention = function.CallingConvention,
                IsVariadic = function.IsVariadic,
                IsInline = function.IsInline,
                IsStatic = true,
                Conversion = MethodConversionKind.FunctionToStaticMethod,
                OperatorKind = GetOperatorKind(function.Name)
            };

            @class.Methods.Add(method);

            Console.WriteLine("Static method: {0}::{1}", @class.Name,
                function.Name);

            return true;
        }

        private static CXXOperatorKind GetOperatorKind(string @operator)
        {
            const string op = "operator";
            if (!@operator.StartsWith(op))
            {
                return CXXOperatorKind.None;
            }
            switch (@operator.Substring(8).Trim())
            {
                case "+": return CXXOperatorKind.Plus;
                case "-": return CXXOperatorKind.Minus;
                case "!": return CXXOperatorKind.Exclaim;
                case "~": return CXXOperatorKind.Tilde;
                case "++": return CXXOperatorKind.PlusPlus;
                case "--": return CXXOperatorKind.MinusMinus;
                case "*": return CXXOperatorKind.Star;
                case "/": return CXXOperatorKind.Slash;
                case "&": return CXXOperatorKind.Amp;
                case "|": return CXXOperatorKind.Pipe;
                case "^": return CXXOperatorKind.Caret;
                case "<<": return CXXOperatorKind.LessLess;
                case ">>": return CXXOperatorKind.GreaterGreater;
                case "==": return CXXOperatorKind.EqualEqual;
                case "!=": return CXXOperatorKind.ExclaimEqual;
                case "<": return CXXOperatorKind.Less;
                case ">": return CXXOperatorKind.Greater;
                case "<=": return CXXOperatorKind.LessEqual;
                case ">=": return CXXOperatorKind.GreaterEqual;
                case "+=": return CXXOperatorKind.PlusEqual;
                case "-=": return CXXOperatorKind.MinusEqual;
                case "*=": return CXXOperatorKind.StarEqual;
                case "/=": return CXXOperatorKind.SlashEqual;
                case "%=": return CXXOperatorKind.PercentEqual;
                case "&=": return CXXOperatorKind.AmpEqual;
                case "|=": return CXXOperatorKind.PipeEqual;
                case "^=": return CXXOperatorKind.CaretEqual;
                case "<<=": return CXXOperatorKind.LessLessEqual;
                case ">>=": return CXXOperatorKind.GreaterGreaterEqual;
                case "[]": return CXXOperatorKind.Subscript;
                case "&&": return CXXOperatorKind.AmpAmp;
                case "||": return CXXOperatorKind.PipePipe;
                case "=": return CXXOperatorKind.Equal;
                case ",": return CXXOperatorKind.Comma;
                case "->*": return CXXOperatorKind.ArrowStar;
                case "->": return CXXOperatorKind.Arrow;
                case "()": return CXXOperatorKind.Call;
                case "?": return CXXOperatorKind.Conditional;
                case "new": return CXXOperatorKind.New;
                case "delete": return CXXOperatorKind.Delete;
                case "new[]": return CXXOperatorKind.Array_New;
                case "delete[]": return CXXOperatorKind.Array_Delete;
            }
            return CXXOperatorKind.None;
        }
    }
}
