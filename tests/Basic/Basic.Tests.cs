using System;
using CppSharp.Utils;
using NUnit.Framework;
using Basic;
using Enum = Basic.Enum;

public class BasicTests : GeneratorTestFixture
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
        Assert.That(hello.AddFooPtr(foo), Is.EqualTo(11));
        Assert.That(hello.AddFooRef(foo), Is.EqualTo(11));
        unsafe
        {
            var pointer = foo.SomePointer;
            var pointerPointer = foo.SomePointerPointer;
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, pointer[i]);
                Assert.AreEqual(i, (*pointerPointer)[i]);
            }
        }

        var bar = new Bar { A = 4, B = 7 };
        Assert.That(hello.AddBar(bar), Is.EqualTo(11));
        Assert.That(bar.RetItem1(), Is.EqualTo(Bar.Item.Item1));

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
        //Assert.That(hello.RetEnum(Enum.D), Is.EqualTo(-2147483648));
        Assert.That(hello.RetEnum(Enum.E), Is.EqualTo(1));
        Assert.That(hello.RetEnum(Enum.F), Is.EqualTo(-9));
    }

    [Test]
    public void TestPrimitiveOutParameters()
    {
        var hello = new Hello();

        float f;
        Assert.That(hello.TestPrimitiveOut(out f), Is.True);
        Assert.That(f, Is.EqualTo(10.0f));
    }

    [Test]
    public void TestPrimitiveOutRefParameters()
    {
        var hello = new Hello();

        float f;
        Assert.That(hello.TestPrimitiveOutRef(out f), Is.True);
        Assert.That(f, Is.EqualTo(10.0f));
    }

    [Test]
    public void TestNullRef()
    {
        var hello = new Hello ();
        Assert.That(hello.RetNull(), Is.Null);
    }

    [Test]
    public void TestAmbiguous()
    {
        var def = new DefaultParameters();
        def.Foo(1, 2);
        def.Bar();
    }

    [Test]
    public void TestLeftShiftOperator()
    {
        var foo2 = new Foo2 {C = 2};
        Foo2 result = foo2 << 3;
        Assert.That(result.C, Is.EqualTo(16));
    }

    [Test]
    public void TestAbstractReturnType()
    {
        var returnsAbstractFoo = new ReturnsAbstractFoo();
        var abstractFoo = returnsAbstractFoo.Foo;
        Assert.AreEqual(abstractFoo.pureFunction(1), 5);
        Assert.AreEqual(abstractFoo.pureFunction1(), 10);
        Assert.AreEqual(abstractFoo.pureFunction2(), 15);
    }

    [Test]
    public void TestANSI()
    {
        var foo = new Foo();
        Assert.That(foo.ANSI, Is.EqualTo("ANSI"));
    }

    [Test]
    public void TestMoveOperatorToClass()
    {
        // Unary operator
        var unary = new TestMoveOperatorToClass() { A = 4, B = 7 };
        var unaryMinus = -unary;

        Assert.That(unaryMinus.A, Is.EqualTo(-unary.A));
        Assert.That(unaryMinus.B, Is.EqualTo(-unary.B));

        // Binary operator
        var bin = new TestMoveOperatorToClass { A = 4, B = 7 };
        var bin1 = new TestMoveOperatorToClass { A = 5, B = 10 };
        var binSum = bin + bin1;

        Assert.That(binSum.A, Is.EqualTo(bin.A + bin1.A));
        Assert.That(binSum.B, Is.EqualTo(bin.B + bin1.B));

        // Multiple argument operator
        var multiArg = new TestMoveOperatorToClass { A = 4, B = 7 };
        var multiArgStar = multiArg * 2; 
        Assert.That(multiArgStar, Is.EqualTo(8));
    }

    [Test]
    public void TestMoveFunctionToClass()
    {
        Assert.That(basic.test(new basic()), Is.EqualTo(5));
    }

    [Test]
    public void TestMethodWithFixedInstance()
    {
        var bar = new Bar2 { A = 1, B = 2, C = 3 };
        Foo2 foo = bar.needFixedInstance();
        Assert.AreEqual(foo.A, 1);
        Assert.AreEqual(foo.B, 2);
        Assert.AreEqual(foo.C, 3);
    }

    [Test]
    public void TestConversionOperator()
    {
        var bar = new Bar2 {A = 1, B = 2, C = 3};
        Foo2 foo = bar;
        Assert.AreEqual(foo.A, 1);
        Assert.AreEqual(foo.B, 2);
        Assert.AreEqual(foo.C, 3);

        Assert.AreEqual(300, new Bar2.Nested());
        Assert.AreEqual(500, new Bar2());
    }

    [Test]
    public void TestDelegates()
    {
        var delegates = new TestDelegates();
        var doubleSum = delegates.A(2) + delegates.B(2);
        Assert.AreEqual(8, doubleSum);
    }

    [Test]
    public void TestAttributedDelegate()
    {
        var result = basic.AttributedDelegate(2);
        Assert.AreEqual(4, result);
    }

    [Test]
    public void TestUnion()
    {
        Hello.NestedPublic nestedPublic = new Hello.NestedPublic();
        nestedPublic.j = 5;
        Assert.That(nestedPublic.l, Is.EqualTo(5));
        Assert.That(nestedPublic.g, Is.Not.EqualTo(0));
    }

    [Test]
    public void TestPropertyChains()
    {
        var bar2 = new Bar2();
        bar2.pointerToStruct.A = 15;
        Assert.That(bar2.pointerToStruct.A, Is.EqualTo(15));
    }

    [Test]
    public void TestStaticClasses()
    {
        Assert.That(TestStaticClass.Add(1, 2), Is.EqualTo(3));
    }

    [Test, Ignore]
    public void TestChar16()
    {
    }

    [Test]
    public void TestCopyConstructor()
    {
        Foo foo = new Foo { A = 5, B = 5.5f };
        var copyFoo = new Foo(foo);
        Assert.That(foo.A, Is.EqualTo(copyFoo.A));
        Assert.That(foo.B, Is.EqualTo(copyFoo.B));

        var testCopyConstructorRef = new TestCopyConstructorRef { A = 10, B = 5 };
        var copyBar = new TestCopyConstructorRef(testCopyConstructorRef);
        Assert.That(testCopyConstructorRef.A, Is.EqualTo(copyBar.A));
        Assert.That(testCopyConstructorRef.B, Is.EqualTo(copyBar.B));
    }

    [Test]
    public void TestCharMarshalling()
    {
        Foo2 foo2 = new Foo2();
        for (char c = char.MinValue; c <= sbyte.MaxValue; c++)
            Assert.That(foo2.testCharMarshalling(c), Is.EqualTo(c));
        Assert.Catch<ArgumentException>(() => foo2.testCharMarshalling('ж'));
    }

    [Test]
    public unsafe void TestIndexers()
    {
        var someStruct = new SomeStruct();
        Assert.That(someStruct[0], Is.EqualTo(1));
        Assert.That(someStruct["foo"], Is.EqualTo(1));
        someStruct[0] = 2;
        Assert.That(someStruct[0], Is.EqualTo(2));
    }

    [Test]
    public unsafe void TestOperators()
    {
        var @class = new ClassWithOverloadedOperators();
        Assert.AreEqual(1, (char) @class);
        Assert.AreEqual(2, (int)@class);
        Assert.AreEqual(3, (short)@class);
    }

    [Test]
    public void TestFunctions()
    {
        var ret = Basic.basic.Function();
        Assert.That(ret, Is.EqualTo(5));
    }

    [Test]
    public void TestProperties()
    {
        // Test field property
        var prop = new TestProperties();
        Assert.That(prop.Field, Is.EqualTo(0));
        prop.Field = 10;
        Assert.That(prop.Field, Is.EqualTo(10));

        // Test getter/setter property
        prop.Field = 20;
        Assert.That(prop.FieldValue, Is.EqualTo(20));
        prop.FieldValue = 10;
        Assert.That(prop.FieldValue, Is.EqualTo(10));
    }

    [Test]
    public unsafe void TestArraysPointers()
    {
        var values = MyEnum.A;
        var arrays = new TestArraysPointers(&values, 1);
        Assert.That(arrays.Value, Is.EqualTo(MyEnum.A));
    }

    [Test]
    public unsafe void TestGetterSetterToProperties()
    {
        var @class = new TestGetterSetterToProperties();
        Assert.That(@class.Width, Is.EqualTo(640));
        Assert.That(@class.Height, Is.EqualTo(480));
    }

    [Test]
    public unsafe void TestSingleArgumentCtorToCastOperator()
    {
        var classA = new ClassA(10);
        ClassB classB = classA;
        Assert.AreEqual(classA.Value, classB.Value);
        ClassC classC = (ClassC)classB;
        Assert.AreEqual(classB.Value, classC.Value);
    }
}
 