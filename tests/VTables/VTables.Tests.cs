using System;
using CppSharp.Utils;
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

public class VTablesTests : GeneratorTestFixture
{
    [Test]
    public void TestFoo()
    {
        var foo = new Foo();
        Assert.That(foo.vfoo(), Is.EqualTo(5));
        Assert.That(foo.Vbar(), Is.EqualTo(5));
        Assert.That(foo.CallFoo(), Is.EqualTo(7));
        Assert.That(foo.CallVirtualWithParameter(6514), Is.EqualTo(6514 + 1));

        var foo2 = new FooDerived();
        Assert.That(foo2.CallFoo(), Is.EqualTo(12));
    }

    static void Main(string[] args)
    {
    }
}
