using NUnit.Framework;

[TestFixture]
public class HelloTests
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

        var foo2 = new Foo2 {A = 4, B = 2, C = 3};
        Assert.That(hello.AddFoo(foo2), Is.EqualTo(6));
        Assert.That(hello.AddFoo2(foo2), Is.EqualTo(9));

        var bar2 = new Bar2 { A = 4, B = 7, C = 3 };
        Assert.That(hello.AddBar2(bar2), Is.EqualTo(14));
    }

    static void Main(string[] args)
    {
        var hello = new Hello();
    }
}
 