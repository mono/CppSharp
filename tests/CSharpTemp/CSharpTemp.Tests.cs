using System;
using System.Reflection;
using CSharpTemp;
using NUnit.Framework;
using Foo = CSharpTemp.Foo;

[TestFixture]
public class CSharpTempTests
{
    [Test]
    public unsafe void TestIndexer()
    {
        var foo = new Foo();

        Assert.That(foo[0], Is.EqualTo(50));
        foo[0] = 250;
        Assert.That(foo[0], Is.EqualTo(250));

        Assert.That(foo[(uint) 0], Is.EqualTo(15));
        
        var bar = new Bar();
        Assert.That(bar[0].A, Is.EqualTo(10));
        bar[0] = new Foo { A = 25 };
        Assert.That(bar[0].A, Is.EqualTo(25));
    }

    [Test]
    public void TestPropertyAccessModifier()
    {
        Assert.That(typeof(Foo).GetProperty("P",
            BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
    }

    [Test]
    public void TestMultipleInheritance()
    {
        Baz baz = new Baz();
        Assert.That(baz.method(), Is.EqualTo(1));
        var bar = (IBar) baz;
        Assert.That(bar.method(), Is.EqualTo(2));
        Assert.That(baz[0], Is.EqualTo(50));
        bar[0] = new Foo { A = 1000 };
        Assert.That(bar[0].A, Is.EqualTo(1000));
        Assert.That(baz.farAwayFunc(), Is.EqualTo(20));
        Assert.That(baz.takesQux(baz), Is.EqualTo(20));
        Assert.That(baz.returnQux().farAwayFunc(), Is.EqualTo(20));
        int cast = baz;
        Assert.That(cast, Is.EqualTo(500));
    }
}