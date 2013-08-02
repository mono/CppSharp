using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CleanUnitPass : TranslationUnitPass
    {
        public DriverOptions DriverOptions;

        public CleanUnitPass(DriverOptions options)
        {
            DriverOptions = options;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            // Try to get an include path that works from the original include
            // directories paths.

            unit.IncludePath = GetIncludePath(unit.FilePath);
            return true;
        }

        string GetIncludePath(string filePath)
        {
            var includePath = filePath;
            var shortestIncludePath = filePath;

            foreach (var path in DriverOptions.IncludeDirs)
            {
                int idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                if (idx == -1) continue;

                string inc = filePath.Substring(path.Length);

                if (inc.Length < includePath.Length && inc.Length < shortestIncludePath.Length)
                    shortestIncludePath = inc;
            }

            includePath = DriverOptions.IncludePrefix
                + shortestIncludePath.TrimStart(new char[] { '\\', '/' });

            return includePath.Replace('\\', '/');
        }
    }
}