using System;
using NUnit.Framework;
using VTables;

public class FooDerived : Foo
{
    public override int vfoo()
    {
        Console.WriteLine("Hello from FooDerived");
        return 10;
    }
}

[TestFixture]
public class VTablesTests
{
    [Test]
    public void TestFoo()
    {
        var foo = new Foo();
        Assert.That(foo.vfoo(), Is.EqualTo(5));
        Assert.That(foo.Vbar(), Is.EqualTo(3));
        Assert.That(foo.CallFoo(), Is.EqualTo(7));

        var foo2 = new FooDerived();
        Assert.That(foo2.CallFoo(), Is.EqualTo(12));
    }

    static void Main(string[] args)
    {
    }
}
