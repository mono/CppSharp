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

            if (unit.IsSystemHeader)
            {
                unit.Module = Options.SystemModule;
            }
            else
            {
                var includeDir = Path.GetFullPath(Path.GetDirectoryName(unit.FilePath));
                unit.Module = Options.Modules.FirstOrDefault(
                    m => m.IncludeDirs.Any(i => Path.GetFullPath(i) == includeDir)) ??
                    Options.MainModule;
            }
            unit.Module.Units.Add(unit);
            // Try to get an include path that works from the original include directories paths
            unit.IncludePath = GetIncludePath(unit.FilePath);
            return true;
        }

        public override bool VisitDeclarationContext(DeclarationContext context)
        {
            return false;
        }

        string GetIncludePath(string filePath)
        {
            var includePath = filePath;
            var shortestIncludePath = filePath;

            for (uint i = 0; i < Context.ParserOptions.IncludeDirsCount; ++i)
            {
                var path = Context.ParserOptions.getIncludeDirs(i);

                int idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                if (idx == -1)
                {
                    path = path.Replace('/', '\\');
                    idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                }

                if (idx == -1) continue;

                string inc = filePath.Substring(path.Length);

                if (inc.Length < includePath.Length && inc.Length < shortestIncludePath.Length)
                    shortestIncludePath = inc;
            }

            includePath = Options.IncludePrefix
                + shortestIncludePath.TrimStart(new char[] { '\\', '/' });

            return includePath.Replace('\\', '/');
        }
    }
}