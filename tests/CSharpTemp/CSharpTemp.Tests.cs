using System;
using System.Reflection;
using CSharpTemp;
using CppSharp.Utils;
using NUnit.Framework;
using Foo = CSharpTemp.Foo;

public class CSharpTempTests : GeneratorTestFixture
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
    public void TestMultipleInheritance()
    {
        var baz = new Baz();
        Assert.That(baz.Method, Is.EqualTo(1));
        var bar = (IBar) baz;
        Assert.That(bar.Method, Is.EqualTo(2));
        Assert.That(baz[0], Is.EqualTo(50));
        bar[0] = new Foo { A = 1000 };
        Assert.That(bar[0].A, Is.EqualTo(1000));
        Assert.That(baz.FarAwayFunc, Is.EqualTo(20));
        Assert.That(baz.TakesQux(baz), Is.EqualTo(20));
        Assert.That(baz.ReturnQux().FarAwayFunc, Is.EqualTo(20));
    }

    [Test]
    public void TestProperties()
    {
        var proprietor = new Proprietor();
        proprietor.Value = 20;
        Assert.That(proprietor.Value, Is.EqualTo(20));
        proprietor.Prop = 50;
        Assert.That(proprietor.Prop, Is.EqualTo(50));
        var p = new P((IQux) null);
        p.Value = 20;
        Assert.That(p.Value, Is.EqualTo(30));
        p.Prop = 50;
        Assert.That(p.Prop, Is.EqualTo(150));

        ComplexType complexType = new ComplexType();
        p.ComplexType = complexType;
        Assert.That(p.ComplexType.Check(), Is.EqualTo(5));

        Assert.That(p.Test, Is.True);
        Assert.That(p.IsBool, Is.False);
    }

    [Test]
    public void TestAttributes()
    {
        Assert.That(typeof(Qux).GetMethod("Obsolete")
            .GetCustomAttributes(typeof(ObsoleteAttribute), false).Length,
            Is.GreaterThan(0));
    }

    [Test]
    public void TestDestructors()
    {
        CSharpTemp.TestDestructors.InitMarker();
        Assert.AreEqual(0, CSharpTemp.TestDestructors.Marker);

        var dtors = new TestDestructors();
        Assert.AreEqual(0xf00d, CSharpTemp.TestDestructors.Marker);
        dtors.Dispose();
        Assert.AreEqual(0xcafe, CSharpTemp.TestDestructors.Marker);
    }

    [Test]
    public unsafe void TestArrayOfPointersToPrimitives()
    {
        var bar = new Bar();
        var array = new IntPtr[1];
        int i = 5;
        array[0] = new IntPtr(&i);
        bar.ArrayOfPrimitivePointers = array;
        Assert.That(i, Is.EqualTo(*(int*) bar.ArrayOfPrimitivePointers[0]));
    }

    [Test]
    public void TestCopyConstructorValue()
    {
        var testCopyConstructorVal = new TestCopyConstructorVal { A = 10, B = 5 };
        var copyBar = new TestCopyConstructorVal(testCopyConstructorVal);
        Assert.That(testCopyConstructorVal.A, Is.EqualTo(copyBar.A));
        Assert.That(testCopyConstructorVal.B, Is.EqualTo(copyBar.B));
    }

    [Test]
    public void TestPropertiesConflictingWithMethod()
    {
        var p = new P((IQux) new Qux()) { Test = true };
        Assert.That(p.Test, Is.True);
        p.GetTest();
    }

    [Test]
    public void TestDefaultArguments()
    {
        var methodsWithDefaultValues = new MethodsWithDefaultValues();
        methodsWithDefaultValues.DefaultChar();
        methodsWithDefaultValues.DefaultEmptyChar();
        methodsWithDefaultValues.DefaultPointer();
        methodsWithDefaultValues.DefaultVoidStar();
        methodsWithDefaultValues.DefaultValueType();
        methodsWithDefaultValues.DefaultRefTypeAfterOthers();
        methodsWithDefaultValues.DefaultRefTypeBeforeAndAfterOthers(5, new Foo());
        methodsWithDefaultValues.DefaultRefTypeBeforeOthers();
        methodsWithDefaultValues.DefaultValueType();
        methodsWithDefaultValues.DefaultIntAssignedAnEnum();
    }

    [Test]
    public void TestGenerationOfAnotherUnitInSameFile()
    {
        AnotherUnit.FunctionInAnotherUnit();
    }

    [Test]
    public void TestPrivateOverride()
    {
        new HasPrivateOverride().PrivateOverride();
    }

    [Test]
    public void TestQFlags()
    {
        Assert.AreEqual(TestFlag.Flag2, new ComplexType().ReturnsQFlags);
    }

    [Test]
    public void TestCopyCtor()
    {
        Qux q1 = new Qux();
        for (int i = 0; i < q1.Array.Length; i++)
        {
            q1.Array[i] = i;
        }
        Qux q2 = new Qux(q1);
        for (int i = 0; i < q2.Array.Length; i++)
        {
            Assert.AreEqual(q1.Array[i], q2.Array[i]);
        }
    }

    [Test]
    public void TestImplicitCtor()
    {
        Foo foo = new Foo { A = 10 };
        MethodsWithDefaultValues m = foo;
        Assert.AreEqual(foo.A, m.A);
        MethodsWithDefaultValues m1 = 5;
        Assert.AreEqual(5, m1.A);
    }

    [Test]
    public void TestStructWithPrivateFields()
    {
        var structWithPrivateFields = new StructWithPrivateFields(10, new Foo { A = 5 });
        Assert.AreEqual(10, structWithPrivateFields.SimplePrivateField);
        Assert.AreEqual(5, structWithPrivateFields.ComplexPrivateField.A);
    }

    [Test]
    public void TestRenamingVariable()
    {
        Assert.AreEqual(5, Foo.Rename);
    }
}