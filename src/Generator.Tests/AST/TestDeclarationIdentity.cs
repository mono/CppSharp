using System;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Parser;
using NUnit.Framework;

namespace CppSharp.Generator.Tests.AST
{
    /// <summary>
    /// Without a unity build, every header is parsed by its own Clang instance, which is freed
    /// before the next header is parsed. Clang declarations of later headers can then reuse the
    /// addresses of earlier ones, so these addresses must not be used to identify declarations
    /// when the native AST is converted.
    /// </summary>
    [TestFixture]
    public class TestDeclarationIdentity
    {
        private const int HeaderCount = 40;
        private const int MethodCount = 8;

        private string headersDir;
        private Driver driver;
        private ASTContext astContext;

        [OneTimeSetUp]
        public void Init()
        {
            headersDir = Path.Combine(Path.GetTempPath(), "CppSharp.TestDeclarationIdentity." + Guid.NewGuid());
            Directory.CreateDirectory(headersDir);

            // A header shared by all others, like the base class headers of a large library
            File.WriteAllText(Path.Combine(headersDir, "Shared.h"),
                "#pragma once\n" +
                "class Shared\n{\npublic:\n" +
                string.Concat(Enumerable.Range(0, MethodCount).Select(m => $"    void SharedMethod{m}(int sharedParam{m});\n")) +
                "};\n");

            var options = new DriverOptions { GeneratorKind = GeneratorKind.CSharp };
            var module = options.AddModule("Test");
            module.IncludeDirs.Add(headersDir);

            for (var h = 0; h < HeaderCount; h++)
            {
                var header = $"Header{h}.h";
                File.WriteAllText(Path.Combine(headersDir, header),
                    "#pragma once\n" +
                    "#include \"Shared.h\"\n" +
                    $"class Class{h} : public Shared\n{{\npublic:\n" +
                    string.Concat(Enumerable.Range(0, MethodCount).Select(m => $"    void Method{h}_{m}(int param{h}_{m});\n")) +
                    "};\n");
                module.Headers.Add(header);
            }

            driver = new Driver(options)
            {
                ParserOptions = new ParserOptions { UnityBuild = false, SkipPrivateDeclarations = true }
            };

            driver.Setup();
            if (!driver.ParseCode())
                throw new Exception("Error parsing the code");

            astContext = driver.Context.ASTContext;
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            driver?.Dispose();
            if (headersDir != null && Directory.Exists(headersDir))
                Directory.Delete(headersDir, true);
        }

        [Test]
        public void TestMethodsBelongToTheirClass()
        {
            for (var h = 0; h < HeaderCount; h++)
            {
                var @class = astContext.FindCompleteClass($"Class{h}");
                Assert.IsNotNull(@class, $"Class{h} not found");

                var methods = @class.Methods.Where(m => !m.IsImplicit && m.Kind == CXXMethodKind.Normal).ToList();
                Assert.That(methods.Select(m => m.Name),
                    Is.EquivalentTo(Enumerable.Range(0, MethodCount).Select(m => $"Method{h}_{m}")),
                    $"Methods of Class{h}");

                foreach (var method in methods)
                {
                    Assert.AreSame(@class, method.Namespace, $"Namespace of {method.Name}");
                    Assert.AreEqual(1, method.Parameters.Count, $"Parameters of {method.Name}");
                    Assert.AreEqual(method.Name.Replace("Method", "param"), method.Parameters[0].Name,
                        $"Parameter of {method.Name}");
                }
            }
        }

        [Test]
        public void TestSharedClassIsComplete()
        {
            var @class = astContext.FindCompleteClass("Shared");
            Assert.IsNotNull(@class);

            var methods = @class.Methods.Where(m => !m.IsImplicit && m.Kind == CXXMethodKind.Normal).ToList();
            Assert.That(methods.Select(m => m.Name),
                Is.EquivalentTo(Enumerable.Range(0, MethodCount).Select(m => $"SharedMethod{m}")));
            foreach (var method in methods)
                Assert.AreEqual(method.Name.Replace("SharedMethod", "sharedParam"), method.Parameters.Single().Name);
        }
    }
}
