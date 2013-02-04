using System.Linq;
using Cxxi;

namespace Generator.Tests
{
    public static class LibraryQueryExtensions
    {
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

        public static Parameter Param(this Function function, int index)
        {
            return function.Parameters[index];
        }
    }
}
