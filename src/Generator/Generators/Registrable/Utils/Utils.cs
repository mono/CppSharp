using CppSharp.AST;

namespace CppSharp.Generators.Registrable
{
    public static class Utils
    {
        public static Declaration FindDescribedTemplate(Declaration declaration)
        {
            foreach (var template in declaration.Namespace.Templates)
            {
                if (template.TemplatedDecl == declaration)
                {
                    return template;
                }
            }
            return null;
        }
    }
}
