using System.Collections.Generic;
using Cxxi.Generators;
using Cxxi.Generators.CLI;
using Cxxi.Passes;
using Cxxi.Types;

namespace Cxxi.Libraries
{
    class Qt : ILibrary
    {
        public void Preprocess(Library lib)
        {
            // Qt Base
            lib.IgnoreFile("qalgorithms.h");
            lib.IgnoreFile("qarraydata.h");
            lib.IgnoreFile("qatomic.h");
            lib.IgnoreFile("qatomic_x86.h");
            lib.IgnoreFile("qbytearray.h");
            lib.IgnoreFile("qbasicatomic.h");
            lib.IgnoreFile("qflags.h");
            lib.IgnoreFile("qglobal.h");
            lib.IgnoreFile("qstring.h");
            lib.IgnoreFile("qrefcount.h");
            lib.IgnoreFile("qtypeinfo.h");
            lib.IgnoreFile("qtypetraits.h");

            lib.IgnoreClassWithName("QForeachContainer");
            lib.IgnoreClassWithName("QFlags");
            lib.IgnoreClassWithName("QUrlTwoFlags");

            lib.SetClassAsValueType("QChar");
            lib.SetClassAsValueType("QPair");
            lib.SetClassAsValueType("QPoint");
            lib.SetClassAsValueType("QPointF");
        }

        public void Postprocess(Library lib)
        {
        }

        public void Setup(DriverOptions options)
        {
            options.LibraryName = "Qt";
            options.OutputNamespace = "Qt";
            options.OutputDir = @"C:\Users\triton\Development\cxxi\examples\qt\wrappers";
            options.IncludeDirs.Add(@"C:\Qt\Qt5.0.1\5.0.1\msvc2010\include");
            options.GeneratorKind = LanguageGeneratorKind.CPlusPlusCLI;

            SetupHeaders(options.Headers);
        }

        public void SetupHeaders(List<string> headers)
        {
            var sources = new string[]
                {
                    "QtCore/QPoint",
                    "QtCore/QUrl",
                };

            headers.AddRange(sources);
        }

        public void SetupPasses(PassBuilder p)
        {
            p.RemovePrefix("Q");

            const RenameTargets renameTargets = RenameTargets.Function
                | RenameTargets.Method | RenameTargets.Field;
            p.RenameDeclsCase(renameTargets, RenameCasePattern.UpperCamelCase);

            p.FunctionToInstanceMethod();
            p.FunctionToStaticMethod();
            p.CheckDuplicateNames();
        }

        public void GenerateStart(TextTemplate template)
        {
            if (template is CLISourcesTemplate)
                template.WriteLine("#include \"_Marshal.h\"");
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
            if (template is CLISourcesTemplate)
                template.WriteLine("using namespace clix;");
        }
    }

    static class Program
    {
        public static void Main(string[] args)
        {
            Cxxi.Program.Run(new Libraries.Qt());
        }
    }
}
