using System.Collections.Generic;
using System.Text;

namespace CppSharp.Generators
{
    public abstract class TextTemplate : TextGenerator
    {
        public Driver Driver { get; private set; }
        public DriverOptions Options { get; private set; }
        public Library Library { get; private set; }
        public TranslationUnit TranslationUnit { get; private set; }

        protected TextTemplate(Driver driver, TranslationUnit unit)
        {
            Driver = driver;
            Options = driver.Options;
            Library = driver.Library;
            TranslationUnit = unit;
        }

        public abstract string FileExtension { get; }

        public abstract void Generate();

        public virtual string GenerateText()
        {
            return base.ToString();
        }
    }

}