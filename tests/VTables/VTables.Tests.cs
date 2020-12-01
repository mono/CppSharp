using System;
using NUnit.Framework;
using VTables;

public class FooDerived : Foo
{
    public override int vfoo
    {
        get
        {
            Console.WriteLine("Hello from FooDerived");
            return 10;
        }
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

[TestFixture]
public class VTablesTests
{
    [Test]
    public void TestFoo()
    {
        using (var foo = new Foo())
        {
            Assert.That(foo.vfoo, Is.EqualTo(5));
            Assert.That(foo.Vbar, Is.EqualTo(5));
            Assert.That(foo.CallFoo(), Is.EqualTo(7));
            Assert.That(foo.CallVirtualWithParameter(6514), Is.EqualTo(6514 + 1));
        }

        using (var foo2 = new FooDerived())
        {
            Assert.That(foo2.CallFoo(), Is.EqualTo(12));
        }
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
        using (var baseVirtual = BaseClassVirtual.Base)
        {
            TestVirtualFunction(baseVirtual, 5);
        }

        BaseClassVirtual baseClass = new DerivedClassVirtual();
        TestVirtualFunction(baseClass, 10);
        baseClass.Dispose();

        using (var basePtr = BaseClassVirtual.BasePtr)
        {
            TestVirtualFunction(basePtr, 10);
        }

        using (var managed = new ManagedDerivedClassVirtual())
        {
            TestVirtualFunction(managed, 15);

            baseClass = managed;
            TestVirtualFunction(baseClass, 15);
        }

        using (var retBase = new ManagedDerivedClassVirtualRetBase())
        {
            TestVirtualFunction(retBase, 10);
        }
    }

    [Test]
    public void TestStdStringInField()
    {
        using (var foo = new Foo())
        {
            Assert.That(foo.S, Is.Empty);
            foo.S = "test";
            Assert.That(foo.S, Is.EqualTo("test"));
        }
    }

    [Test]
    public void TestTypeNameRTTI() =>
        Assert.That(BaseClassVirtual.TypeName, Does.EndWith(nameof(BaseClassVirtual)));
}
