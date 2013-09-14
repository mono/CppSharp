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

        // TODO: Most of the ugliness below will disappear when pointers to simple types are represented by C#-pointers or ref modifiers instead of the nasty IntPtr
        var value = *(int*) foo[0];
        Assert.That(value, Is.EqualTo(50));
        int x = 250;
        foo[0] = new IntPtr(&x);
        value = *(int*) foo[0];
        Assert.That(value, Is.EqualTo(x));

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
}