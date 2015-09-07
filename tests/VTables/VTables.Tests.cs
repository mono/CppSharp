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

public class ManagedDerivedClassVirtual : DerivedClassVirtual
{
    public override unsafe int RetInt()
    {
        return 15;
    }
}

public class ManagedDerivedClassVirtualRetBase : DerivedClassVirtual
{
    public override unsafe int RetInt()
    {
        return base.RetInt();
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

    void TestVirtualFunction(BaseClassVirtual obj, int actual)
    {
        var ret = obj.RetInt();
        Assert.AreEqual(actual, ret);

        ret = BaseClassVirtual.VirtualCallRetInt(obj);
        Assert.AreEqual(actual, ret);
    }

    [Test]
    public void TestVirtualFuntionRetVal()
    {
        // Virtual Functions Object Slicing case
        // See http://stackoverflow.com/questions/3479712/virtual-functions-object-slicing
        var baseVirtual = BaseClassVirtual.GetBase();
        TestVirtualFunction(baseVirtual, 5);

        BaseClassVirtual baseClass = new DerivedClassVirtual();
        TestVirtualFunction(baseClass, 10);

        var basePtr = BaseClassVirtual.GetBasePtr();
        TestVirtualFunction(basePtr, 10);

        var managed = new ManagedDerivedClassVirtual();
        TestVirtualFunction(managed, 15);

        baseClass = managed;
        TestVirtualFunction(baseClass, 15);

        var retBase = new ManagedDerivedClassVirtualRetBase();
        TestVirtualFunction(retBase, 10);
    }
}
