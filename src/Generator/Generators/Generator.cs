using Cxxi.Types;

namespace Cxxi.Generators
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
    }
}