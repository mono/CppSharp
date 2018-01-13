﻿using System;
using System.Reflection;
using CommonTest;
using CppSharp.Utils;
using NUnit.Framework;
using Enum = CommonTest.Enum;

[TestFixture]
public class CommonTests : GeneratorTestFixture
{
    [Test]
    public void TestCodeGeneration()
    {
#pragma warning disable 0168 // warning CS0168: The variable `foo' is declared but never used
#pragma warning disable 0219 // warning CS0219: The variable `foo' is assigned but its value is never used

        using (var changedAccessOfInheritedProperty = new ChangedAccessOfInheritedProperty())
        {
            Assert.That(changedAccessOfInheritedProperty.Property, Is.EqualTo(2));
        }
        Foo.NestedAbstract a;
        var renamedEmptyEnum = Foo.RenamedEmptyEnum.EmptyEnum1;
        using (var foo = new Foo())
        {
            Bar bar = foo;
            Assert.IsTrue(Bar.Item.Item1 == bar);
        }
        using (var overridesNonDirectVirtual = new OverridesNonDirectVirtual())
        {
            using (var foo = new Foo())
            {
                Assert.That(overridesNonDirectVirtual.RetInt(foo), Is.EqualTo(3));
                Assert.That(foo.FooPtr, Is.EqualTo(1));
            }
        }
        using (var derivedFromTemplateInstantiationWithVirtual = new DerivedFromTemplateInstantiationWithVirtual())
        {
        }
        using (var hasProtectedEnum = new HasProtectedEnum())
        {
        }
        EnumWithUnderscores e = EnumWithUnderscores.lOWER_BEFORE_CAPITAL;
        e = EnumWithUnderscores.UnderscoreAtEnd;
        e = EnumWithUnderscores.CAPITALS_More;
        e = EnumWithUnderscores.UsesDigits1_0;
        e.GetHashCode();
        Common.SMallFollowedByCapital();
        using (new DerivedFromSecondaryBaseWithIgnoredVirtualMethod()) { }

#pragma warning restore 0168
#pragma warning restore 0219
    }

