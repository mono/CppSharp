using System;
using System.Reflection;
using CppSharp.Utils;
using CSharpTemp;
using NUnit.Framework;

public class CSharpTempTests : GeneratorTestFixture
{
    public class ExtendsWrapper : TestOverrideFromSecondaryBase
    {
        public ExtendsWrapper()
        {
            ProtectedFunction();
        }
    }

    [Test]
    public void TestUncompilableCode()
    {
        new ForceCreationOfInterface().Dispose();
        new InheritsProtectedVirtualFromSecondaryBase().Dispose();
        new InheritanceBuffer().Dispose();
        new HasProtectedVirtual().Dispose();
        new Proprietor(5).Dispose();
        using (var testOverrideFromSecondaryBase = new TestOverrideFromSecondaryBase())
        {
            testOverrideFromSecondaryBase.function();
            var ok = false;
            testOverrideFromSecondaryBase.function(ref ok);
            var property = testOverrideFromSecondaryBase.property;
            testOverrideFromSecondaryBase.VirtualMember();
        }
        using (var foo = new Foo())
        {
            var isNoParams = foo.IsNoParams;
            foo.SetNoParams();
        }
        using (var hasOverride = new HasOverrideOfHasPropertyWithDerivedType())
            hasOverride.CauseRenamingError();
        // TODO: remove when the bug in question is fixed
        if (Type.GetType("Mono.Runtime") != null)
        {
            Assert.Fail("Crashes with Mono because of a problem with marshalling arrays: https://gist.github.com/tritao/7e62c71ffe57d6bc326e");
        }
        else
        {
            using (var qux = new Qux())
            {
                new Bar(qux).Dispose();
            }
        }
        using (ComplexType complexType = TestFlag.Flag1)
        {
        }
        using (var typeMappedWithOperator = new TypeMappedWithOperator())
        {
            int i = typeMappedWithOperator | 5;
        }
    }

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
        // TODO: remove when the bug in question is fixed
        if (Type.GetType("Mono.Runtime") != null)
        {
            Assert.Fail("Crashes with Mono because of a problem with marshalling arrays: https://gist.github.com/tritao/7e62c71ffe57d6bc326e");
        }
        using (var baz = new Baz())
        {
            Assert.That(baz.Method, Is.EqualTo(1));
            var bar = (IBar) baz;
            Assert.That(bar.Method, Is.EqualTo(2));
            Assert.That(baz[0], Is.EqualTo(50));
            bar[0] = new Foo { A = 1000 };
            Assert.That(bar[0].A, Is.EqualTo(1000));
            Assert.That(baz.FarAwayFunc, Is.EqualTo(20));
            Assert.That(baz.TakesQux(baz), Is.EqualTo(20));
            Assert.That(baz.ReturnQux().FarAwayFunc, Is.EqualTo(20));
            baz.SetMethod(1);
            Assert.AreEqual(5, baz.P);
        }
    }

    [Test]
    public void TestProperties()
    {
        var proprietor = new Proprietor();
        Assert.That(proprietor.Parent, Is.EqualTo(0));
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
        methodsWithDefaultValues.DefaultPointer();
        methodsWithDefaultValues.DefaultVoidStar();
        methodsWithDefaultValues.DefaultValueType();
        methodsWithDefaultValues.DefaultChar();
        methodsWithDefaultValues.DefaultEmptyChar();
        methodsWithDefaultValues.DefaultRefTypeBeforeOthers();
        methodsWithDefaultValues.DefaultRefTypeAfterOthers();
        methodsWithDefaultValues.DefaultRefTypeBeforeAndAfterOthers(0, null);
        methodsWithDefaultValues.DefaultIntAssignedAnEnum();
        methodsWithDefaultValues.defaultRefAssignedValue();
        methodsWithDefaultValues.DefaultRefAssignedValue();
        methodsWithDefaultValues.DefaultEnumAssignedBitwiseOr();
        methodsWithDefaultValues.DefaultEnumAssignedBitwiseOrShort();
        methodsWithDefaultValues.DefaultNonEmptyCtor();
        methodsWithDefaultValues.DefaultMappedToEnum();
        methodsWithDefaultValues.DefaultMappedToZeroEnum();
        methodsWithDefaultValues.DefaultMappedToEnumAssignedWithCtor();
        methodsWithDefaultValues.DefaultImplicitCtorInt();
        methodsWithDefaultValues.DefaultImplicitCtorChar();
        methodsWithDefaultValues.DefaultImplicitCtorFoo();
        methodsWithDefaultValues.DefaultIntWithLongExpression();
        methodsWithDefaultValues.DefaultRefTypeEnumImplicitCtor();
        methodsWithDefaultValues.Rotate4x4Matrix(0, 0, 0);
        methodsWithDefaultValues.DefaultPointerToValueType();
        methodsWithDefaultValues.DefaultDoubleWithoutF();
        methodsWithDefaultValues.DefaultIntExpressionWithEnum();
        methodsWithDefaultValues.DefaultCtorWithMoreThanOneArg();
    }

    [Test]
    public void TestGenerationOfAnotherUnitInSameFile()
    {
        AnotherUnit.FunctionInAnotherUnit();
    }

    [Test]
    public void TestPrivateOverride()
    {
        using (var hasOverridesWithChangedAccess = new HasOverridesWithChangedAccess())
            hasOverridesWithChangedAccess.PrivateOverride();
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

    [Test, Ignore]
    public void TestPrimarySecondaryBase()
    {
        var a = new MI_A0();
        var resa = a.Get();
        Assert.That(resa, Is.EqualTo(50));

        var c = new MI_C();
        var res = c.Get();
        Assert.That(res, Is.EqualTo(50));
    }

    [Test]
    public void TestInnerClasses()
    {
        QMap.Iterator test_iter;
    }

    [Test]
    public void TestNativeToManagedMapWithForeignObjects()
    {
        IntPtr native1;
        IntPtr native2;
        using (var testNativeToManagedMap = new TestNativeToManagedMap())
        {
            var hasVirtualDtor2 = testNativeToManagedMap.HasVirtualDtor2;
            native2 = hasVirtualDtor2.__Instance;
            native1 = hasVirtualDtor2.HasVirtualDtor1.__Instance;
            Assert.IsTrue(HasVirtualDtor2.NativeToManagedMap.ContainsKey(native2));
            Assert.IsTrue(HasVirtualDtor1.NativeToManagedMap.ContainsKey(native1));
            Assert.AreSame(hasVirtualDtor2, testNativeToManagedMap.HasVirtualDtor2);
        }
        Assert.IsFalse(HasVirtualDtor2.NativeToManagedMap.ContainsKey(native2));
        Assert.IsFalse(HasVirtualDtor1.NativeToManagedMap.ContainsKey(native1));
    }

    [Test]
    public void TestNativeToManagedMapWithOwnObjects()
    {
        using (var testNativeToManagedMap = new TestNativeToManagedMap())
        {
            var bar = new Bar();
            testNativeToManagedMap.PropertyWithNoVirtualDtor = bar;
            Assert.AreSame(bar, testNativeToManagedMap.PropertyWithNoVirtualDtor);
            Assert.IsTrue(Bar.NativeToManagedMap.ContainsKey(bar.__Instance));
            bar.Dispose();
            Assert.IsFalse(Bar.NativeToManagedMap.ContainsKey(bar.__Instance));
        }
    }

    [Test]
    public void TestNotDestroyingForeignObjects()
    {
        using (var testNativeToManagedMap = new TestNativeToManagedMap())
        {
            var hasVirtualDtor2 = testNativeToManagedMap.HasVirtualDtor2;
            hasVirtualDtor2.Dispose();
            using (var hasVirtualDtor1 = hasVirtualDtor2.HasVirtualDtor1)
            {
                Assert.AreEqual(5, hasVirtualDtor1.TestField);
            }
        }
    }

    [Test]
    public void TestParamTypeToInterfacePass()
    {
        var baseClass = new TestParamToInterfacePassBaseTwo();
        baseClass++;
        Assert.AreEqual(baseClass.M, 1);
        ITestParamToInterfacePassBaseTwo baseInterface = new TestParamToInterfacePassBaseTwo();
        var dervClass = new TestParamToInterfacePass();
        dervClass.AddM(baseClass);
        Assert.AreEqual(dervClass.M, 1);
        dervClass = new TestParamToInterfacePass(dervClass + baseClass);
        Assert.AreEqual(dervClass.M, 2);
        dervClass = new TestParamToInterfacePass(dervClass + baseInterface);
        Assert.AreEqual(dervClass.M, 2);
    }

    [Test]
    public void TestNullAttributedFunctionPtr()
    {
        using (var foo = new Foo())
        {
            foo.AttributedFunctionPtr = null;
        }
    }

    [Test]
    public unsafe void TestMultiOverLoadPtrToRef()
    {
        var r = 0;
        MultiOverloadPtrToRef m = &r;
        m.Dispose();
        var obj = new MultiOverloadPtrToRef(ref r);
        var p = obj.ReturnPrimTypePtr();
        Assert.AreEqual(0, p[0]);
        Assert.AreEqual(0, p[1]);
        Assert.AreEqual(0, p[2]);

        obj.TakePrimTypePtr(ref *p);
        Assert.AreEqual(100, p[0]);
        Assert.AreEqual(200, p[1]);
        Assert.AreEqual(300, p[2]);
            
        int[] array = { 1, 2, 3 };
        fixed (int* p1 = array)
        {
            obj.TakePrimTypePtr(ref *p1);
            Assert.AreEqual(100, p1[0]);
            Assert.AreEqual(200, p1[1]);
            Assert.AreEqual(300, p1[2]);
        }

        Assert.AreEqual(100, array[0]);
        Assert.AreEqual(200, array[1]);
        Assert.AreEqual(300, array[2]);

        float pThree = 0;
        var refInt = 0;
        obj.FuncPrimitivePtrToRef(ref refInt, null, ref pThree);
        obj.FuncPrimitivePtrToRefWithDefVal(ref refInt, null, null, ref refInt);
        obj.FuncPrimitivePtrToRefWithMultiOverload(ref refInt, null, null, ref refInt);
    }
}
