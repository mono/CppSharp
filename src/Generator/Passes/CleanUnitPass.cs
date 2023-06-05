using System.IO;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CleanUnitPass : TranslationUnitPass
    {
        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!base.VisitTranslationUnit(unit))
                return false;

            unit.Module = GetModule(unit);
            unit.Module.Units.Add(unit);

            // Try to get an include path that works from the original include
            // directories paths
            unit.IncludePath = GetIncludePath(unit.FilePath);

            return true;
        }

        private Module GetModule(TranslationUnit unit)
        {
            if (unit.IsSystemHeader)
                return Options.SystemModule;

            var includeDir = Path.GetDirectoryName(unit.FilePath);
            if (string.IsNullOrWhiteSpace(includeDir))
                includeDir = ".";
            includeDir = Path.GetFullPath(includeDir);

            return Options.Modules.FirstOrDefault(
                       m => m.IncludeDirs.Any(i => Path.GetFullPath(i) == includeDir)) ??
                   Options.Modules[1];
        }

        public override bool VisitDeclarationContext(DeclarationContext context)
        {
            return false;
        }

        private string GetIncludePath(string filePath)
        {
            var includePath = filePath;

            for (uint i = 0; i < Context.ParserOptions.IncludeDirsCount; ++i)
            {
                var path = Context.ParserOptions.GetIncludeDirs(i);

                int idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                if (idx == -1)
                {
                    path = path.Replace('/', '\\');
                    idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                }

                if (idx != -1)
                {
                    includePath = filePath[path.Length..];
                    break;
                }
            }

            includePath = Options.IncludePrefix
                + includePath.TrimStart(new char[] { '\\', '/' });

            return includePath.Replace('\\', '/');
        }
    }
}