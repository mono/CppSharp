namespace Cxxi.Generators
{
    public abstract class TextTemplate : TextGenerator
    {
        private const uint DefaultIndent = 4;
        private const uint MaxIndent = 80;

        public Driver Driver { get; set; }
        public DriverOptions Options { get; set; }
        public Library Library { get; set; }
        public ILibrary Transform;
        public TranslationUnit TranslationUnit { get; set; }
        public abstract string FileExtension { get; }

        public abstract void Generate();

        protected TextTemplate(Driver driver, TranslationUnit unit)
        {
            Driver = driver;
            Options = driver.Options;
            Library = driver.Library;
            Transform = driver.Transform;
            TranslationUnit = unit;
        }

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

            bool isEmptyCtor = method.IsConstructor && method.Parameters.Count == 0;

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