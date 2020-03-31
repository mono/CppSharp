using CppSharp.Utils;
using NUnit.Framework;
using CLI;

public class CLITests : GeneratorTestFixture
{
    [Test]
    public void TestTypes()
    {
        // Attributed types
        var sum = new Types().AttributedSum(3, 4);
        Assert.That(sum, Is.EqualTo(7));
    }

    [Test]
    public void TestStdString()
    {
        Assert.AreEqual("test_test", new Date(0, 0, 0).TestStdString("test"));
    }

    [Test]
    public void TestFreeFunctionsUniqueClassName()
    {       
        using (var freeFuncClass = new CLI.FreeFunctions.FreeFunctionsClass())
        {
            Assert.AreEqual(1, freeFuncClass.Int);
        }

        Assert.AreEqual(2, CLI.FreeFunctions.FreeFunctionsClassHelpers.F());
    }
}