using NUnit.Framework;
using UTF16;

[TestFixture]
public class UTF16Tests
{
    [Test]
    public void TestFoo()
    {
        var foo = new Foo();
        const string georgia = "საქართველო";
        foo.Unicode = georgia;
        Assert.That(foo.Unicode, Is.EqualTo(georgia));
    }
}
