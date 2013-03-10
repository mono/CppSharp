namespace Cxxi.Passes
{
    public class CleanUnitPass : TranslationUnitPass
    {
        public DriverOptions Options;
        public PassBuilder Passes;

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

            foreach (var path in Options.IncludeDirs)
            {
                int idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                if (idx == -1) continue;

                string inc = filePath.Substring(path.Length);

                if (inc.Length < includePath.Length && inc.Length < shortestIncludePath.Length)
                    shortestIncludePath = inc;
            }

            return Options.IncludePrefix + shortestIncludePath.TrimStart(new char[] { '\\', '/' });
        }
    }

    public static class CleanUnitPassExtensions
    {
        public static void CleanUnit(this PassBuilder builder)
        {
            var pass = new CleanUnitPass();
            builder.AddPass(pass);
        }
    }
}