    [Test]
    public void TestHello()
    {
        var hello = new Hello();
        hello.PrintHello("Hello world");

        Assert.That(hello.Add(1, 1), Is.EqualTo(2));
        Assert.That(hello.Add(5, 5), Is.EqualTo(10));

        Assert.IsTrue(hello.Test1(3, 3.0f));
        Assert.IsFalse(hello.Test1(2, 3.0f));

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
    public void TestPrimitiveConstCharStringInOut()
    {
        var hello = new Hello();

        string str;
        hello.StringOut(out str);
        Assert.That(str, Is.EqualTo("HelloStringOut"));
        hello.StringOutRef(out str);
        Assert.That(str, Is.EqualTo("HelloStringOutRef"));
        str = "Hello";
        hello.StringInOut(ref str);
        Assert.That(str, Is.EqualTo("StringInOut"));
        str = "Hello";
        hello.StringInOutRef(ref str);
        Assert.That(str, Is.EqualTo("StringInOutRef"));
        hello.StringTypedef(str);
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

    public void TestPrimitiveInOutParameters()
    {
        var hello = new Hello();

        int i = 10;
        Assert.That(hello.TestPrimitiveInOut(ref i), Is.True);
        Assert.That(i, Is.EqualTo(20));
    }

    [Test]
    public void TestPrimitiveInOutRefParameters()
    {
        var hello = new Hello();

        int i = 10;
        Assert.That(hello.TestPrimitiveInOutRef(ref i), Is.True);
        Assert.That(i, Is.EqualTo(20));
    }

    [Test]
    public void TestEnumOut()
    {
        var hello = new Hello();

        Enum e;
        hello.EnumOut((int) Enum.C, out e);
        Assert.That(e, Is.EqualTo(Enum.C));
    }

    [Test]
    public void TestEnumOutRef()
    {
        var hello = new Hello();

        Enum e;
        hello.EnumOutRef((int) Enum.C, out e);
        Assert.That(e, Is.EqualTo(Enum.C));
    }

    [Test]
    public void TestEnumInOut()
    {
        var hello = new Hello();

        var e = Enum.E;
        hello.EnumInOut(ref e);
        Assert.That(e, Is.EqualTo(Enum.F));
    }

    [Test]
    public void TestEnumInOutRef()
    {
        var hello = new Hello();

        var e = Enum.E;
        hello.EnumInOut(ref e);
        Assert.That(e, Is.EqualTo(Enum.F));
    }

    [Test]
    public void TestNullRef()
    {
        var hello = new Hello();
        Assert.That(hello.RetNull(), Is.Null);
    }

    [Test]
    public void TestAmbiguous()
    {
        var def = new DefaultParameters();
        def.Foo(1, 2);
        def.Bar();
        using (Foo foo = new Foo())
        {
            Common.HasPointerParam(foo, 0);
            Common.HasPointerParam(foo);
        }
    }

    [Test]
    public void TestLeftShiftOperator()
    {
        var foo2 = new Foo2 { C = 2 };
        Foo2 result = foo2 << 3;
        foo2.TestKeywordParam(IntPtr.Zero, Bar.Item.Item1, 1);
        Assert.That(result.C, Is.EqualTo(16));
    }

    [Test]
    public void TestAbstractReturnType()
    {
        var returnsAbstractFoo = new ReturnsAbstractFoo();
        var abstractFoo = returnsAbstractFoo.Foo;
        Assert.AreEqual(abstractFoo.PureFunction(1), 5);
        Assert.AreEqual(abstractFoo.PureFunction1, 10);
        var ok = false;
        Assert.AreEqual(abstractFoo.PureFunction2(ref ok), 15);
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
        using (var common = new Common())
            Assert.That(Common.Test(common), Is.EqualTo(5));
    }

    [Test]
    public void TestMethodWithFixedInstance()
    {
        var bar = new Bar2 { A = 1, B = 2, C = 3 };
        Foo2 foo = bar.NeedFixedInstance;
        Assert.AreEqual(foo.A, 1);
        Assert.AreEqual(foo.B, 2);
        Assert.AreEqual(foo.C, 3);
    }

    [Test]
    public void TestConversionOperator()
    {
        var bar = new Bar2 { A = 1, B = 2, C = 3 };
        using (Foo2 foo = bar)
        {
            Assert.That(1, Is.EqualTo(foo.A));
            Assert.That(2, Is.EqualTo(foo.B));
            Assert.That(3, Is.EqualTo(foo.C));
        }
        using (var bar2Nested = new Bar2.Nested())
        {
            int bar2NestedInt = bar2Nested;
            Assert.That(bar2NestedInt, Is.EqualTo(300));
        }

        int bar2Int = new Bar2();
        Assert.That(bar2Int, Is.EqualTo(500));
    }

    [Test]
    public void TestDelegates()
    {
        var delegates = new TestDelegates();
        var doubleSum = delegates.A(2) + delegates.B(2);
        Assert.AreEqual(8, doubleSum);

        var stdcall = delegates.StdCall(i => i);
        Assert.AreEqual(1, stdcall);

        var cdecl = delegates.CDecl(i => i);
        Assert.AreEqual(1, cdecl);

        var emptydelegeate = delegates.MarshalNullDelegate;
        Assert.AreEqual(emptydelegeate, null);
    }

    [Test]
    public void TestUnion()
    {
        Hello.NestedPublic nestedPublic = new Hello.NestedPublic();
        nestedPublic.J = 5;
        Assert.That(nestedPublic.L, Is.EqualTo(5));
        Assert.That(nestedPublic.G, Is.Not.EqualTo(0));
    }

    [Test]
    public void TestPropertyChains()
    {
        var bar2 = new Bar2();
        bar2.PointerToStruct = new Bar { A = 15 };
        Assert.That(bar2.PointerToStruct.A, Is.EqualTo(15));
    }

    [Test]
    public void TestStaticClasses()
    {
        Type staticClassType = typeof(TestStaticClass);
        // Only static class can be both abstract and sealed
        Assert.IsTrue(staticClassType.IsAbstract && staticClassType.IsSealed);
        Assert.That(TestStaticClass.Add(1, 2), Is.EqualTo(3));
        Assert.That(TestStaticClass.OneTwoThree, Is.EqualTo(123));
        Assert.That(TestStaticClassDerived.Foo, Is.EqualTo(0));
        TestNotStaticClass.StaticFunction.GetHashCode();
    }

    [Test]
    public void TestCopyConstructor()
    {
        using (Foo foo = new Foo { A = 5, B = 5.5f })
        {
            using (var copyFoo = new Foo(foo))
            {
                Assert.That(foo.A, Is.EqualTo(copyFoo.A));
                Assert.That(foo.B, Is.EqualTo(copyFoo.B));
            }
        }

        using (var testCopyConstructorRef = new TestCopyConstructorRef { A = 10, B = 5 })
        {
            using (var copyBar = new TestCopyConstructorRef(testCopyConstructorRef))
            {
                Assert.That(testCopyConstructorRef.A, Is.EqualTo(copyBar.A));
                Assert.That(testCopyConstructorRef.B, Is.EqualTo(copyBar.B));
            }
        }

        using (var original = new HasCopyAndMoveConstructor(5))
        {
            using (var copy = new HasCopyAndMoveConstructor(original))
            {
                Assert.That(copy.Field, Is.EqualTo(original.Field));
            }
        }
    }

    [Test]
    public void TestCharMarshalling()
    {
        Foo2 foo2 = new Foo2();
        for (char c = char.MinValue; c <= sbyte.MaxValue; c++)
            Assert.That(foo2.TestCharMarshalling(c), Is.EqualTo(c));
        Assert.Catch<OverflowException>(() => foo2.TestCharMarshalling('ж'));
    }

    [Test]
    public void TestIndexers()
    {
        using (var indexedProperties = new TestIndexedProperties())
        {
            Assert.AreEqual(1, indexedProperties[0]);
            Assert.AreEqual(1, indexedProperties["foo"]);
            indexedProperties[0] = 2;
            Assert.AreEqual(2, indexedProperties[0]);
            indexedProperties[0f] = 3;
            Assert.AreEqual(3, indexedProperties[0f]);
            var properties = indexedProperties[(byte) 0];
            Assert.AreEqual(0, properties.Field);
            var newProperties = new TestProperties();
            newProperties.Field = 4;
            indexedProperties[(byte) 0] = newProperties;
            Assert.AreEqual(4, indexedProperties[(byte) 0].Field);
            newProperties = indexedProperties[(short) 0];
            Assert.AreEqual(4, newProperties.Field);
            newProperties.Field = 5;
            Assert.AreEqual(5, indexedProperties[(byte) 0].Field);

            var bar = new Bar { A = 5 };
            indexedProperties[0u] = bar;
            Assert.That(bar.A, Is.EqualTo(indexedProperties[0u].A));
            indexedProperties[(ushort) 0] = bar;
            Assert.That(bar.A, Is.EqualTo(indexedProperties[(ushort) 0].A));

            using (var foo = new Foo())
            {
                var bar2 = new Bar { A = 10 };
                indexedProperties[foo] = bar2;
                Assert.That(bar2.A, Is.EqualTo(indexedProperties[foo].A));
            }
        }
    }

    [Test]
    public void TestOperators()
    {
        using (var @class = new ClassWithOverloadedOperators())
        {
            char @char = @class;
            Assert.That(@char, Is.EqualTo(1));
            short @short = @class;
            Assert.That(@short, Is.EqualTo(3));
        }
        using (var @class = new ClassWithOverloadedOperators())
        {
            int classInt = @class;
            Assert.That(classInt, Is.EqualTo(2));
        }
    }

    [Test]
    public void TestFunctions()
    {
        var ret = Common.Function;
        Assert.That(ret, Is.EqualTo(5));

        Common.FuncWithTypeAlias(0);
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

        prop.GetterAndSetterWithTheSameName = 25;
        Assert.That(prop.GetterAndSetterWithTheSameName, Is.EqualTo(25));

        prop.SetterReturnsBoolean = 35;
        Assert.That(prop.SetterReturnsBoolean, Is.EqualTo(35));

        prop.VirtualSetterReturnsBoolean = 45;
        Assert.That(prop.VirtualSetterReturnsBoolean, Is.EqualTo(45));
    }

    [Test]
    public void TestVariable()
    {
        // Test field property
        var @var = new TestVariables();
        @var.SetValue(10);
        Assert.That(TestVariables.VALUE, Is.EqualTo(10));
    }

    [Test]
    public void TestWideStrings()
    {
        var ws = new TestWideStrings();
        var s = ws.WidePointer;
        Assert.That(ws.WidePointer, Is.EqualTo("Hello"));
        Assert.That(ws.WideNullPointer, Is.EqualTo(null));
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
        ClassC classC = (ClassC) classB;
        Assert.AreEqual(classB.Value, classC.Value);
    }

    [Test]
    public unsafe void TestFieldRef()
    {
        var classD = new ClassD(10);
        var fieldRef = classD.Field;
        fieldRef.Value = 20;
        Assert.AreEqual(20, classD.Field.Value);
    }

    [Test]
    public unsafe void TestDecltype()
    {
        var ret = Common.TestDecltype;
        Assert.AreEqual(0, ret);
    }

    [Test]
    public unsafe void TestNullPtrType()
    {
        var ret = Common.TestNullPtrTypeRet;
        Assert.AreEqual(IntPtr.Zero, new IntPtr(ret));
    }

    [Test]
    public void TestCtorByValue()
    {
        var bar = new Bar { A = 4, B = 5.5f };
        var foo2 = new Foo2 { C = 4, ValueTypeField = bar };
        var result = foo2 << 2;
        Assert.AreEqual(foo2.C << 2, result.C);
        Assert.AreEqual(bar.A << 2, result.ValueTypeField.A);
        Assert.AreEqual(bar.B, result.ValueTypeField.B);
    }

    [Test]
    public void TestMarshalUnattributedDelegate()
    {
        new TestDelegates().MarshalUnattributedDelegate(i => i);
    }

    [Test]
    public void TestPassAnonymousDelegate()
    {
        var testDelegates = new TestDelegates();
        int value = testDelegates.MarshalAnonymousDelegate(i => i * 2);
        Assert.AreEqual(2, value);
        int value5 = testDelegates.MarshalAnonymousDelegate5(i => i * 2);
        Assert.AreEqual(4, value5);
        int value6 = testDelegates.MarshalAnonymousDelegate6(i => i * 2);
        Assert.AreEqual(6, value6);
    }

    [Test]
    public void TestGetAnonymousDelegate()
    {
        var testDelegates = new TestDelegates();
        var @delegate = testDelegates.MarshalAnonymousDelegate4;
        int value = @delegate.Invoke(1);
        Assert.AreEqual(2, value);
    }

    [Test]
    public void TestFixedArrays()
    {
        var foo = new Foo();
        var array = new[] { 1, 2, 3 };
        foo.FixedArray = array;
        for (int i = 0; i < foo.FixedArray.Length; i++)
            Assert.That(array[i], Is.EqualTo(foo.FixedArray[i]));
    }

    [Test]
    public void TestInternalCtorAmbiguity()
    {
        new InvokesInternalCtorAmbiguity().InvokeInternalCtor();
    }

    [Test]
    public void TestEqualityOperator()
    {
#pragma warning disable 1718 // Comparison made to same variable; did you mean to compare something else?

        using (var foo = new Foo { A = 5, B = 5.5f })
        {
            Assert.IsTrue(foo == foo);
            using (var notEqual = new Foo { A = 5, B = 5.6f })
            {
                Assert.IsTrue(notEqual != foo);
            }
            Assert.IsTrue(foo != null);
        }
        var bar = new Bar { A = 5, B = 5.5f };
        Assert.IsTrue(bar == bar);
        Assert.IsFalse(new Bar { A = 5, B = 5.6f } == bar);
        using (var differentConstOverloads = new DifferentConstOverloads())
        {
            Assert.IsTrue(differentConstOverloads != null);
        }

#pragma warning restore 1718
    }

    [Test]
    public void TestFriendOperator()
    {
        HasFriend h1 = 5;
        HasFriend h2 = 10;
        Assert.AreEqual(15, (h1 + h2).M);
        Assert.AreEqual(-5, (h1 - h2).M);
    }

    [Test]
    public void TestOperatorOverloads()
    {
        var differentConstOverloads = new DifferentConstOverloads();
        Assert.IsTrue(differentConstOverloads == new DifferentConstOverloads());
        Assert.IsFalse(differentConstOverloads == 5);
    }

    [Test]
    public void TestPacking()
    {
        foreach (var testPacking in new TestPacking[] { new TestPacking1(), new TestPacking2(),
            new TestPacking4(), new TestPacking8() })
        {
            testPacking.I1 = 2;
            testPacking.I2 = 4;
            testPacking.B = true;

            Assert.That(testPacking.I1, Is.EqualTo(2));
            Assert.That(testPacking.I2, Is.EqualTo(4));
            Assert.That(testPacking.B, Is.EqualTo(true));
            testPacking.Dispose();
        }
    }

    [Test]
    public void TestRenamingVariableNamedAfterKeyword()
    {
        Assert.AreEqual(10, Foo.Unsafe);
    }

    [Test]
    public void TestMarshallingEmptyType()
    {
        var empty = new ReturnsEmpty().Empty;
    }

    [Test]
    public void TestOutTypeClassesPassTry()
    {
        RefTypeClassPassTry refTypeClassPassTry;
        Common.FuncTryRefTypeOut(out refTypeClassPassTry);
        Common.FuncTryRefTypePtrOut(out refTypeClassPassTry);

        ValueTypeClassPassTry valueTypeClassPassTry;
        Common.FuncTryValTypeOut(out valueTypeClassPassTry);
        Common.FuncTryValTypePtrOut(out valueTypeClassPassTry);
    }

    [Test]
    public void TestVirtualReturningClassWithCharField()
    {
        using (var hasVirtualReturningHasProblematicFields = new HasVirtualReturningHasProblematicFields())
        {
            var hasProblematicFields = hasVirtualReturningHasProblematicFields.ReturnsProblematicFields;
            Assert.That(hasProblematicFields.B, Is.EqualTo(false));
            hasProblematicFields.B = true;
            Assert.That(hasProblematicFields.B, Is.EqualTo(true));
            Assert.That(hasProblematicFields.C, Is.EqualTo(char.MinValue));
            hasProblematicFields.C = 'a';
            Assert.That(hasProblematicFields.C, Is.EqualTo('a'));
        }
    }

    [Test]
    public void TestDisposingCustomDerivedFromVirtual()
    {
        using (new CustomDerivedFromVirtual())
        {
        }
    }

    [Test]
    public void TestIncompleteCharArray()
    {
        Assert.That(Foo.CharArray, Is.EqualTo("abc"));
    }

    [Test]
    public void TestPassingNullToRef()
    {
        using (var foo = new Foo())
        {
            Assert.Catch<ArgumentNullException>(() => foo.TakesRef(null));
        }
    }

    [Test]
    public void TestNonTrivialDtorInvocation()
    {
        using (var nonTrivialDtor = new NonTrivialDtor())
        {
        }
        Assert.IsTrue(NonTrivialDtor.dtorCalled);
    }

    [Test]
    public void TestFixedCharArray()
    {
        using (var foo = new Foo())
        {
            foo.FixedCharArray = new char[] { 'a', 'b', 'c' };
            Assert.That(foo.FixedCharArray[0], Is.EqualTo('a'));
            Assert.That(foo.FixedCharArray[1], Is.EqualTo('b'));
            Assert.That(foo.FixedCharArray[2], Is.EqualTo('c'));
        }
    }

    [Test]
    public void TestStaticFields()
    {
        Assert.That(Foo.ReadWrite, Is.EqualTo(15));
        Foo.ReadWrite = 25;
        Assert.That(Foo.ReadWrite, Is.EqualTo(25));
    }

    [Test]
    public void TestStdString()
    {
        // when C++ memory is deleted, it's only marked as free but not immediadely freed
        // this can hide memory bugs while marshalling
        // so let's use a long string to increase the chance of a crash right away
        const string t = @"This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. 
This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string.
This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string.
This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string.
This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string.
This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string. This is a very long string.";
        using (var hasStdString = new HasStdString())
        {
            Assert.That(hasStdString.TestStdString(t), Is.EqualTo(t + "_test"));
            hasStdString.S = t;
            Assert.That(hasStdString.S, Is.EqualTo(t));
            Assert.That(hasStdString.StdString, Is.EqualTo(t));
            Assert.That(hasStdString.StdString, Is.EqualTo(t));
        }
    }

    [Test, Platform(Exclude = "Win")]
    public void TestNullStdString()
    {
        using (var hasStdString = new HasStdString())
        {
            Assert.That(() => hasStdString.TestStdString(null), Throws.ArgumentNullException);
        }
    }

    private class CustomDerivedFromVirtual : AbstractWithVirtualDtor
    {
        public override void Abstract()
        {
        }
    }

    [Test]
    public void TestFuncWithUnionParam()
    {
        var ut = new UnionT();
        ut.C = 20;
        var v = Common.FuncUnion(ut);
        Assert.AreEqual(20, v);
    }

    [Test]
    public void TestVirtualFuncWithStringParams()
    {
        using (var VirtFuncWithStringParam = new ImplementsVirtualFunctionsWithStringParams())
        {
            VirtFuncWithStringParam.PureVirtualFunctionWithStringParams("anyRandomString");
            Assert.That(VirtFuncWithStringParam.VirtualFunctionWithStringParam("anyRandomString").Equals(5));
        }
    }

    [Test]
    public void TestOverriddenSetter()
    {
        var propertyInfo = typeof(HasOverridenSetter).GetProperty("Virtual",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(propertyInfo, Is.Not.Null);
        Assert.That(propertyInfo.CanWrite);
    }

    [Test]
    public void TestVirtualFunctionWithBoolParams()
    {
        using (var hasVirtualFunctionWithBoolParam = new HasVirtualFunctionWithBoolParams())
            Assert.That(hasVirtualFunctionWithBoolParam.VirtualFunctionWithBoolParamAndReturnsBool(true));
    }

    [Test]
    public void TestRefToPrimitiveInSetter()
    {
        using (var testProperties = new TestProperties())
        {
            const double value = 5.5;
            testProperties.RefToPrimitiveInSetter = value;
            Assert.That(testProperties.RefToPrimitiveInSetter, Is.EqualTo(value));
        }
    }

    [Test]
    public void TestReturnChar16()
    {
        using (var foo = new Foo())
        {
            Assert.That(foo.ReturnChar16(), Is.EqualTo('a'));
        }
    }
}
