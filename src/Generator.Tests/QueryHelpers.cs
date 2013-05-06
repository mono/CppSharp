using System.Linq;
using CppSharp;

namespace Generator.Tests
{
    public static class LibraryQueryExtensions
    {
        public static Namespace Namespace(this TranslationUnit unit, string name)
        {
            return unit.FindNamespace(name);
        }

        public static Class Class(this Library library, string name)
        {
            return library.FindClass(name).ToList().First();
        }

        public static Function Function(this Library library, string name)
        {
            return library.FindFunction(name).ToList().First();
        }

        public static Enumeration Enum(this Library library, string name)
        {
            return library.FindEnum(name).ToList().First();
        }

        public static TypedefDecl Typedef(this Library library, string name)
        {
            return library.FindTypedef(name).ToList().First();
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
