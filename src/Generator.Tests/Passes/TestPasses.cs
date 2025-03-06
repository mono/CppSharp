using CodingSeb.ExpressionEvaluator;
using CppSharp.AST;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using NUnit.Framework;
using System.Collections;
using System.Linq;

namespace CppSharp.Generator.Tests.Passes
{
    [TestFixture]
    public class TestPasses : ASTTestFixture
    {
        [SetUp]
        public void Setup()
        {
            ParseLibrary("Passes.h");
            passBuilder = new PassBuilder<TranslationUnitPass>(Driver.Context);
        }

        [TearDown]
        public void TearDown()
        {
            Driver.Dispose();
        }

        [Test]
        public void TestExtractInterfacePass()
        {
            var c = AstContext.Class("TestExtractInterfacePass");

            Assert.IsNull(c.GetInterface());

            passBuilder.AddPass(new ExtractInterfacePass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsNotNull(c.GetInterface());
            Assert.AreEqual("ITestExtractInterfacePass", c.GetInterface().Name);
        }

        [Test]
        public void TestCheckEnumsPass()
        {
            var @enum = AstContext.Enum("FlagEnum");
            var enum2 = AstContext.Enum("FlagEnum2");
            var boolClassEnum = AstContext.Enum("BoolEnum");
            var ucharClassEnum = AstContext.Enum("UCharEnum");

            Assert.IsFalse(@enum.IsFlags);
            Assert.IsFalse(enum2.IsFlags);
            Assert.IsFalse(boolClassEnum.IsFlags);
            Assert.IsFalse(ucharClassEnum.IsFlags);

            Assert.IsTrue(boolClassEnum.BuiltinType.Type == PrimitiveType.Bool);
            Assert.IsTrue(ucharClassEnum.BuiltinType.Type == PrimitiveType.UChar);

            passBuilder.AddPass(new CheckEnumsPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsTrue(@enum.IsFlags);
            Assert.IsFalse(enum2.IsFlags);
            Assert.IsFalse(boolClassEnum.IsFlags);
            Assert.IsTrue(ucharClassEnum.IsFlags);

            Assert.IsTrue(boolClassEnum.BuiltinType.Type != PrimitiveType.Bool, "C# does not support Bool enums");
            Assert.IsTrue(ucharClassEnum.BuiltinType.Type == PrimitiveType.UChar);
        }

        [Test]
        public void TestCheckStaticClassPass()
        {
            var staticClass = AstContext.Class("TestCheckStaticClass");
            var staticStruct = AstContext.Class("TestCheckStaticStruct");
            var staticClassDeletedCtor = AstContext.Class("TestCheckStaticClassDeleted");
            var nonStaticClass = AstContext.Class("TestCheckNonStaticClass");
            var nonStaticEmptyClass = AstContext.Class("TestCommentsPass");

            Assert.IsFalse(staticClass.IsStatic);
            Assert.IsFalse(staticStruct.IsStatic);
            Assert.IsFalse(staticClassDeletedCtor.IsStatic);
            Assert.IsFalse(nonStaticClass.IsStatic);
            Assert.IsFalse(nonStaticEmptyClass.IsStatic);

            passBuilder.AddPass(new CheckStaticClassPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsTrue(staticClass.IsStatic, "`TestCheckStaticClass` should be static");
            Assert.IsTrue(staticStruct.IsStatic, "`TestCheckStaticStruct` should be static");
            Assert.IsTrue(staticClassDeletedCtor.IsStatic, "`TestCheckStaticClassDeleted` should be static");

            Assert.IsFalse(nonStaticClass.IsStatic, "`TestCheckNonStaticClass` should NOT be static, since it has a private data field with default ctor");
            Assert.IsFalse(nonStaticEmptyClass.IsStatic, "`TestCommentsPass` should NOT be static, since it doesn't have any static declarations");
        }

        [Test]
        public void TestFunctionToInstancePass()
        {
            var c = AstContext.Class("Foo");

            Assert.IsNull(c.Method("Start"));

            passBuilder.AddPass(new FunctionToInstanceMethodPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestFunctionToStaticPass()
        {
            var c = AstContext.Class("Foo");

            Assert.IsTrue(AstContext.Function("FooStart").IsGenerated);
            Assert.IsNull(c.Method("Start"));

            passBuilder.AddPass(new FunctionToStaticMethodPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsFalse(AstContext.Function("FooStart").IsGenerated);
            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestCleanCommentsPass()
        {
            var c = AstContext.FindClass("TestCommentsPass").FirstOrDefault();

            passBuilder.AddPass(new CleanCommentsPass());
            passBuilder.RunPasses(pass => pass.VisitDeclaration(c));

            var para = (ParagraphComment)c.Comment.FullComment.Blocks[0];
            var textGenerator = new TextGenerator();
            textGenerator.Print(para, CommentKind.BCPLSlash);

            Assert.That(textGenerator.StringBuilder.ToString().Trim(),
                Is.EqualTo("/// <summary>A simple test.</summary>"));
        }

        [Test]
        public void TestCaseRenamePass()
        {
            Type.TypePrinterDelegate += TypePrinterDelegate;

            var c = AstContext.Class("TestRename");

            passBuilder.AddPass(new FieldToPropertyPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var method = c.Method("lowerCaseMethod");
            var property = c.Properties.Find(p => p.Name == "lowerCaseField");

            passBuilder.RenameDeclsUpperCase(RenameTargets.Any);
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.That(method.Name, Is.EqualTo("LowerCaseMethod"));
            Assert.That(property.Name, Is.EqualTo("LowerCaseField"));

            Type.TypePrinterDelegate -= TypePrinterDelegate;
        }

        [Test]
        public void TestCleanEnumItemNames()
        {
            AstContext.GenerateEnumFromMacros("TestEnumItemName", "TEST_ENUM_ITEM_NAME_(.*)");

            var @enum = AstContext.Enum("TestEnumItemName");
            Assert.IsNotNull(@enum);

            // Testing the values read for the enum
            Assert.AreEqual(3, @enum.Items[0].Value); // Decimal literal
            Assert.AreEqual(0, @enum.Items[1].Value); // Hex literal
            Assert.AreEqual(1, @enum.Items[2].Value); // Hex literal with suffix
            Assert.AreEqual(2, @enum.Items[3].Value); // Enum item

            passBuilder.RemovePrefix("TEST_ENUM_ITEM_NAME_", RenameTargets.EnumItem);
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.That(@enum.Items[1].Name, Is.EqualTo("_0"));
        }

        [Test]
        public void TestEnumBaseTypesFromMacroValues()
        {
            AstContext.GenerateEnumFromMacros("TestEnumMaxSbyte", "TEST_ENUM_MAX_SBYTE");
            var @enum = AstContext.Enum("TestEnumMaxSbyte");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(sbyte.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.UChar, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxByte", "TEST_ENUM_MAX_BYTE");
            @enum = AstContext.Enum("TestEnumMaxByte");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(byte.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.UChar, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxShort", "TEST_ENUM_MAX_SHORT");
            @enum = AstContext.Enum("TestEnumMaxShort");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(short.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.Short, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxUshort", "TEST_ENUM_MAX_USHORT");
            @enum = AstContext.Enum("TestEnumMaxUshort");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(ushort.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.UShort, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxInt", "TEST_ENUM_MAX_INT");
            @enum = AstContext.Enum("TestEnumMaxInt");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(int.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.Int, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxLong", "TEST_ENUM_MAX_LONG");
            @enum = AstContext.Enum("TestEnumMaxLong");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(long.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.LongLong, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxUint", "TEST_ENUM_MAX_UINT");
            @enum = AstContext.Enum("TestEnumMaxUint");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(uint.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.UInt, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMaxUlong", "TEST_ENUM_MAX_ULONG");
            @enum = AstContext.Enum("TestEnumMaxUlong");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(ulong.MaxValue, @enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.ULongLong, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMinInt", "TEST_ENUM_MIN_INT");
            @enum = AstContext.Enum("TestEnumMinInt");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(int.MinValue, (long)@enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.Int, @enum.BuiltinType.Type);

            AstContext.GenerateEnumFromMacros("TestEnumMinLong", "TEST_ENUM_MIN_LONG");
            @enum = AstContext.Enum("TestEnumMinLong");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(long.MinValue, (long)@enum.Items[0].Value);
            Assert.AreEqual(PrimitiveType.LongLong, @enum.BuiltinType.Type);
        }

        // These tests have nothing to do with testing passes except that the expression evaluator
        // is only used by GenerateEnumFromMacros. Add a separate TestFixture? 
        static ExprEvalTestParams[] TestExpressionEvaluatorTestCases =
        {
            new ExprEvalTestParams("2147483647", int.MaxValue),
            new ExprEvalTestParams("-2147483648", int.MinValue),
            new ExprEvalTestParams("4294967295", uint.MaxValue),
            new ExprEvalTestParams("9223372036854775807", long.MaxValue),
            new ExprEvalTestParams("-9223372036854775808", long.MinValue),
            new ExprEvalTestParams("18446744073709551615", ulong.MaxValue),

            // Must use "L" suffix here or the expression evaluator will overflow with no warning. Note
            // that it won't compile in C# without it.
            new ExprEvalTestParams("-2147483648L - 1", -2147483648L - 1),

            // Note that the dynamic subsystem used by the expression evaluator types this result as
            // long, but the compiler types the expression a uint. Adding the casts to dynamic in
            // the expressions below matches the result of the expression evaluator. Not sure we
            // care that they're different.
            new ExprEvalTestParams("2147483648 + 5", (dynamic)2147483648 + (dynamic)5),
            new ExprEvalTestParams("5 + 2147483648", (dynamic)5 + (dynamic)2147483648),

            new ExprEvalTestParams("0x2A828670572C << 1", 0x2A828670572C << 1),
            new ExprEvalTestParams("-0xFC84D76B0482", -0xFC84D76B0482),
            new ExprEvalTestParams("27 - 9223372036854775807", 27 - 9223372036854775807),
            new ExprEvalTestParams("9223372036854775807", 9223372036854775807),
            new ExprEvalTestParams("18446744073709551615", 18446744073709551615),
            new ExprEvalTestParams("1 << 5", 1 << 5),
            new ExprEvalTestParams("1 << 32", 1 << 32),
            new ExprEvalTestParams("1L << 32", 1L << 32),
            new ExprEvalTestParams("5u", 5u),
            new ExprEvalTestParams("5ul", 5ul),
            new ExprEvalTestParams("\"This is\" + \" a string expression\"", "This is" + " a string expression"),
            new ExprEvalTestParams("(17 - 48 + 80 * 81 - 88) + (74 + 79 - 50) - ((76 - 51 - 88 + (9 + 98 - 47)))", (17 - 48 + 80 * 81 - 88) + (74 + 79 - 50) - ((76 - 51 - 88 + (9 + 98 - 47)))),
            new ExprEvalTestParams("3.14159265", 3.14159265),
            new ExprEvalTestParams("16 - 47 * (75 / 91) + 44", 16 - 47 * (75 / 91) + 44),  // Does C# truncate the same way?
            new ExprEvalTestParams("16 - 47 * (75d / 91d) + 44", 16 - 47 * (75d / 91d) + 44),
            new ExprEvalTestParams("69d / 5 - 48 - 47 / (82d - 71 + 2 + 6 / 39d) - 56", 69d / 5 - 48 - 47 / (82d - 71 + 2 + 6 / 39d) - 56),
            new ExprEvalTestParams("55.59m", 55.59m),
            new ExprEvalTestParams("55.59m + 23", 55.59m + 23),
            new ExprEvalTestParams("'A'", 'A'),
            new ExprEvalTestParams("'A'+'B'", 'A' + 'B'),
            new ExprEvalTestParams("(int)'A'", (int)'A'),

            // C++ specific sufixes not supported. C++ octal not supported.
            //new EETestParams("5ll", 5L),
            //new EETestParams("5ull", 5UL),
            //new EETestParams("016", 8 + 6),

            new ExprEvalTestParams("V1 | V2", 3, ("V1", 1), ("V2", 2)),
            new ExprEvalTestParams("TRUE && FALSE", false, ("TRUE", true), ("FALSE", false)),
        };

        class ExprEvalTestParams
        {
            public readonly string Expression;
            public readonly object ExpectedResult;
            public readonly (string Name, object Value)[] Symbols;
            public ExprEvalTestParams(string expression, object expectedResult, params (string Name, object Value)[] symbols)
            {
                Expression = expression;
                ExpectedResult = expectedResult;
                Symbols = symbols;
            }
        }

        static IEnumerable GetExpressionEvaluatorTestCases()
        {
            var testNumber = 100;
            foreach (var tc in TestExpressionEvaluatorTestCases)
            {
                // "testNumber" is just used to cause the test runner to show the tests in the order
                // we've listed them which is simply a development time convenience.
                yield return new TestCaseData(testNumber++, tc.Expression, tc.ExpectedResult, tc.Symbols);
            }
        }

        [TestCaseSource(nameof(GetExpressionEvaluatorTestCases))]
        public void TestExpressionEvaluator(int testNumber, string expression, object expectedResult, (string SymbolName, object SymbolValue)[] symbols)
        {
            var evaluator = symbols == null || symbols.Length == 0
                            ? new ExpressionEvaluator()
                            : new ExpressionEvaluator(symbols.ToDictionary(s => s.SymbolName, s => s.SymbolValue));

            var result = evaluator.Evaluate(expression);
            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedResult.GetType(), result.GetType());
        }

        [Test]
        public void TestEnumsWithBitwiseExpressionMacroValues()
        {
            var @enum = AstContext.GenerateEnumFromMacros("TestBitwiseShift", "TEST_BITWISE_SHIFT_(.*)");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(PrimitiveType.LongLong, @enum.BuiltinType.Type);
            Assert.AreEqual(0x2A828670572C << 1, (long)@enum.Items[0].Value);

            @enum = AstContext.GenerateEnumFromMacros("TestNegativeHex", "TEST_NEGATIVE_HEX_(.*)");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(PrimitiveType.LongLong, @enum.BuiltinType.Type);
            Assert.AreEqual(-0xFC84D76B0482, (long)@enum.Items[0].Value);

            @enum = AstContext.GenerateEnumFromMacros("TestBitwiseOr", "TEST_BITWISE_OR_1");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(PrimitiveType.UChar, @enum.BuiltinType.Type);
            Assert.AreEqual(0x7F | 0x80, @enum.Items[0].Value);

            @enum = AstContext.GenerateEnumFromMacros("TestBitwiseAnd", "TEST_BITWISE_AND_(.*)");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(PrimitiveType.UChar, @enum.BuiltinType.Type);
            Assert.AreEqual(0x7F & 0xFF, @enum.Items[0].Value);
            Assert.AreEqual(0x73 & -1, @enum.Items[1].Value);
            Assert.AreEqual(0x42 & ~0x2, @enum.Items[2].Value);

            @enum = AstContext.GenerateEnumFromMacros("TestBitwiseXor", "TEST_BITWISE_XOR_(.*)");
            Assert.IsNotNull(@enum);
            Assert.AreEqual(PrimitiveType.UChar, @enum.BuiltinType.Type);
            Assert.AreEqual(0x7F ^ 0x03, @enum.Items[0].Value);
        }

        [Test]
        public void TestUnnamedEnumSupport()
        {
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var unnamedEnum1 = AstContext.FindEnum("Unnamed_Enum_1").Single();
            var unnamedEnum2 = AstContext.FindEnum("Unnamed_Enum_2").Single();
            Assert.IsNotNull(unnamedEnum1);
            Assert.IsNotNull(unnamedEnum2);

            Assert.AreEqual(2, unnamedEnum1.Items.Count);
            Assert.AreEqual(2, unnamedEnum2.Items.Count);

            Assert.AreEqual(1, unnamedEnum1.Items[0].Value);
            Assert.AreEqual(2, unnamedEnum1.Items[1].Value);
            Assert.AreEqual(3, unnamedEnum2.Items[0].Value);
            Assert.AreEqual(4, unnamedEnum2.Items[1].Value);
        }

        [Test, Ignore("Nameless enums are no longer uniquely named.")]
        public void TestUniqueNamesAcrossTranslationUnits()
        {
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var unnamedEnum1 = AstContext.GetEnumWithMatchingItem("UnnamedEnumA1");
            var unnamedEnum2 = AstContext.GetEnumWithMatchingItem("UnnamedEnumB1");
            Assert.IsNotNull(unnamedEnum1);
            Assert.IsNotNull(unnamedEnum2);

            Assert.AreNotEqual(unnamedEnum1.Name, unnamedEnum2.Name);
        }

        [Test]
        public void TestSkippedPrivateMethod()
        {
            AstContext.IgnoreClassMethodWithName("Foo", "toIgnore");
            Assert.That(AstContext.FindClass("Foo").First().Methods.Find(
                m => m.Name == "toIgnore"), Is.Null);
        }

        [Test]
        public void TestSetPropertyAsReadOnly()
        {
            const string className = "TestReadOnlyProperties";
            passBuilder.AddPass(new FieldToPropertyPass());
            passBuilder.AddPass(new GetterSetterToPropertyPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));
            AstContext.SetPropertyAsReadOnly(className, "readOnlyProperty");
            Assert.IsFalse(AstContext.FindClass(className).First().Properties.Find(
                m => m.Name == "readOnlyProperty").HasSetter);
            AstContext.SetPropertyAsReadOnly(className, "readOnlyPropertyMethod");
            Assert.IsFalse(AstContext.FindClass(className).First().Properties.Find(
                m => m.Name == "readOnlyPropertyMethod").HasSetter);
        }

        [Test]
        public void TestCheckAmbiguousFunctionsPass()
        {
            passBuilder.AddPass(new CheckAmbiguousFunctions());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));
            var @class = AstContext.FindClass("TestCheckAmbiguousFunctionsPass").FirstOrDefault();
            Assert.IsNotNull(@class);
            var overloads = @class.Methods.Where(m => m.Name == "Method");
            var constMethod = overloads
                .FirstOrDefault(m => m.IsConst && m.Parameters.Count == 0);
            var nonConstMethod = overloads
                .FirstOrDefault(m => !m.IsConst && m.Parameters.Count == 0);
            Assert.IsNotNull(constMethod);
            Assert.IsNotNull(nonConstMethod);
            Assert.IsTrue(constMethod.GenerationKind == GenerationKind.None);
            Assert.IsTrue(nonConstMethod.GenerationKind == GenerationKind.Generate);
            var constMethodWithParam = overloads
                .FirstOrDefault(m => m.IsConst && m.Parameters.Count == 1);
            var nonConstMethodWithParam = overloads
                .FirstOrDefault(m => !m.IsConst && m.Parameters.Count == 1);
            Assert.IsNotNull(constMethodWithParam);
            Assert.IsNotNull(nonConstMethodWithParam);
            Assert.IsTrue(constMethodWithParam.GenerationKind == GenerationKind.None);
            Assert.IsTrue(nonConstMethodWithParam.GenerationKind == GenerationKind.Generate);
        }

        [Test]
        public void TestSetMethodAsInternal()
        {
            var c = AstContext.Class("TestMethodAsInternal");
            var method = c.Method("beInternal");
            Assert.AreEqual(method.Access, AccessSpecifier.Public);
            passBuilder.AddPass(new CheckMacroPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));
            Assert.AreEqual(method.Access, AccessSpecifier.Internal);
        }

        private string TypePrinterDelegate(Type type)
        {
            return type.Visit(new CSharpTypePrinter(Driver.Context)).Type;
        }

        [Test]
        public void TestAbstractOperator()
        {
            passBuilder.AddPass(new ValidateOperatorsPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var @class = AstContext.FindDecl<Class>("ClassWithAbstractOperator").First();
            Assert.AreEqual(@class.Operators.First().GenerationKind, GenerationKind.None);
        }

        [Test]
        public void TestFlattenAnonymousTypesToFields()
        {
            passBuilder.AddPass(new FlattenAnonymousTypesToFields());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var @class = AstContext.FindDecl<Class>("TestFlattenAnonymousTypesToFields").First();

            /* TODO: Enable this test and fix the parsing bug
            var @public = @class.Fields.Where(f => f.Name == "Public").FirstOrDefault();
            Assert.IsNotNull(@public);
            Assert.AreEqual(AccessSpecifier.Public, @public.Access); */

            var @protected = @class.Fields.Find(f => f.Name == "Protected");
            Assert.IsNotNull(@protected);
            Assert.AreEqual(AccessSpecifier.Protected, @protected.Access);
        }

        private PassBuilder<TranslationUnitPass> passBuilder;
    }
}
