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
            if (IsExternalDeclaration(unit) && unit.IsGenerated)
                unit.GenerationKind = GenerationKind.Link;
                
            // Try to get an include path that works from the original include
            // directories paths.
            if (unit.IsValid)
            {
                unit.IncludePath = GetIncludePath(unit.FilePath);
                return true;
            }
            return false;
        }

        string GetIncludePath(string filePath)
        {
            var includePath = filePath;
            var shortestIncludePath = filePath;

            for (uint i = 0; i < DriverOptions.IncludeDirsCount; ++i)
            {
                var path = DriverOptions.getIncludeDirs(i);

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

            includePath = DriverOptions.IncludePrefix
                + shortestIncludePath.TrimStart(new char[] { '\\', '/' });

            return includePath.Replace('\\', '/');
        }


        bool IsExternalDeclaration(TranslationUnit translationUnit)
        {
            if (DriverOptions.NoGenIncludeDirs == null)
                return false;

            foreach (var path in DriverOptions.NoGenIncludeDirs)
            {
                if (translationUnit.FilePath.StartsWith(path))
                    return true;
            }

            return false;
        }
    }
}