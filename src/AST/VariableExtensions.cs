namespace CppSharp.AST
{
    public static class VariableExtensions
    {
        public static string GetMangled(this Variable variable, string targetTriple) =>
            variable.Mangled[0] == '_' && variable.Namespace is TranslationUnit &&
                targetTriple.IsMacOS() ?
                // the symbol name passed to dlsym() must NOT be prepended with an underscore
                // https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man3/dlsym.3.html
                variable.Mangled.TrimStart('_') : variable.Mangled;
    }
}
