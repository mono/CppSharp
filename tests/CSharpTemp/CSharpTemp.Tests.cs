using System.Reflection;
using CSharpTemp;
using NUnit.Framework;
using Foo = CSharpTemp.Foo;

[TestFixture]
public class CSharpTempTests
{
    [Test]
    public void TestIndexer()
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
    public void TestFixedArrays()
    {
        Qux qux = new Qux();
        var array = new[] { 1, 2, 3 };
        qux.array = array;
        for (int i = 0; i < qux.array.Length; i++)
            Assert.That(array[i], Is.EqualTo(qux.array[i]));
    }

    [Test]
    public void TestMultipleInheritance()
    {
        Baz baz = new Baz();
        Assert.That(baz.method, Is.EqualTo(1));
        var bar = (IBar) baz;
        Assert.That(bar.method, Is.EqualTo(2));
        Assert.That(baz[0], Is.EqualTo(50));
        bar[0] = new Foo { A = 1000 };
        Assert.That(bar[0].A, Is.EqualTo(1000));
        Assert.That(baz.farAwayFunc, Is.EqualTo(20));
        Assert.That(baz.takesQux(baz), Is.EqualTo(20));
        Assert.That(baz.returnQux().farAwayFunc, Is.EqualTo(20));
        int cast = baz;
        Assert.That(cast, Is.EqualTo(500));
        var nested = new Baz.Nested();
        int nestedCast = nested;
        Assert.That(nestedCast, Is.EqualTo(300));
    }

    [Test]
    public void TestProperties()
    {
        var proprietor = new Proprietor();
        proprietor.value = 20;
        Assert.That(proprietor.value, Is.EqualTo(20));
        proprietor.prop = 50;
        Assert.That(proprietor.prop, Is.EqualTo(50));
        var p = new P();
        p.value = 20;
        Assert.That(p.value, Is.EqualTo(30));
        p.prop = 50;
        Assert.That(p.prop, Is.EqualTo(150));

        ComplexType complexType = new ComplexType();
        p.complexType = complexType;
        Assert.That(p.complexType.check(), Is.EqualTo(5));
    }
}