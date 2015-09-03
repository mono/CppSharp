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
    public void TestToStringOverride()
    {
        var date = new Date(24, 12, 1924);
        var s = date.ToString();
        Assert.AreEqual("24/12/1924", s);
    }

    [Test]
    public void TestStdString()
    {
        Assert.AreEqual("test_test", new Date(0, 0, 0).testStdString("test"));
    }
}