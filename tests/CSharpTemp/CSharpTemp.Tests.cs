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
        Qux qux = new Qux(null);
        var array = new[] { 1, 2, 3 };
        qux.Array = array;
        for (int i = 0; i < qux.Array.Length; i++)
            Assert.That(array[i], Is.EqualTo(qux.Array[i]));
    }

    [Test]
    public void TestMultipleInheritance()
    {
        Baz baz = new Baz();
        Assert.That(baz.Method, Is.EqualTo(1));
        var bar = (IBar) baz;
        Assert.That(bar.Method, Is.EqualTo(2));
        Assert.That(baz[0], Is.EqualTo(50));
        bar[0] = new Foo { A = 1000 };
        Assert.That(bar[0].A, Is.EqualTo(1000));
        Assert.That(baz.FarAwayFunc, Is.EqualTo(20));
        Assert.That(baz.TakesQux(baz), Is.EqualTo(20));
        Assert.That(baz.ReturnQux().FarAwayFunc, Is.EqualTo(20));
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
        proprietor.Value = 20;
        Assert.That(proprietor.Value, Is.EqualTo(20));
        proprietor.Prop = 50;
        Assert.That(proprietor.Prop, Is.EqualTo(50));
        var p = new P();
        p.Value = 20;
        Assert.That(p.Value, Is.EqualTo(30));
        p.Prop = 50;
        Assert.That(p.Prop, Is.EqualTo(150));

        ComplexType complexType = new ComplexType();
        p.ComplexType = complexType;
        Assert.That(p.ComplexType.Check(), Is.EqualTo(5));
    }
}