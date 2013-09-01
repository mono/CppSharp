using NUnit.Framework;
using Basic;

[TestFixture]
public class BasicTests
{
    [Test]
    public void TestHello()
    {
        var hello = new Hello();
        hello.PrintHello("Hello world");

        Assert.That(hello.add(1, 1), Is.EqualTo(2));
        Assert.That(hello.add(5, 5), Is.EqualTo(10));

        Assert.IsTrue(hello.test1(3, 3.0f));
        Assert.IsFalse(hello.test1(2, 3.0f));

        var foo = new Foo { A = 4, B = 7 };
        Assert.That(hello.AddFoo(foo), Is.EqualTo(11));
        Assert.That(hello.AddFooPtr(foo), Is.EqualTo(11));
        Assert.That(hello.AddFooRef(foo), Is.EqualTo(11));

        var bar = new Bar { A = 4, B = 7 };
        Assert.That(hello.AddBar(bar), Is.EqualTo(11));

        var retFoo = hello.RetFoo(7, 2.0f);
        Assert.That(retFoo.A, Is.EqualTo(7));
        Assert.That(retFoo.B, Is.EqualTo(2.0));

        var foo2 = new Foo2 { A = 4, B = 2, C = 3 };
        Assert.That(hello.AddFoo(foo2), Is.EqualTo(6));
        Assert.That(hello.AddFoo2(foo2), Is.EqualTo(9));

        var bar2 = new Bar2 { A = 4, B = 7, C = 3 };
        Assert.That(hello.AddBar2(bar2), Is.EqualTo(14));

        Assert.That(hello.RetEnum(Enum.A), Is.EqualTo(0));
        Assert.That(hello.RetEnum(Enum.B), Is.EqualTo(2));
        Assert.That(hello.RetEnum(Enum.C), Is.EqualTo(5));
        Assert.That(hello.RetEnum(Enum.D), Is.EqualTo(-2147483648));
        Assert.That(hello.RetEnum(Enum.E), Is.EqualTo(1));
        Assert.That(hello.RetEnum(Enum.F), Is.EqualTo(-9));
    }

    [Test]
    public void TestNullRef()
    {
        var hello = new Hello ();
        Assert.That(hello.RetNull(), Is.Null);
    }

    [Test]
    public void TestUnaryOperator()
    {
        var bar = new Bar { A = 4, B = 7 };
        var barMinus = -bar;
        Assert.That(barMinus.A, Is.EqualTo(-bar.A));
        Assert.That(barMinus.B, Is.EqualTo(-bar.B));
    }

    [Test]
    public void TestBinaryOperator()
    {
        var bar = new Bar { A = 4, B = 7 };
        var bar1 = new Bar { A = 5, B = 10 };
        var barSum = bar + bar1;
        Assert.That(barSum.A, Is.EqualTo(bar.A + bar1.A));
        Assert.That(barSum.B, Is.EqualTo(bar.B + bar1.B));
    }

    [Test]
    public void TestAmbiguous()
    {
        var def = new DefaultParameters();
        def.Foo(1, 2);
        def.Bar();
    }

    [Test, Ignore]
    public void TestLeftShiftOperator()
    {
        Foo2 foo2 = new Foo2();
        foo2.C = 2;
        Foo2 result = foo2 << 3;
        Assert.That(result.C, Is.EqualTo(16));
    }

    [Test]
    public void TestAbstractReturnType()
    {
        var returnsAbstractFoo = new ReturnsAbstractFoo();
        var abstractFoo = returnsAbstractFoo.getFoo();
        Assert.AreEqual(abstractFoo.pureFunction(), 5);
    }
}
 