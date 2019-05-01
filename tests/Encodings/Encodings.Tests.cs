using CppSharp.Utils;
using NUnit.Framework;
using Foo = Encodings.Foo;

public class EncodingsTests : GeneratorTestFixture
{
    [Test]
    public void TestFoo()
    {
        const string georgia = "საქართველო";
        Foo.Unicode = georgia;
        Assert.That(Foo.Unicode, Is.EqualTo(georgia));

        // TODO: move this, it has nothing to do with Unicode, it's here only not to break the CLI branch
        using (var foo = new Foo())
        {
            Assert.That(foo[0], Is.EqualTo(5));
        }
    }
}
