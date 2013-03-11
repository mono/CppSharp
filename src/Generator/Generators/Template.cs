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
    }
}