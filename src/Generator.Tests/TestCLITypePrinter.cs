using CppSharp;
using CppSharp.Generators.CLI;
using CppSharp.Types;
using NUnit.Framework;

namespace Generator.Tests
{
    [TypeMap("FnPtr3")]
    public class CLITypePrinterTypeMap : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "TypedefFn3";
        }
    }

    [TestFixture]
    public class CLITypePrinterTest : HeaderTestFixture
    {
        CLITypePrinter printer;

        [TestFixtureSetUp]
        public void Init()
        {
            ParseLibrary("CLITypes.h");
            printer = new CLITypePrinter(Driver, new CLITypePrinterContext());
        }

        public void CheckType<T>(T decl, string check) where T : ITypedDecl
        {
            var type = decl.Type.Visit(printer);
            Assert.That(type, Is.EqualTo(check));
        }

        public void CheckDecl<T>(T decl, string check) where T : Declaration
        {
            var output = decl.Visit(printer);
            Assert.That(output, Is.EqualTo(check));
        }

        [Test]
        public void TestPrimitive()
        {
            var p = Library.Class("Primitives");
            CheckType(p.Field("B"), "bool");
            CheckType(p.Field("C"), "char");
            CheckType(p.Field("UC"), "unsigned char");
            CheckType(p.Field("S"), "short");
            CheckType(p.Field("US"), "unsigned short");
            CheckType(p.Field("I"), "int");
            CheckType(p.Field("UI"), "unsigned int");
            CheckType(p.Field("L"), "int");
            CheckType(p.Field("UL"), "unsigned int");
            CheckType(p.Field("LL"), "long long");
            CheckType(p.Field("ULL"), "unsigned long long");

            CheckType(p.Field("F"), "float");
            CheckType(p.Field("D"), "double");
        }

        [Test]
        public void TestArray()
        {
            var c = Library.Class("Arrays");
            CheckType(c.Field("Array"), "cli::array<float>^");
            CheckType(c.Field("Prim"), "cli::array<::Primitives^>^");

        }

        [Test]
        public void TestPointers()
        {
            var p = Library.Class("Pointers");
            CheckType(p.Field("pv"), "System::IntPtr");
            CheckType(p.Field("pc"), "System::IntPtr");
            CheckType(p.Field("puc"), "System::IntPtr");
            CheckType(p.Field("cpc"), "System::String^");
            CheckType(p.Field("pi"), "System::IntPtr");
        }

        [Test]
        public void TestFunctionPointers()
        {
            var p = Library.Class("FunctionPointers");
            CheckType(p.Field("fn"), "::FnPtr^");
            CheckType(p.Field("fn2"), "::FnPtr2^");
            CheckType(p.Field("fn3"), "::FnPtr3^");
        }

        [Test]
        public void TestTypedefs()
        {
            CheckType(Library.Typedef("FnPtr"), "System::Func<int, double>^");
            CheckType(Library.Typedef("FnPtr2"), "System::Action<char, float>^");
            CheckType(Library.Typedef("FnPtr3"), "System::Action^");
        }

        [Test]
        public void TestTags()
        {
            var p = Library.Class("Tag");
            CheckType(p.Field("p"), "::Primitives^");
            CheckType(p.Field("e"), "::E");
        }
    }
}