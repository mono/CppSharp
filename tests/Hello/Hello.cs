using Cxxi.Generators;
using Cxxi.Passes;

namespace Cxxi.Tests
{
    class Hello : ILibrary
    {
        private readonly LanguageGeneratorKind kind;

        public Hello(LanguageGeneratorKind kind)
        {
            this.kind = kind;
        }

        public void Setup(DriverOptions options)
        {
            options.LibraryName = "Hello";
            options.GeneratorKind = kind;
            options.OutputDir = kind == LanguageGeneratorKind.CPlusPlusCLI ?
                "cli" : "cs";
            options.Headers.Add("Hello.h");
            options.IncludeDirs.Add("../../../examples/Hello");
        }

        public void Preprocess(Library lib)
        {
        }

        public void Postprocess(Library lib)
        {
        }

        public void SetupPasses(Driver driver, PassBuilder p)
        {
        }

        public void GenerateStart(TextTemplate template)
        {
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
        }

        static class Program
        {
            public static void Main(string[] args)
            {
                Driver.Run(new Hello(LanguageGeneratorKind.CPlusPlusCLI));
                Driver.Run(new Hello(LanguageGeneratorKind.CSharp));
            }
        }
    }
}
