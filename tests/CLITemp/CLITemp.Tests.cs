using CppSharp.Utils;
using NUnit.Framework;
using CLITemp;

[TestFixture]
public class CLITests : GeneratorTestFixture
{
    [Test]
    public void TestTypes()
    {
        // Attributed types
        var sum = new Types().AttributedSum(3, 4);
        Assert.That(sum, Is.EqualTo(7));
    }
}