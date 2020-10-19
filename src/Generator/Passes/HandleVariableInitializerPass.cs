using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Internal;

namespace CppSharp.Passes
{
    public class HandleVariableInitializerPass : TranslationUnitPass
    {
        public override bool VisitVariableDecl(Variable variable)
        {
            if (!base.VisitVariableDecl(variable) || variable.Ignore || variable.Initializer == null)
                return false;

            bool supported =
                variable.Type.IsPrimitiveType() ||
                variable.Type.IsPointerToPrimitiveType(PrimitiveType.Char) ||
                variable.Type.IsPointerToPrimitiveType(PrimitiveType.WideChar) ||
                (variable.Type is ArrayType arrayType && (
                    arrayType.Type.IsPrimitiveType() || 
                    arrayType.Type.IsPointerToPrimitiveType(PrimitiveType.Char) ||
                    arrayType.Type.IsPointerToPrimitiveType(PrimitiveType.WideChar)));

            if (!supported)
            {
                variable.Initializer = null;
                return false;
            }

            string initializerString = variable.Initializer.String;
            ExpressionHelper.PrintExpression(Context, null, variable.Type, variable.Initializer, allowDefaultLiteral: true, ref initializerString);

            if (string.IsNullOrWhiteSpace(initializerString))
                variable.Initializer = null;
            else
                variable.Initializer.String = initializerString;

            return true;
        }
    }
}
