using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CppSharp.Generators;
using NUnit.Framework;

namespace CppSharp.Utils
{
    /// <summary>
    /// The main NUnit fixture base class for a generator-based tests project.
    /// Provides support for a text-based test system that looks for lines
    /// in the native test declarations that match a certain pattern, which
    /// are used for certain kinds of tests that cannot be done with just
    /// C# code and using the generated wrappers.
    /// </summary>
    [TestFixture]
    public abstract class GeneratorTestFixture
    {
        readonly string assemblyName;

        protected GeneratorTestFixture()
        {
            var location = Assembly.GetCallingAssembly().Location;
            assemblyName = Path.GetFileNameWithoutExtension(location);
        }

        static bool GetGeneratorKindFromLang(string lang, out GeneratorKind kind)
        {
            kind = GeneratorKind.CSharp;

            switch(lang)
            {
            case "CSharp":
            case "C#":
                kind = GeneratorKind.CSharp;
                return true;
            case "CLI":
                kind = GeneratorKind.CLI;
                return true;
            }

            return false;
        }

        [Test]
        public void CheckDirectives()
        {
            var name = assemblyName.Substring(0, assemblyName.IndexOf('.'));
            var kind = assemblyName.Substring(assemblyName.LastIndexOf('.') + 1);
            GeneratorKind testKind;
            if (!GetGeneratorKindFromLang(kind, out testKind))
                throw new NotSupportedException("Unknown language generator");

            var path = Path.GetFullPath(GeneratorTest.GetTestsDirectory(name));

            foreach (var header in Directory.EnumerateFiles(path, "*.h"))
            {
                var headerText = File.ReadAllText(header);

                // Parse the header looking for suitable lines to test.
                foreach (var line in File.ReadAllLines(header))
                {
                    var match = Regex.Match(line, @"^\s*///*\s*(\S+)\s*:\s*(.*)");
                    if (!match.Success)
                        continue;

                    var matchLang = match.Groups[1].Value;
                    GeneratorKind matchKind;
                    if (!GetGeneratorKindFromLang(matchLang.ToUpper(), out matchKind))
                        continue;

                    if (matchKind != testKind)
                        continue;

                    var matchText = match.Groups[2].Value;
                    if (string.IsNullOrWhiteSpace(matchText))
                        continue;

                    var matchIndex = headerText.IndexOf(matchText, StringComparison.Ordinal);
                    Assert.IsTrue(matchIndex != -1,
                        string.Format("Could not match '{0}' in file '{1}'",
                        matchText, header));
                }
            }

        }
    }
}
