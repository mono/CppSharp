using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CppSharp.Generators.CSharp
{
    internal class CSharpLibrarySymbolTable : TextGenerator
    {
        private static readonly Regex identifierCleanerRegex = new(@"[^\w]", RegexOptions.Compiled);
        private readonly Dictionary<string, string> symbols = new();
        private readonly HashSet<string> uniqueVariableNames = new();
        private readonly string path = string.Empty;
        private readonly string @namespace = string.Empty;
        private readonly string @class;
        private int counter = 0;

        public CSharpLibrarySymbolTable(string path, string @namespace)
        {
            this.path = path;
            @class = identifierCleanerRegex.Replace(path, "_");
            this.@namespace = (!string.IsNullOrEmpty(@namespace) ? @namespace : @class) + ".__Symbols";
        }

        public string Generate()
        {
            using (WriteBlock($"namespace {@namespace}"))
            {
                using (WriteBlock($"internal class {@class}"))
                {
                    foreach (var (_, variableIdentifier) in symbols)
                        WriteLine($"public static IntPtr {variableIdentifier} {{ get; }}");

                    using (WriteBlock($"static {@class}()"))
                    {
                        WriteLine($"var path = \"{path}\";");
                        WriteLine("var image = CppSharp.SymbolResolver.LoadImage(ref path);");
                        WriteLine("if (image == IntPtr.Zero) throw new global::System.DllNotFoundException(path);");

                        foreach (var (mangled, variableIdentifier) in symbols)
                            WriteLine($"{variableIdentifier} = CppSharp.SymbolResolver.ResolveSymbol(image, \"{mangled}\");");
                    }
                }
            }

            return ToString();
        }

        public string GetFullVariablePath(string mangled)
        {
            return $"global::{@namespace}.{@class}." + GenerateUniqueVariableIdentifier(mangled);
        }

        public string GenerateUniqueVariableIdentifier(string mangled)
        {
            if (!symbols.TryGetValue(mangled, out string result))
            {
                result = identifierCleanerRegex.Replace(mangled, "_");
                if (!result.StartsWith("_"))
                    result = "_" + result;

                if (!uniqueVariableNames.Add(result))
                {
                    result += "_v";
                    while (!uniqueVariableNames.Add(result))
                        result += counter++;
                }

                symbols[mangled] = result;
            }

            return result;
        }
    }
}
