using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CppSharp
{
    public static class IntHelpers
    {
        public static bool IsPowerOfTwo(this ulong x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }

    public static class StringHelpers
    {
        public static string CommonPrefix(this string[] ss)
        {
            if (ss.Length == 0)
            {
                return "";
            }

            if (ss.Length == 1)
            {
                return ss[0];
            }

            int prefixLength = 0;

            foreach (char c in ss[0])
            {
                if (ss.Any(s => s.Length <= prefixLength || s[prefixLength] != c))
                {
                    return ss[0].Substring(0, prefixLength);
                }
                prefixLength++;
            }

            return ss[0]; // all strings identical
        }

        public static void CleanupText(ref string debugText)
        {
            // Strip off newlines from the debug text.
            if (string.IsNullOrWhiteSpace(debugText))
            {
                debugText = string.Empty;
                return;
            }

            // TODO: Make this transformation in the output.
            debugText = Regex.Replace(debugText, " {2,}", " ");
            debugText = debugText.Replace("\n", "");
        }

        public static string[] SplitCamelCase(string input)
        {
            var str = Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled);
            return str.Trim().Split();
        }

        public static IEnumerable<string> SplitAndKeep(this string s, string seperator)
        {
            string[] obj = s.Split(new[] { seperator }, StringSplitOptions.None);

            return obj.Select((t, i) => i == obj.Length - 1 ? t : t + seperator);
        }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string ReplaceLineBreaks(this string lines, string replacement)
        {
            return lines.Replace("\r\n", replacement)
                        .Replace("\r", replacement)
                        .Replace("\n", replacement);
        }

        /// <summary>
        /// Get the string slice between the two indexes.
        /// Inclusive for start index, exclusive for end index.
        /// </summary>
        public static string Slice(this string source, int start, int end)
        {
            if (end < 0)
                end = source.Length + end;

            return source.Substring(start, end - start);
        }
    }

    public static class LinqHelpers
    {
        public static IEnumerable<T> WithoutLast<T>(this IEnumerable<T> xs)
        {
            T lastX = default(T);

            var first = true;
            foreach (var x in xs)
            {
                if (first)
                    first = false;
                else
                    yield return lastX;
                lastX = x;
            }
        }
    }

    public static class AssemblyHelpers
    {
        public static IEnumerable<Type> FindDerivedTypes(this Assembly assembly,
                                                         Type baseType)
        {
            return assembly.GetTypes().Where(baseType.IsAssignableFrom);
        }
    }

    public static class PathHelpers
    {
        public static string GetRelativePath(string fromPath, string toPath)
        {
            var path1 = fromPath.Trim('\\', '/');
            var path2 = toPath.Trim('\\', '/');

            var uri1 = new Uri("c:\\" + path1 + "\\");
            var uri2 = new Uri("c:\\" + path2 + "\\");

            return uri1.MakeRelativeUri(uri2).ToString();
        }
        
    }
}
