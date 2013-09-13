using System.Reflection;
using NUnit.Framework;
using Foo = CSharpTemp.Foo;

[TestFixture]
public class CSharpTempTests
{
    [Test]
    public void TestIndexer()
    {
        var foo = new Foo();
        Assert.That(foo[0], Is.EqualTo(5));
    }

    [Test]
    public void TestPropertyAccessModifier()
    {
        Assert.That(typeof(Foo).GetProperty("P",
            BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
    }
}