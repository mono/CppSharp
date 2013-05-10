using System.IO;

namespace CppSharp.Generators
{
    public enum LanguageGeneratorKind
    {
        CPlusPlusCLI,
        CSharp
    }

    public interface IGenerator
    {
        bool Generate(TranslationUnit unit);
    }

    public abstract class Generator : IGenerator
    {
        public Driver Driver { get; private set; }

        protected Generator(Driver driver)
        {
            Driver = driver;
        }

        public abstract bool Generate(TranslationUnit unit);

        public string GetOutputPath(TranslationUnit unit)
        {
            var file = unit.FileNameWithoutExtension;

            if (Driver.Options.GenerateName != null)
                file = Driver.Options.GenerateName(unit);

            var path = Path.Combine(Driver.Options.OutputDir, file);
            return Path.GetFullPath(path);
        }
    }
}