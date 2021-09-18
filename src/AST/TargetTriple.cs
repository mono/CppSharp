using System.Linq;

namespace CppSharp.AST
{
    // over time we should turn this into a real class
    // like http://llvm.org/docs/doxygen/html/classllvm_1_1Triple.html
    public static class TargetTriple
    {
        public static bool IsWindows(this string targetTriple)
        {
            var parts = targetTriple.Split('-');
            return parts.Contains("windows") ||
                parts.Contains("win32") || parts.Contains("win64") ||
                parts.Any(p => p.StartsWith("mingw"));
        }

        public static bool IsMacOS(this string targetTriple)
        {
            var parts = targetTriple.Split('-');
            return parts.Contains("apple") ||
                parts.Contains("darwin") || parts.Contains("osx");
        }
    }
}
