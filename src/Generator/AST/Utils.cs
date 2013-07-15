
namespace CppSharp.AST
{
    public static class ASTUtils
    {
        public static bool CheckIgnoreFunction(Class @class, Function function)
        {
            if (function.Ignore) return true;

            if (function is Method)
                return CheckIgnoreMethod(@class, function as Method);

            return false;
        }

        public static bool CheckIgnoreMethod(Class @class, Method method)
        {
            if (method.Ignore) return true;

            var isEmptyCtor = method.IsConstructor && method.Parameters.Count == 0;

            if (@class.IsValueType && isEmptyCtor)
                return true;

            if (method.IsCopyConstructor || method.IsMoveConstructor)
                return true;

            if (method.IsDestructor)
                return true;

            if (method.OperatorKind == CXXOperatorKind.Equal)
                return true;

            if (method.Kind == CXXMethodKind.Conversion)
                return true;

            if (method.Access != AccessSpecifier.Public)
                return true;

            return false;
        }

        public static bool CheckIgnoreField(Class @class, Field field)
        {
            if (field.Ignore) return true;

            if (field.Access != AccessSpecifier.Public)
                return true;

            return false;
        }
    }
}
