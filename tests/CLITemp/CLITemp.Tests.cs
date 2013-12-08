using NUnit.Framework;
using CLITemp;

[TestFixture]
public class CLITests
{
    [Test]
    public void TestTypes()
    {
        // Attributed types
        var sum = new Types().AttributedSum(3, 4);
        Assert.That(sum, Is.EqualTo(7));
    }
}