using System.Linq;
using CppSharp.AST;

namespace CppSharp.Generator.Tests
{
    public static class LibraryQueryExtensions
    {
        public static Namespace Namespace(this TranslationUnit unit, string name)
        {
            return unit.FindNamespace(name);
        }

        public static Class Class(this ASTContext context, string name)
        {
            return context.FindClass(name).First();
        }

        public static Function Function(this ASTContext context, string name)
        {
            return context.FindFunction(name).First();
        }

        public static Enumeration Enum(this ASTContext context, string name)
        {
            return context.FindEnum(name).First();
        }

        public static TypedefDecl Typedef(this ASTContext context, string name)
        {
            return context.FindTypedef(name).First();
        }

        public static Field Field(this Class @class, string name)
        {
            return @class.Fields.Find(field => field.Name == name);
        }

        public static Method Method(this Class @class, string name)
        {
            return @class.Methods.Find(method => method.Name == name);
        }

        public static Parameter Param(this Function function, int index)
        {
            return function.Parameters[index];
        }
    }
}
