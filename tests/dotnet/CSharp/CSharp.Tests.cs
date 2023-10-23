using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CSharp;
using NUnit.Framework;

[TestFixture]
public unsafe class CSharpTests
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
#pragma warning disable 0168 // warning CS0168: The variable `foo' is declared but never used
#pragma warning disable 0219 // warning CS0219: The variable `foo' is assigned but its value is never used

        ALLCAPS_UNDERSCORES a;
        new MultipleInheritance().Dispose();
        using (var testRenaming = new TestRenaming())
        {
            testRenaming.name();
            testRenaming.Name();
            testRenaming.Property.GetHashCode();
        }
        new ForceCreationOfInterface().Dispose();
        new InheritsProtectedVirtualFromSecondaryBase().Dispose();
        new InheritanceBuffer().Dispose();
        new HasProtectedVirtual().Dispose();
        new Proprietor(5).Dispose();
        new HasCtorWithMappedToEnum<TestFlag>(TestFlag.Flag1).Dispose();
        new TestAnonymousMemberNameCollision().Dispose();
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
            foo.Width = 5;
            using (var hasSecondaryBaseWithAbstractWithDefaultArg = new HasSecondaryBaseWithAbstractWithDefaultArg())
            {
                hasSecondaryBaseWithAbstractWithDefaultArg.Abstract();
                hasSecondaryBaseWithAbstractWithDefaultArg.AbstractWithNoDefaultArg(foo);
            }
            Assert.That(foo.PublicFieldMappedToEnum, Is.EqualTo(TestFlag.Flag2));
            Assert.That(foo.ReturnConstRef(), Is.EqualTo(5));
        }
        using (var hasOverride = new HasOverrideOfHasPropertyWithDerivedType())
            hasOverride.CauseRenamingError();
        using (var qux = new Qux())
        {
            qux.Type.GetHashCode();
            using (Bar bar = new Bar(qux))
            {
                bar.Type.GetHashCode();
            }
        }
        using (var quux = new Quux())
        {
            quux.SetterWithDefaultOverload = null;
            quux.SetSetterWithDefaultOverload();
        }
        using (ComplexType complexType = TestFlag.Flag1)
        {
        }
        using (var typeMappedWithOperator = new TypeMappedWithOperator())
        {
            int i = typeMappedWithOperator | 5;
        }
        using (Base<int> @base = new DerivedFromSpecializationOfUnsupportedTemplate())
        {
        }

        CSharp.CSharpCool.FunctionInsideInlineNamespace();
        CSharp.CSharpCool.TakeMappedEnum(TestFlag.Flag1);
        using (CSharpTemplatesCool.SpecialiseReturnOnly())
        {
        }

        using (var t1 = new T1())
        using (new IndependentFields<int>(t1))
        {
        }

        using (var t2 = new T2())
        using (new IndependentFields<int>(t2))
        {
        }

#pragma warning restore 0168
#pragma warning restore 0219
    }

    private class OverriddenInManaged : Baz
    {
        public override int Type => 10;
    }

    [Test]
    public void TestDer()
    {
        using (var der = new OverriddenInManaged())
        {
            using (var hasDer = new HasOverriddenInManaged())
            {
                hasDer.SetOverriddenInManaged(der);
                Assert.That(hasDer.CallOverriddenInManaged(), Is.EqualTo(10));
            }
        }
    }

    [Test]
    [Ignore("https://github.com/mono/CppSharp/issues/1518")]
    public void TestReturnCharPointer()
    {
        Assert.That(new IntPtr(CSharp.CSharpCool.ReturnCharPointer()), Is.EqualTo(IntPtr.Zero));
        const char z = 'z';
        Assert.That(*CSharp.CSharpCool.TakeConstCharRef(z), Is.EqualTo(z));
    }

    [Test]
    public void TestTakeCharPointer()
    {
        char c = 'c';
        Assert.That(*CSharp.CSharpCool.TakeCharPointer(&c), Is.EqualTo(c));
    }

    [Test]
    public void TestIndexer()
    {
        using (var foo = new Foo())
        {
            Assert.That(foo[0], Is.EqualTo(50));
            foo[0] = 250;
            Assert.That(foo[0], Is.EqualTo(250));

            Assert.That(foo[(uint)0], Is.EqualTo(15));
        }

        using (var bar = new Bar())
        {
            Assert.That(bar[0].A, Is.EqualTo(10));
            using (Foo foo = new Foo { A = 25 })
            {
                bar[0] = foo;
                Assert.That(bar[0].A, Is.EqualTo(25));
            }
        }
    }

    [Test]
    public void TestReturnSmallPOD()
    {
        using (var f = new Foo())
        {
            foreach (var pod in new[] { f.SmallPodCdecl, f.SmallPodStdcall, f.SmallPodThiscall })
            {
                Assert.That(pod.A, Is.EqualTo(10000));
                Assert.That(pod.B, Is.EqualTo(40000));
            }
        }
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
        using (var baz = new Baz())
        {
            Assert.That(baz.Method, Is.EqualTo(1));
            var bar = (IBar)baz;
            Assert.That(bar.Method, Is.EqualTo(2));
            Assert.That(baz[0], Is.EqualTo(50));
            using (Foo foo = new Foo { A = 1000 })
            {
                bar[0] = foo;
                Assert.That(bar[0].A, Is.EqualTo(1000));
            }
            Assert.That(baz.FarAwayFunc, Is.EqualTo(20));
            Assert.That(baz.TakesQux(baz), Is.EqualTo(20));
            Assert.That(baz.ReturnQux().FarAwayFunc, Is.EqualTo(20));
            baz.SetMethod(Bar.ProtectedNestedEnum.Item1);
            Assert.That(baz.P, Is.EqualTo(5));
            baz.PublicDouble = 1.5;
            Assert.That(baz.PublicDouble, Is.EqualTo(1.5));
            baz.PublicInt = 15;
            Assert.That(baz.PublicInt, Is.EqualTo(15));
        }
    }

    [Test]
    public void TestProperties()
    {
        using (var proprietor = new Proprietor())
        {
            Assert.That(proprietor.Parent, Is.EqualTo(0));
            proprietor.Value = 20;
            Assert.That(proprietor.Value, Is.EqualTo(20));
            proprietor.Prop = 50;
            Assert.That(proprietor.Prop, Is.EqualTo(50));
        }
        using (var qux = new Qux())
        {
            using (var p = new P((IQux)qux) { Value = 20 })
            {
                Assert.That(p.Value, Is.EqualTo(30));
                p.Prop = 50;
                Assert.That(p.Prop, Is.EqualTo(150));

                using (var complexType = new ComplexType())
                {
                    p.ComplexType = complexType;
                    Assert.That(p.ComplexType.Check(), Is.EqualTo(5));
                }

                Assert.That(p.Test, Is.True);
                Assert.That(p.IsBool, Is.False);
            }
        }
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
        CSharp.TestDestructors.InitMarker();
        Assert.AreEqual(0, CSharp.TestDestructors.Marker);

        using (var dtors = new TestDestructors())
        {
            Assert.AreEqual(0xf00d, CSharp.TestDestructors.Marker);
            dtors.Dispose();
        }
        Assert.AreEqual(0xcafe, CSharp.TestDestructors.Marker);
    }

    [Test]
    public unsafe void TestArrayOfPointersToPrimitives()
    {
        using (var bar = new Bar())
        {
            var array = new IntPtr[1];
            int i = 5;
            array[0] = new IntPtr(&i);
            bar.ArrayOfPrimitivePointers = array;
            Assert.That(i, Is.EqualTo(*(int*)bar.ArrayOfPrimitivePointers[0]));
        }
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
        using (var p = new P((IQux)new Qux()) { Test = true })
        {
            Assert.That(p.Test, Is.True);
            p.GetTest();
        }
    }

    [Test]
    public void TestDefaultArguments()
    {
        using (var methodsWithDefaultValues = new MethodsWithDefaultValues())
        {
            methodsWithDefaultValues.DefaultPointer();
            methodsWithDefaultValues.DefaultVoidStar();
            methodsWithDefaultValues.DefaultFunctionPointer();
            methodsWithDefaultValues.DefaultValueType();
            methodsWithDefaultValues.DefaultChar();
            methodsWithDefaultValues.DefaultEmptyChar();
            methodsWithDefaultValues.DefaultEmptyEnum();
            methodsWithDefaultValues.DefaultRefTypeBeforeOthers();
            methodsWithDefaultValues.DefaultRefTypeAfterOthers();
            methodsWithDefaultValues.DefaultRefTypeBeforeAndAfterOthers();
            methodsWithDefaultValues.DefaultIntAssignedAnEnum();
            methodsWithDefaultValues.defaultRefAssignedValue();
            methodsWithDefaultValues.DefaultRefAssignedValue();
            methodsWithDefaultValues.DefaultEnumAssignedBitwiseOr();
            methodsWithDefaultValues.DefaultEnumAssignedBitwiseOrShort();
            methodsWithDefaultValues.DefaultNonEmptyCtor();
            methodsWithDefaultValues.DefaultNonEmptyCtorWithNullPtr();
            Assert.That(methodsWithDefaultValues.DefaultMappedToEnum(), Is.EqualTo(Flags.Flag3));
            methodsWithDefaultValues.DefaultMappedToZeroEnum();
            methodsWithDefaultValues.DefaultMappedToEnumAssignedWithCtor();
            methodsWithDefaultValues.DefaultTypedefMappedToEnumRefAssignedWithCtor();
            methodsWithDefaultValues.DefaultZeroMappedToEnumAssignedWithCtor();
            Assert.That(methodsWithDefaultValues.DefaultImplicitCtorInt().Priv, Is.EqualTo(0));
            methodsWithDefaultValues.DefaultImplicitCtorChar();
            methodsWithDefaultValues.DefaultImplicitCtorFoo();
            methodsWithDefaultValues.DefaultImplicitCtorEnum();
            methodsWithDefaultValues.DefaultIntWithLongExpression();
            methodsWithDefaultValues.DefaultRefTypeEnumImplicitCtor();
            methodsWithDefaultValues.Rotate4x4Matrix(0, 0, 0);
            methodsWithDefaultValues.DefaultPointerToValueType();
            methodsWithDefaultValues.DefaultDoubleWithoutF();
            methodsWithDefaultValues.DefaultIntExpressionWithEnum();
            methodsWithDefaultValues.DefaultCtorWithMoreThanOneArg();
            methodsWithDefaultValues.DefaultEmptyBraces();
            methodsWithDefaultValues.DefaultWithRefManagedLong();
            methodsWithDefaultValues.DefaultWithFunctionCall();
            methodsWithDefaultValues.DefaultWithPropertyCall();
            methodsWithDefaultValues.DefaultWithGetPropertyCall();
            methodsWithDefaultValues.DefaultWithIndirectStringConstant();
            methodsWithDefaultValues.DefaultWithDirectIntConstant();
            methodsWithDefaultValues.DefaultWithEnumInLowerCasedNameSpace();
            methodsWithDefaultValues.DefaultWithCharFromInt();
            methodsWithDefaultValues.DefaultWithFreeConstantInNameSpace();
            methodsWithDefaultValues.DefaultWithStdNumericLimits(10, 5);
            methodsWithDefaultValues.DefaultWithSpecialization();
            methodsWithDefaultValues.DefaultOverloadedImplicitCtor();
            methodsWithDefaultValues.DefaultWithParamNamedSameAsMethod(5);
            Assert.That(methodsWithDefaultValues.DefaultIntAssignedAnEnumWithBinaryOperatorAndFlags(), Is.EqualTo((int)(Bar.Items.Item1 | Bar.Items.Item2)));
            Assert.That(methodsWithDefaultValues.DefaultWithConstantFlags(), Is.EqualTo(CSharp.CSharpCool.ConstFlag1 | CSharp.CSharpCool.ConstFlag2 | CSharp.CSharpCool.ConstFlag3));
            Assert.IsTrue(methodsWithDefaultValues.DefaultWithPointerToEnum());
            Assert.AreEqual(CSharp.CSharpCool.DefaultSmallPODInstance.__Instance, methodsWithDefaultValues.DefaultWithNonPrimitiveType().__Instance);
        }
    }

    [Test]
    public void TestGenerationOfAnotherUnitInSameFile()
    {
        AnotherUnitCool.FunctionInAnotherUnit();
    }

    [Test]
    public void TestPrivateOverride()
    {
        using (var hasOverridesWithChangedAccess = new HasOverridesWithChangedAccess())
            hasOverridesWithChangedAccess.PrivateOverride();
        using (var hasOverridesWithIncreasedAccess = new HasOverridesWithIncreasedAccess())
            hasOverridesWithIncreasedAccess.PrivateOverride();
    }

    [Test]
    public void TestQFlags()
    {
        Assert.AreEqual(TestFlag.Flag2, new ComplexType().ReturnsQFlags);
    }

    [Test]
    public void TestCopyCtor()
    {
        using (Qux q1 = new Qux())
        {
            for (int i = 0; i < q1.Array.Length; i++)
            {
                q1.Array[i] = i;
            }
            using (Qux q2 = new Qux(q1))
            {
                for (int i = 0; i < q2.Array.Length; i++)
                {
                    Assert.AreEqual(q1.Array[i], q2.Array[i]);
                }
            }
        }
    }

    [Test]
    public void TestBooleanArray()
    {
        using (Foo foo = new Foo { A = 10 })
        {
            var new_values = new bool[5];
            for (int i = 0; i < new_values.Length; ++i)
            {
                new_values[i] = i % 2 == 0;
            }
            foo.Btest = new_values;
            Assert.AreEqual(true, foo.Btest[0]);
            Assert.AreEqual(false, foo.Btest[1]);
            Assert.AreEqual(true, foo.Btest[2]);
            Assert.AreEqual(false, foo.Btest[3]);
            Assert.AreEqual(true, foo.Btest[4]);
        }
    }


    [Test]
    public void TestImplicitCtor()
    {
        using (Foo foo = new Foo { A = 10 })
        {
            using (MethodsWithDefaultValues m = foo)
            {
                Assert.AreEqual(foo.A, m.A);
            }
        }
        using (MethodsWithDefaultValues m1 = 5)
        {
            Assert.AreEqual(5, m1.A);
        }
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

    [Test]
    public void TestPrimarySecondaryBase()
    {
        using (var a = new MI_A0())
        {
            Assert.That(a.Get(), Is.EqualTo(50));
        }

        using (var c = new MI_C())
        {
            Assert.That(c.Get(), Is.EqualTo(50));
        }

        using (var d = new MI_D())
        {
            Assert.That(d.Get(), Is.EqualTo(50));
        }
    }

    [Test]
    public void TestNativeToManagedMapWithForeignObjects()
    {
        IntPtr native1;
        IntPtr native2;
        var hasVirtualDtor1Map = (IDictionary<IntPtr, HasVirtualDtor1>)typeof(
            HasVirtualDtor1).GetField("NativeToManagedMap",
            BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var hasVirtualDtor2Map = (IDictionary<IntPtr, HasVirtualDtor2>)typeof(
            HasVirtualDtor2).GetField("NativeToManagedMap",
            BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        using (var testNativeToManagedMap = new TestNativeToManagedMap())
        {
            var hasVirtualDtor2 = testNativeToManagedMap.HasVirtualDtor2;
            native2 = hasVirtualDtor2.__Instance;
            native1 = hasVirtualDtor2.HasVirtualDtor1.__Instance;
            Assert.IsTrue(hasVirtualDtor1Map.ContainsKey(native1));
            Assert.IsTrue(hasVirtualDtor2Map.ContainsKey(native2));
            Assert.AreSame(hasVirtualDtor2, testNativeToManagedMap.HasVirtualDtor2);
        }
        Assert.IsFalse(hasVirtualDtor1Map.ContainsKey(native1));
        Assert.IsFalse(hasVirtualDtor2Map.ContainsKey(native2));
    }

    [Test]
    public void TestNativeToManagedMapWithOwnObjects()
    {
        using (var testNativeToManagedMap = new TestNativeToManagedMap())
        {
            var quxMap = (IDictionary<IntPtr, IQux>)typeof(
                Qux).GetField("NativeToManagedMap",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var bar = new Bar();
            testNativeToManagedMap.PropertyWithNoVirtualDtor = bar;
            Assert.AreSame(bar, testNativeToManagedMap.PropertyWithNoVirtualDtor);
            Assert.IsTrue(quxMap.ContainsKey(bar.__Instance));
            bar.Dispose();
            Assert.IsFalse(quxMap.ContainsKey(bar.__Instance));
        }
    }

    [Test]
    public void TestCallingVirtualDtor()
    {
        CallDtorVirtually.Destroyed = false;
        using (var callDtorVirtually = new CallDtorVirtually())
        {
            var hasVirtualDtor1 = CallDtorVirtually.GetHasVirtualDtor1(callDtorVirtually);
            hasVirtualDtor1.Dispose();
        }
        Assert.That(CallDtorVirtually.Destroyed, Is.True);
    }

    [Test]
    public void TestNonOwning()
    {
        CallDtorVirtually.Destroyed = false;
        var nonOwned = CallDtorVirtually.NonOwnedInstance;
        nonOwned.Dispose();
        Assert.That(CallDtorVirtually.Destroyed, Is.False);
    }

    // Verify that the finalizer gets called if an instance is not disposed. Note that
    // we've arranged to have the generator turn on finalizers for (just) the
    // TestFinalizer class.
    [Test]
    public void TestFinalizerGetsCalledWhenNotDisposed()
    {
        using var callbackRegistration = new TestFinalizerDisposeCallbackRegistration();
        var nativeAddr = CreateAndReleaseTestFinalizerInstance(false);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.That(callbackRegistration.NativeAddr, Is.EqualTo(nativeAddr));
        Assert.That(callbackRegistration.IsDisposing, Is.False);
    }

    // Verify that the finalizer is not called if an instance is disposed. Note that we've
    // arranged to have the generator turn on finalizers for (just) the TestFinalizer
    // class.
    [Test]
    public void TestFinalizerNotCalledWhenDisposed()
    {
        using var callbackRegistration = new TestFinalizerDisposeCallbackRegistration();
        var nativeAddr = CreateAndReleaseTestFinalizerInstance(true);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        Assert.That(callbackRegistration.NativeAddr, Is.EqualTo(nativeAddr));
        Assert.That(callbackRegistration.IsDisposing, Is.True);
    }

    // Empirically, the finalizer won't release a reference until the stack frame in which
    // the reference was created has been released. Use this method to create/release the
    // instance.
    private Int64 CreateAndReleaseTestFinalizerInstance(bool dispose)
    {
        var instance = new TestFinalizer();
        Assert.That(instance.__Instance, !Is.EqualTo(IntPtr.Zero));
        var nativeAddr = instance.__Instance.ToInt64();
        if (dispose) instance.Dispose();
        return nativeAddr;
    }

    class TestFinalizerDisposeCallbackRegistration : IDisposable
    {
        public bool? IsDisposing = null;
        public long? NativeAddr = null;

        public TestFinalizerDisposeCallbackRegistration()
        {
            TestFinalizer.DisposeCallback = (isDisposing, intPtr) =>
            {
                IsDisposing = isDisposing;
                NativeAddr = intPtr.ToInt64();
            };
        }

        public void Dispose() => TestFinalizer.DisposeCallback = null;
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
        baseClass.Dispose();
    }

    [Test]
    public unsafe void TestMultiOverLoadPtrToRef()
    {
        var r = 0;
        MultiOverloadPtrToRef m = &r;
        m.Dispose();
        using (var obj = new MultiOverloadPtrToRef(ref r))
        {
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

    [Test]
    public void TestFixedArrayRefType()
    {
        Foo[] foos = new Foo[4];
        foos[0] = new Foo { A = 5 };
        foos[1] = new Foo { A = 6 };
        foos[2] = new Foo { A = 7 };
        foos[3] = new Foo { A = 8 };
        Bar bar = new Bar { Foos = foos };

        Foo[] retFoos = bar.Foos;
        Assert.AreEqual(5, retFoos[0].A);
        Assert.AreEqual(6, retFoos[1].A);
        Assert.AreEqual(7, retFoos[2].A);
        Assert.AreEqual(8, retFoos[3].A);

        foreach (Foo foo in foos)
        {
            foo.Dispose();
        }

        Foo[] foosMore = new Foo[2];
        foosMore[0] = new Foo();
        foosMore[1] = new Foo();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => bar.Foos = foosMore);
        Assert.AreEqual("value", ex.ParamName);
        string[] message = ex.Message.Split(
            Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual("The dimensions of the provided array don't match the required size. (Parameter 'value')", message[0]);

        foreach (Foo foo in foosMore)
        {
            foo.Dispose();
        }
    }

    [Test]
    public void TestOutTypeInterfacePassTry()
    {
        using (var interfaceClassObj = new TestParamToInterfacePassBaseTwo())
        {
            ITestParamToInterfacePassBaseTwo interfaceType = interfaceClassObj;
            using (var obj = new TestOutTypeInterfaces())
            {
                obj.FuncTryInterfaceTypeOut(out interfaceType);
                ITestParamToInterfacePassBaseTwo interfaceTypePtr;
                obj.FuncTryInterfaceTypePtrOut(out interfaceTypePtr);
            }
        }
    }

    [Test]
    public void TestConversionForCtorWithDefaultParams()
    {
        using (Foo foo = 15)
        {
            Assert.That(foo.A, Is.EqualTo(15));
        }
    }

    [Test]
    public unsafe void TestSizeOfDerivesFromTemplateInstantiation()
    {
        Assert.That(sizeof(DerivesFromTemplateInstantiation.__Internal), Is.EqualTo(sizeof(int)));
    }

    [Test]
    public void TestReferenceToArrayWithConstSize()
    {
        int[] incorrectlySizedArray = { 1 };
        Assert.Catch<ArgumentOutOfRangeException>(() => CSharp.CSharpCool.PassConstantArrayRef(incorrectlySizedArray));
        int[] array = { 1, 2 };
        var result = CSharp.CSharpCool.PassConstantArrayRef(array);
        Assert.That(result, Is.EqualTo(array[0]));
    }

    [Test]
    public void TestComparison()
    {
        var testComparison1 = new TestComparison { A = 5, B = 5.5f };
        var testComparison2 = new TestComparison { A = 5, B = 5.5f };
        Assert.IsTrue(testComparison1 == testComparison2);
        Assert.IsTrue(testComparison1.Equals(testComparison2));
        var testHashes = new Dictionary<TestComparison, int>();
        testHashes[testComparison1] = 1;
        testHashes[testComparison2] = 2;
        Assert.That(testHashes[testComparison1], Is.EqualTo(2));
    }

    [Test]
    public void TestOverriddenPropertyFromIndirectBase()
    {
        using (var overridePropertyFromIndirectPrimaryBase = new OverridePropertyFromIndirectPrimaryBase())
        {
            Assert.That(overridePropertyFromIndirectPrimaryBase.Property, Is.EqualTo(5));
        }
    }

    [Test]
    public void TestCallingVirtualBeforeCtorFinished()
    {
        using (new QApplication())
        {
            using (new QWidget())
            {
            }
        }
    }

    [Test]
    public void TestMultipleInheritanceFieldOffsets()
    {
        using (var multipleInheritanceFieldOffsets = new MultipleInheritanceFieldOffsets())
        {
            Assert.That(multipleInheritanceFieldOffsets.Primary, Is.EqualTo(1));
            Assert.That(multipleInheritanceFieldOffsets.Secondary, Is.EqualTo(2));
            Assert.That(multipleInheritanceFieldOffsets.Own, Is.EqualTo(3));
        }
    }

    [Test]
    public void TestVirtualDtorAddedInDerived()
    {
        using (new VirtualDtorAddedInDerived())
        {
        }
        Assert.IsTrue(VirtualDtorAddedInDerived.DtorCalled);
    }

    [Test]
    public void TestGetEnumFromNativePointer()
    {
        using (var getEnumFromNativePointer = new GetEnumFromNativePointer())
        {
            Assert.That(UsesPointerToEnumInParamOfVirtual.CallOverrideOfHasPointerToEnumInParam(
                getEnumFromNativePointer, Flags.Flag3), Is.EqualTo(Flags.Flag3));
        }
    }

    [Test]
    public void TestStdStringConstant()
    {
        Assert.That(CSharp.HasFreeConstant.AnotherUnitCool.STD_STRING_CONSTANT, Is.EqualTo("test"));
        // check a second time to ensure it hasn't been improperly freed
        Assert.That(CSharp.HasFreeConstant.AnotherUnitCool.STD_STRING_CONSTANT, Is.EqualTo("test"));
    }

    [Test]
    public void TestZeroAllocatedMemoryOption()
    {
        // We've arranged in the generator for the ZeroAllocatedMemory option to return true for this one
        // class.
        var test = new ClassZeroAllocatedMemoryTest();
        Assert.That(test.P1, Is.EqualTo(0));
        Assert.That(test.p2.P2p1, Is.EqualTo(0));
        Assert.That(test.p2.P2p2, Is.EqualTo(0));
        Assert.That(test.P3, Is.EqualTo(false));
        Assert.That(test.P4, Is.EqualTo('\0'));
    }

    [Test]
    public void TestAlignment()
    {
        foreach (var internalType in new[]
            {
                typeof(CSharp.IndependentFields.__Internal),
                typeof(CSharp.DependentValueFields.__Internalc__S_DependentValueFields__b),
                typeof(CSharp.DependentValueFields.__Internalc__S_DependentValueFields__f),
                typeof(CSharp.DependentPointerFields.__Internal),
                typeof(CSharp.DependentValueFields.__Internal_Ptr),
                typeof(CSharp.HasDefaultTemplateArgument.__Internalc__S_HasDefaultTemplateArgument__I___S_IndependentFields__I)
            })
        {
            var independentFields = internalType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            var fieldOffset = (FieldOffsetAttribute)independentFields[0].GetCustomAttribute(typeof(FieldOffsetAttribute));
            if (fieldOffset != null)
                Assert.That(fieldOffset.Value, Is.EqualTo(0));
            Assert.That((int)Marshal.OffsetOf(internalType, independentFields[0].Name), Is.EqualTo(0));
            Assert.That(Marshal.SizeOf(internalType), Is.EqualTo(internalType.StructLayoutAttribute.Size));
        }

        foreach (var internalType in new Type[]
            {
                typeof(CSharp.TwoTemplateArgs.__Internal_Ptr),
                typeof(CSharp.TwoTemplateArgs.__Internalc__S_TwoTemplateArgs___I_I),
                typeof(CSharp.TwoTemplateArgs.__Internalc__S_TwoTemplateArgs___I_f)
            })
        {
            var independentFields = internalType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(independentFields.Length, Is.EqualTo(2));
            var fieldOffsetKey = (FieldOffsetAttribute)independentFields[0].GetCustomAttribute(typeof(FieldOffsetAttribute));
            if (fieldOffsetKey != null)
                Assert.That(fieldOffsetKey.Value, Is.EqualTo(0));
            Assert.That((int)Marshal.OffsetOf(internalType, independentFields[0].Name), Is.EqualTo(0));
            var fieldOffsetValue = (FieldOffsetAttribute)independentFields[1].GetCustomAttribute(typeof(FieldOffsetAttribute));
            if (fieldOffsetValue != null)
                Assert.That(fieldOffsetValue.Value, Is.EqualTo(Marshal.SizeOf(IntPtr.Zero)));
            Assert.That((int)Marshal.OffsetOf(internalType, independentFields[1].Name), Is.EqualTo(Marshal.SizeOf(IntPtr.Zero)));
            Assert.That(Marshal.SizeOf(internalType), Is.EqualTo(internalType.StructLayoutAttribute.Size));
        }

        foreach (var (type, offsets) in new (Type, uint[])[] {
            (typeof(ClassCustomTypeAlignment), CSharp.CSharpCool.ClassCustomTypeAlignmentOffsets),
            (typeof(ClassCustomObjectAlignment), CSharp.CSharpCool.ClassCustomObjectAlignmentOffsets),
            (typeof(ClassMicrosoftObjectAlignment), CSharp.CSharpCool.ClassMicrosoftObjectAlignmentOffsets),
            (typeof(StructWithEmbeddedArrayOfStructObjectAlignment), CSharp.CSharpCool.StructWithEmbeddedArrayOfStructObjectAlignmentOffsets),
        })
        {
            var internalType = type.GetNestedType("__Internal");
            var managedOffsets = internalType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .SkipWhile(x => x.FieldType == typeof(IntPtr))
                .Where(x => !x.Name.EndsWith("Padding"))
                .Select(field => (uint)Marshal.OffsetOf(internalType, field.Name));

            Assert.That(managedOffsets, Is.EqualTo(offsets));
            Assert.That(Marshal.SizeOf(internalType), Is.EqualTo(internalType.StructLayoutAttribute.Size));
        }
    }

    [Test]
    public void TestEmbeddedArrayOfStructAccessor()
    {
        const ulong firstLong = 0xC92EEDE87AAB4FECul;
        const ulong secondLong = 0xAD5FB16491935522ul;

        var testStruct = new StructWithEmbeddedArrayOfStructObjectAlignment();
        testStruct.EmbeddedStruct[0].Ui64 = firstLong;
        testStruct.EmbeddedStruct[1].Ui64 = secondLong;

        // Since the memory allocated for EmbeddedStruct is generally uninintialized, I suppose it _could_
        // just happen to match, but it seems very unlikely.
        Assert.That(firstLong, Is.EqualTo(testStruct.EmbeddedStruct[0].Ui64));
        Assert.That(secondLong, Is.EqualTo(testStruct.EmbeddedStruct[1].Ui64));
    }

    public void TestClassSize()
    {
        Assert.That(Marshal.SizeOf<HasSecondaryBaseWithAbstractWithDefaultArg.__Internal>, Is.EqualTo(Marshal.SizeOf<IntPtr>() * 2));
    }

    [Test]
    public void TestConstantArray()
    {
        Assert.That(CSharp.CSharpCool.VariableWithFixedPrimitiveArray[0], Is.EqualTo(5));
        Assert.That(CSharp.CSharpCool.VariableWithFixedPrimitiveArray[1], Is.EqualTo(10));
        Assert.That(CSharp.CSharpCool.VariableWithVariablePrimitiveArray[0], Is.EqualTo(15));
        Assert.That(CSharp.CSharpCool.VariableWithVariablePrimitiveArray[1], Is.EqualTo(20));
    }

    [Test]
    public void TestStaticVariables()
    {
        Assert.That(StaticVariables.Boolean, Is.EqualTo(true));
        Assert.That(StaticVariables.Chr, Is.EqualTo('G'));
        Assert.That(StaticVariables.UChr, Is.EqualTo('G'));
        Assert.That(StaticVariables.Int, Is.EqualTo(1020304050));
        Assert.That(StaticVariables.Float, Is.EqualTo(0.5020f));
        Assert.That(StaticVariables.String, Is.EqualTo("Str"));
        Assert.That(StaticVariables.ChrArray, Is.EqualTo(new[] { 'A', 'B' }));
        Assert.That(StaticVariables.IntArray, Is.EqualTo(new[] { 1020304050, 1526374850 }));
        Assert.That(StaticVariables.FloatArray, Is.EqualTo(new[] { 0.5020f, 0.6020f }));
        Assert.That(StaticVariables.BoolArray, Is.EqualTo(new[] { false, true }));
        Assert.That(StaticVariables.VoidPtrArray, Is.EqualTo(new IntPtr[] { new IntPtr(0x10203040), new IntPtr(0x40302010) }));
    }

    [Test]
    public void TestVariableInitializer()
    {
        Assert.That(VariablesWithInitializer.Boolean, Is.EqualTo(true));
        Assert.That(VariablesWithInitializer.Chr, Is.EqualTo('G'));
        Assert.That(VariablesWithInitializer.UChr, Is.EqualTo('G'));
        Assert.That(VariablesWithInitializer.Int, Is.EqualTo(1020304050));
        Assert.That(VariablesWithInitializer.IntSum, Is.EqualTo(VariablesWithInitializer.Int + 500));
        Assert.That(VariablesWithInitializer.Float, Is.EqualTo(0.5020f));
        Assert.That(VariablesWithInitializer.Double, Is.EqualTo(0.700020235));
        Assert.That(VariablesWithInitializer.DoubleSum, Is.EqualTo(0.700020235 + 23.17376));
        Assert.That(VariablesWithInitializer.DoubleFromConstexprFunction, Is.EqualTo(0.700020235 + 23.17376));
        Assert.That(VariablesWithInitializer.Int64, Is.EqualTo(602030405045));
        Assert.That(VariablesWithInitializer.UInt64, Is.EqualTo(9602030405045));
        Assert.That(VariablesWithInitializer.String, Is.EqualTo("Str"));
        Assert.That(VariablesWithInitializer.WideString, Is.EqualTo("Str"));
        Assert.That(VariablesWithInitializer.StringArray1, Is.EqualTo(new[] { "StrF,\"or" }));
        Assert.That(VariablesWithInitializer.StringArray3, Is.EqualTo(new[] { "StrF,\"or", "C#", VariablesWithInitializer.String }));
        Assert.That(VariablesWithInitializer.StringArray30, Is.EqualTo(new[] {
            "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str",
            "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str",
            "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str", "Str"}));
        Assert.That(VariablesWithInitializer.StringArray3EmptyInitList, Is.EqualTo(new[] { "", "", "" }));
        Assert.That(VariablesWithInitializer.WideStringArray, Is.EqualTo(new[] { "Str", "C#" }));
        Assert.That(VariablesWithInitializer.ChrArray, Is.EqualTo(new[] { 'A', 'B' }));
        Assert.That(VariablesWithInitializer.IntArray, Is.EqualTo(new[] { 1020304050, 1526374850 }));
        Assert.That(VariablesWithInitializer.IntArray3, Is.EqualTo(new[] { 1020304050, 1526374850, default }));
        Assert.That(VariablesWithInitializer.FloatArray, Is.EqualTo(new[] { 0.5020f, 0.6020f }));
        Assert.That(VariablesWithInitializer.BoolArray, Is.EqualTo(new[] { false, true }));
    }

    [Test]
    public void TestIndirectVariableInitializer()
    {
        // The actual test is that the generator doesn't throw when generating
        // IndependentStringVariable. If we're running the test, we must have
        // generated CSharp.cs without crashing the generator.
        Assert.That(MoreVariablesWithInitializer.DependentStringVariable, Is.EqualTo(MoreVariablesWithInitializer.IndependentStringVariable));
    }

    [Test]
    public void TestPointerPassedAsItsSecondaryBase()
    {
        using (QApplication application = new QApplication())
        {
            using (QWidget widget = new QWidget())
            {
                using (QPainter painter = new QPainter(widget))
                {
                    Assert.That(widget.Test, Is.EqualTo(5));
                }
            }
        }
    }

    [Test]
    public void TestUnicode()
    {
        using (var testString = new TestString())
        {
            Assert.That(testString.UnicodeConst, Is.EqualTo("ქართული ენა"));
        }
    }

    [Test]
    public void TestStringMemManagement()
    {
        const int instanceCount = 100;
        const string otherString = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

        var batch = new TestString[instanceCount];
        for (var i = 0; i < instanceCount; i++)
        {
            batch[i] = new TestString { UnicodeConst = otherString };
            if (batch[i].UnicodeConst != otherString)
            {
                throw new Exception($"iteration {i}");
            }
        }

        GC.Collect();

        for (var i = 0; i < instanceCount; i++)
        {
            if (batch[i].UnicodeConst != otherString)
            {
                throw new Exception($"iteration {i}");
            }
            Assert.That(batch[i].UnicodeConst, Is.EqualTo(otherString));
        }

        Array.ForEach(batch, ts => ts.Dispose());
    }

    static bool OwnsNativeMemory<T>(T instance, string fieldName)
    {
        return (bool)instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(instance);
    }

    [Test]
    public void TestManagedOwnsChar32String()
    {
        const string constructorString = "ქართული ენა";
        const string str = "ßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵ";

        using (var ts = new TestChar32String())
        {
            Assert.That(ts.ThirtyTwoBitConst, Is.EqualTo(constructorString));
            Assert.That(OwnsNativeMemory(ts, "__thirtyTwoBitConst_OwnsNativeMemory"), Is.EqualTo(false));

            ts.ThirtyTwoBitConst = str;
            Assert.That(ts.RetrieveString, Is.EqualTo(str));
            Assert.That(OwnsNativeMemory(ts, "__thirtyTwoBitConst_OwnsNativeMemory"), Is.EqualTo(true));
        }
    }

    [Test]
    public void TestNativeOwnsChar32String()
    {
        const string constructorString = "ქართული ენა";
        const string str = "ҪҫҬҭҮүҰұҲҳҴҵҶҷҸҹҺһҼҽҾҿӀӁӂӃӄӅӆӇӈӉӊӋӌӍӎӏӐӑӒӓӔӕӖӗӘәӚӛӜӝӞӟӠӡӢӣӤӥӦӧӨөӪӫӬӭӮӯӰӱӲӳӴӵӶӷӸӹӺӻӼӽ";
        const string otherStr = "Test String";

        using (var ts = new TestChar32String())
        {
            Assert.That(ts.ThirtyTwoBitConst, Is.EqualTo(constructorString));
            Assert.That(OwnsNativeMemory(ts, "__thirtyTwoBitConst_OwnsNativeMemory"), Is.EqualTo(false));
            ts.UpdateString(str);
            Assert.That(ts.ThirtyTwoBitConst, Is.EqualTo(str));
            Assert.That(OwnsNativeMemory(ts, "__thirtyTwoBitConst_OwnsNativeMemory"), Is.EqualTo(false));

            var x = (uint*)ts.ThirtyTwoBitNonConst;
            for (int i = 0; i < otherStr.Length; i++)
            {
                Assert.That(*x++, Is.EqualTo(otherStr[i]));
            }
            Assert.That(*x, Is.EqualTo(0));
        }
    }

    [Test]
    public void TestManagedOwnsChar16String()
    {
        const string constructorString = "ქართული ენა";
        const string str = "ßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵ";

        using (var ts = new TestChar16String())
        {
            Assert.That(ts.SixteenBitConst, Is.EqualTo(constructorString));
            Assert.That(OwnsNativeMemory(ts, "__sixteenBitConst_OwnsNativeMemory"), Is.EqualTo(false));

            ts.SixteenBitConst = str;
            Assert.That(ts.RetrieveString, Is.EqualTo(str));
            Assert.That(OwnsNativeMemory(ts, "__sixteenBitConst_OwnsNativeMemory"), Is.EqualTo(true));
        }
    }

    [Test]
    public void TestNativeOwnsChar16String()
    {
        const string constructorString = "ქართული ენა";
        const string str = "ѐёђѓєѕіїјљњћќѝўџѠѡѢѣѤѥѦѧѨѩѪѫѬѭѮѯѰѱѲѳѴѵѶѷѸѹѺѻѼѽѾѿҀҁҊҋҌҍҎҏҐґҒғҔҕҖҗҘҙҚқҜҝҞҟҠҡҢңҤҥҦҧҨҩ";
        const string otherStr = "Test String";

        using (var ts = new TestChar16String())
        {
            Assert.That(ts.SixteenBitConst, Is.EqualTo(constructorString));
            Assert.That(OwnsNativeMemory(ts, "__sixteenBitConst_OwnsNativeMemory"), Is.EqualTo(false));

            ts.UpdateString(str);
            Assert.That(ts.SixteenBitConst, Is.EqualTo(str));
            Assert.That(OwnsNativeMemory(ts, "__sixteenBitConst_OwnsNativeMemory"), Is.EqualTo(false));

            var x = ts.SixteenBitNonConst;
            for (int i = 0; i < otherStr.Length; i++)
            {
                Assert.That(*x++, Is.EqualTo(otherStr[i]));
            }
            Assert.That(*x, Is.EqualTo(0));
        }
    }

    [Test]
    public void TestStringRefWithCopyConstructor()
    {
        const string otherString = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
        var ts1 = new TestString { UnicodeConst = otherString };
        var ts2 = new TestString(ts1);

        // verify that the copy has its own reference to UnicodeConst.
        var ownsNativeMemory = (bool)ts2.GetType()
                    .GetField("__unicodeConst_OwnsNativeMemory", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(ts2);
        Assert.That(true, Is.EqualTo(ownsNativeMemory));

        var offset = Marshal.OffsetOf<TestString.__Internal>("unicodeConst");
        var ts1PtrRef = IntPtr.Add(ts1.__Instance, (int)offset);
        var ts2PtrRef = IntPtr.Add(ts2.__Instance, (int)offset);
        var ts1Ptr = *(IntPtr*)ts1PtrRef;
        var ts2Ptr = *(IntPtr*)ts2PtrRef;
        Assert.That(ts1Ptr != ts2Ptr);

        // should be able to dispose in any order.
        Assert.That(otherString, Is.EqualTo(ts1.UnicodeConst));
        ts1.Dispose();
        Assert.That(otherString, Is.EqualTo(ts2.UnicodeConst));
        ts2.Dispose();
    }

    [Test]
    public void TestEnumProperty()
    {
        using (var proprietor = new Proprietor())
        {
            Assert.That(proprietor.Items, Is.EqualTo(Bar.Items.Item1));
            proprietor.Items = Bar.Items.Item2;
            Assert.That(proprietor.Items, Is.EqualTo(Bar.Items.Item2));
            Assert.That(proprietor.ItemsByValue, Is.EqualTo(Bar.Items.Item1));
            proprietor.ItemsByValue = Bar.Items.Item2;
            Assert.That(proprietor.ItemsByValue, Is.EqualTo(Bar.Items.Item2));
        }
    }

    [Test]
    public void TestOverrideVirtualWithString()
    {
        using (var overrideVirtualWithString = new OverrideVirtualWithString())
        {
            Assert.That(overrideVirtualWithString.CallsVirtualToReturnString("test"), Is.EqualTo("test_test"));
            Assert.IsFalse(overrideVirtualWithString.CallsVirtualToReturnBool(true));
        }
    }

    [Test]
    public void TestStackOverflowOnVirtualCall()
    {
        using (var hasMissingObjectOnVirtualCall = new HasMissingObjectOnVirtualCall())
        {
            using (var missingObjectOnVirtualCall = new MissingObjectOnVirtualCall())
            {
                hasMissingObjectOnVirtualCall.SetMissingObjectOnVirtualCall(missingObjectOnVirtualCall);
                Assert.That(hasMissingObjectOnVirtualCall.MakeMissingObjectOnVirtualCall(), Is.EqualTo(15));
            }
        }
    }

    [Test]
    public void TestTemplateWithPointerToTypeParameter()
    {
        Assert.That(IndependentFieldsExtensions.StaticDependent(5), Is.EqualTo(5));
    }

    [Test]
    public void TestTemplateCopyConstructor()
    {
        using (var original = new IndependentFields<int>(5.0f))
        {
            using (var copy = new IndependentFields<int>(original))
            {
                Assert.That(copy.Independent, Is.EqualTo(original.Independent));
            }
        }
    }

    [Test]
    public void TestTemplateWithIndependentFields()
    {
        using (var independentFields = new IndependentFields<int>())
        {
            Assert.That(independentFields.GetDependent(5), Is.EqualTo(5));
            Assert.That(independentFields.Independent, Is.EqualTo(1));
        }
        using (var independentFields = new IndependentFields<bool>())
        {
            Assert.That(independentFields.GetDependent(true), Is.EqualTo(true));
            Assert.That(independentFields.Independent, Is.EqualTo(1));
        }
    }

    [Test]
    public void TestVirtualTemplate()
    {
        using (var virtualTemplate = new VirtualTemplate<int>())
        {
            Assert.That(virtualTemplate.Function, Is.EqualTo(5));
            int i = 15;
            Assert.That(*virtualTemplate.Function(ref i), Is.EqualTo(15));
        }
        using (var virtualTemplate = new VirtualTemplate<bool>())
        {
            Assert.That(virtualTemplate.Function, Is.EqualTo(5));
            bool b = true;
            Assert.That(*virtualTemplate.Function(ref b), Is.EqualTo(true));
        }
        using (var virtualTemplate = new VirtualDependentValueFields<int>())
        {
        }
        using (var virtualTemplate = new VirtualDependentValueFields<float>())
        {
        }
        using (var virtualTemplate = new VirtualDependentValueFields<string>())
        {
        }
    }

    [Test]
    public void TestOverrideOfTemplate()
    {
        using (var hasVirtualTemplate = new HasVirtualTemplate())
        {
            using (var overrideVirtualTemplate = new OverrideVirtualTemplate())
            {
                hasVirtualTemplate.SetV(overrideVirtualTemplate);
                Assert.That(hasVirtualTemplate.Function, Is.EqualTo(10));
            }
        }
    }

    [Test]
    public void TestDefaultTemplateArgument()
    {
        using (var hasDefaultTemplateArgument = new HasDefaultTemplateArgument<int, int>())
        {
            hasDefaultTemplateArgument.Property = 10;
            Assert.That(hasDefaultTemplateArgument.Property, Is.EqualTo(10));
        }
    }

    [Test]
    public void TestTemplateStaticProperty()
    {
        HasDefaultTemplateArgument<int, int>.StaticProperty = 5;
        Assert.That(HasDefaultTemplateArgument<int, int>.StaticProperty, Is.EqualTo(5));
    }

    [Test]
    public void TestIndependentConstInIndependentTemplate()
    {
        Assert.That(IndependentFields<int>.IndependentConst, Is.EqualTo(15));
        Assert.That(IndependentFields<bool>.IndependentConst, Is.EqualTo(15));
        Assert.That(IndependentFields<T1>.IndependentConst, Is.EqualTo(15));
    }

    [Test]
    public void TestTemplateWithIndexer()
    {
        using (var templateWithIndexer = new TemplateWithIndexer<int>())
        {
            templateWithIndexer[0] = 5;
            Assert.That(templateWithIndexer[0], Is.EqualTo(5));
            templateWithIndexer["test"] = 15;
            Assert.That(templateWithIndexer["test"], Is.EqualTo(15));
        }
        using (var templateWithIndexer = new TemplateWithIndexer<T2>())
        {
            using (var t2 = new T2())
            {
                templateWithIndexer[0] = t2;
                var item = templateWithIndexer[0];
                Assert.That(item.Field, Is.EqualTo(t2.Field));
                item.Field = 5;
                Assert.That(templateWithIndexer[0].Field, Is.EqualTo(5));
            }
            using (var t2 = new T2(15))
            {
                templateWithIndexer["test"] = t2;
                Assert.That(templateWithIndexer["test"].Field, Is.EqualTo(t2.Field));
            }
        }
    }

    [Test]
    public void TestTemplateComparison()
    {
        using (var left = new HasDefaultTemplateArgument<int, int>())
        using (var right = new HasDefaultTemplateArgument<int, int>())
        {
            left.Property = 15;
            right.Property = 15;
            Assert.IsTrue(left == right);
            Assert.IsTrue(left.Equals(right));
        }
    }

    [Test]
    public void TestReturnInjectedClass()
    {
        using (var dependentValueFields = new DependentValueFields<int>())
        {
            dependentValueFields.DependentValue = 5;
            Assert.That(dependentValueFields.ReturnInjectedClass().DependentValue,
                Is.EqualTo(dependentValueFields.DependentValue));
        }
    }

    [Test]
    public void TestReturnTemplateValue()
    {
        using (var dependentValueFields = new DependentValueFields<int>())
        {
            dependentValueFields.DependentValue = 10;
            Assert.That(dependentValueFields.ReturnValue().DependentValue,
                Is.EqualTo(dependentValueFields.DependentValue));
        }
    }

    [Test]
    public void TestOperatorReturnTemplateValue()
    {
        using (var dependentValueFields = new DependentValueFields<int>())
        {
            using (var other = new DependentValueFields<int>())
            {
                dependentValueFields.DependentValue = 10;
                other.DependentValue = 15;
                Assert.That(dependentValueFields.DependentValue, Is.EqualTo(10));
                Assert.That((other).DependentValue, Is.EqualTo(15));
                Assert.That((dependentValueFields + other).DependentValue, Is.EqualTo(0));
            }
        }
    }

    [Test]
    public void TestTemplateSpecializationWithPointer()
    {
        using (var dependentValueFields = new DependentValueFields<IntPtr>())
        {
            int i = 10;
            dependentValueFields.DependentValue = (IntPtr)(&i);
            Assert.That(*(int*)dependentValueFields.DependentValue, Is.EqualTo(10));
        }
    }

    [Test]
    public void TestReturnTemplateWithRenamedTypeArg()
    {
        using (var dependentValueFields = new DependentValueFields<int>())
        {
            dependentValueFields.DependentValue = 10;
            using (var hasDefaultTemplateArgument = new HasDefaultTemplateArgument<int, int>())
            {
                var returnTemplateWithRenamedTypeArg =
                    hasDefaultTemplateArgument.ReturnTemplateWithRenamedTypeArg(dependentValueFields);
                Assert.That(returnTemplateWithRenamedTypeArg.DependentValue, Is.EqualTo(10));
            }
        }
    }

    [Test]
    public void TestPropertyReturnsTemplateWithRenamedTypeArg()
    {
        using (var hasDefaultTemplateArgument = new HasDefaultTemplateArgument<int, int>())
        {
            var returnTemplateWithRenamedTypeArg =
                hasDefaultTemplateArgument.PropertyReturnsTemplateWithRenamedTypeArg;
            Assert.That(returnTemplateWithRenamedTypeArg.DependentValue, Is.EqualTo(0));
        }
    }

    [Test]
    public void TestTemplateDerivedFromRegularDynamic()
    {
        using (var templateDerivedFromRegularDynamic = new TemplateDerivedFromRegularDynamic<RegularDynamic>())
        {
            templateDerivedFromRegularDynamic.VirtualFunction();
        }
    }

    [Test]
    public void TestFieldWithSpecializationType()
    {
        using (var virtualTemplate = new VirtualTemplate<int>())
        {
            using (var dependentValueFields = new DependentValueFields<float>())
            {
                dependentValueFields.DependentValue = 15;
                virtualTemplate.FieldWithSpecializationType = dependentValueFields;
                Assert.That(virtualTemplate.FieldWithSpecializationType.DependentValue, Is.EqualTo(15));
            }
        }
    }

    [Test]
    public void TestAbstractTemplate()
    {
        using (Foo foo = new Foo())
        using (AbstractTemplate<int> abstractTemplate = foo.AbstractTemplate)
        {
            Assert.That(abstractTemplate.Property, Is.EqualTo(55));
            Assert.That(abstractTemplate.CallFunction(), Is.EqualTo(65));
        }
    }

    [Test]
    public void TestAbstractImplTemplate()
    {
        using (var returnsPointer = new DependentValueFields<int>())
        {
            using (var value = returnsPointer.AbstractReturnPointer)
            {
                Assert.That(new IntPtr(value.AbstractReturnPointer()), Is.EqualTo(IntPtr.Zero));
            }
        }
    }

    [Test]
    public void TestSpecializationForSecondaryBase()
    {
        using (var hasSpecializationForSecondaryBase = new HasSpecializationForSecondaryBase())
        {
            hasSpecializationForSecondaryBase.DependentValue = 5;
            Assert.That(hasSpecializationForSecondaryBase.DependentValue, Is.EqualTo(5));
        }
    }

    [Test]
    public void TestExtensionsOfSpecializationsAsSecondaryBases()
    {
        using (var hasSpecializationForSecondaryBase = new HasSpecializationForSecondaryBase())
        {
            Assert.IsTrue(hasSpecializationForSecondaryBase.PropertyReturnDependentPointer() == null);
        }
    }

    [Test]
    public void TestFieldWithDependentPointerType()
    {
        float f = 0.5f;
        using (var dependentPointerFields = DependentPointerFieldsExtensions.DependentPointerFields(ref f))
        {
            Assert.That(dependentPointerFields.Property, Is.EqualTo(f));
        }
    }

    [Test]
    public void TestAbstractImplementatonsInPrimaryAndSecondaryBases()
    {
        using (var implementsAbstractsFromPrimaryAndSecondary = new ImplementsAbstractsFromPrimaryAndSecondary())
        {
            Assert.That(implementsAbstractsFromPrimaryAndSecondary.AbstractInPrimaryBase, Is.EqualTo(101));
            Assert.That(implementsAbstractsFromPrimaryAndSecondary.AbstractInSecondaryBase, Is.EqualTo(5));
            Assert.That(implementsAbstractsFromPrimaryAndSecondary.AbstractReturnsFieldInPrimaryBase, Is.EqualTo(201));
            Assert.That(implementsAbstractsFromPrimaryAndSecondary.AbstractReturnsFieldInSecondaryBase, Is.EqualTo(202));
        }
    }

    [Test]
    public void TestOverriddenSetterOnly()
    {
        using (var hasGetterAndOverriddenSetter = new HasGetterAndOverriddenSetter())
        {
            const int value = 5;
            hasGetterAndOverriddenSetter.SetBaseSetter(value);
            Assert.That(hasGetterAndOverriddenSetter.BaseSetter, Is.EqualTo(value));
        }
    }

    private class OverrideVirtualWithString : HasVirtualTakesReturnsProblematicTypes
    {
        public override string VirtualTakesAndReturnsString(string c)
        {
            return "test_test";
        }

        public override bool VirtualTakesAndReturnsBool(bool b)
        {
            return !base.VirtualTakesAndReturnsBool(b);
        }
    }

    private class GetEnumFromNativePointer : UsesPointerToEnumInParamOfVirtual
    {
        public override Flags HasPointerToEnumInParam(Flags pointerToEnum)
        {
            return base.HasPointerToEnumInParam(pointerToEnum);
        }
    }

    [Test]
    public void TestGenerationOfIncompleteClasses()
    {
        var incompleteStruct = CSharp.CSharpCool.CreateIncompleteStruct();
        Assert.IsNotNull(incompleteStruct);
        Assert.DoesNotThrow(() => CSharp.CSharpCool.UseIncompleteStruct(incompleteStruct));
    }

    [Test]
    public void TestForwardDeclaredStruct()
    {
        using (var forwardDeclaredStruct = CSharp.CSharpCool.CreateForwardDeclaredStruct(10))
        {
            var i = CSharp.CSharpCool.UseForwardDeclaredStruct(forwardDeclaredStruct);
            Assert.AreEqual(forwardDeclaredStruct.I, i);
        }
    }

    [Test, Ignore("The Linux CI (alone) failes to generate these functions.")]
    public void TestDuplicateDeclaredIncompleteStruct()
    {
        //var duplicateDeclaredIncompleteStruct = CSharp.CSharpCool.CreateDuplicateDeclaredStruct(10);
        //var i = CSharp.CSharpCool.UseDuplicateDeclaredStruct(duplicateDeclaredIncompleteStruct);
        //Assert.AreEqual(10, i);
    }

    [Test]
    public void TestMyMacroTestEnum()
    {
        var a = (MyMacroTestEnum)'1';
        var b = (MyMacroTestEnum)'2';
        Assert.IsTrue(a == MyMacroTestEnum.MY_MACRO_TEST_1 && b == MyMacroTestEnum.MY_MACRO_TEST_2);
        Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(MyMacroTestEnum)));
    }

    [Test]
    public void TestMyMacro2TestEnum()
    {
        var a = (MyMacroTest2Enum)0;
        var b = (MyMacroTest2Enum)1;
        var c = (MyMacroTest2Enum)0x2;
        var d = (MyMacroTest2Enum)(1 << 2);
        var e = (MyMacroTest2Enum)(b | c);
        var f = (MyMacroTest2Enum)(b | c | d);
        var g = (MyMacroTest2Enum)(1 << 3);
        var h = (MyMacroTest2Enum)((1 << 4) - 1);
        Assert.IsTrue(a == MyMacroTest2Enum.MY_MACRO_TEST2_0 && b == MyMacroTest2Enum.MY_MACRO_TEST2_1 &&
                    c == MyMacroTest2Enum.MY_MACRO_TEST2_2 && d == MyMacroTest2Enum.MY_MACRO_TEST2_3 &&
                    e == MyMacroTest2Enum.MY_MACRO_TEST2_1_2 && f == MyMacroTest2Enum.MY_MACRO_TEST2_1_2_3 &&
                    g == MyMacroTest2Enum.MY_MACRO_TEST2_4 && h == MyMacroTest2Enum.MY_MACRO_TEST2ALL);
    }

    [Test]
    public void TestSignedMacroToEnums()
    {
        Assert.AreEqual(typeof(long), Enum.GetUnderlyingType(typeof(SignedMacroValuesToEnumTest)));
        Assert.AreEqual(1 << 5, (long)SignedMacroValuesToEnumTest.SIGNED_MACRO_VALUES_TO_ENUM_TEST_1);
        Assert.AreEqual(1 << 22, (long)SignedMacroValuesToEnumTest.SIGNED_MACRO_VALUES_TO_ENUM_TEST_2);
        Assert.AreEqual(1L << 32, (long)SignedMacroValuesToEnumTest.SIGNED_MACRO_VALUES_TO_ENUM_TEST_3);
        Assert.AreEqual(-1, (long)SignedMacroValuesToEnumTest.SIGNED_MACRO_VALUES_TO_ENUM_TEST_4);
    }

    [Test]
    public void BoolValuedEnumsTest()
    {
        Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TestBoolValuedEnums)));
        Assert.AreEqual(Convert.ToByte(true), (byte)TestBoolValuedEnums.TEST_BOOL_VALUED_ENUMS_V1);
        Assert.AreEqual(Convert.ToByte(false), (byte)TestBoolValuedEnums.TEST_BOOL_VALUED_ENUMS_V2);
        Assert.AreEqual(Convert.ToByte(42), (byte)TestBoolValuedEnums.TEST_BOOL_VALUED_ENUMS_V3);
    }

    [Test]
    public void TestGenerationOfArraySetter()
    {
        var complexArrayElements = new ComplexArrayElement[10];
        complexArrayElements[0] = new ComplexArrayElement();
        complexArrayElements[1] = new ComplexArrayElement();
        complexArrayElements[2] = new ComplexArrayElement();
        complexArrayElements[3] = new ComplexArrayElement();
        complexArrayElements[4] = new ComplexArrayElement();
        complexArrayElements[5] = new ComplexArrayElement();
        complexArrayElements[6] = new ComplexArrayElement();
        complexArrayElements[7] = new ComplexArrayElement();
        complexArrayElements[8] = new ComplexArrayElement();
        complexArrayElements[9] = new ComplexArrayElement();

        complexArrayElements[0].BoolField = true;
        complexArrayElements[0].IntField = 2450;
        complexArrayElements[0].FloatField = -40;

        complexArrayElements[1].BoolField = false;
        complexArrayElements[1].IntField = 2451;
        complexArrayElements[1].FloatField = -41;

        complexArrayElements[2].BoolField = true;
        complexArrayElements[2].IntField = 2452;
        complexArrayElements[2].FloatField = -42;

        complexArrayElements[3].BoolField = true;
        complexArrayElements[3].IntField = 2453;
        complexArrayElements[3].FloatField = -43;

        complexArrayElements[4].BoolField = false;
        complexArrayElements[4].IntField = 2454;
        complexArrayElements[4].FloatField = -44;

        complexArrayElements[5].BoolField = true;
        complexArrayElements[5].IntField = 2455;
        complexArrayElements[5].FloatField = -45;

        complexArrayElements[6].BoolField = true;
        complexArrayElements[6].IntField = 2456;
        complexArrayElements[6].FloatField = -46;

        complexArrayElements[7].BoolField = true;
        complexArrayElements[7].IntField = 2457;
        complexArrayElements[7].FloatField = -47;

        complexArrayElements[8].BoolField = false;
        complexArrayElements[8].IntField = 2458;
        complexArrayElements[8].FloatField = -48;

        complexArrayElements[9].BoolField = true;
        complexArrayElements[9].IntField = 2459;
        complexArrayElements[9].FloatField = -49;

        using (HasComplexArray tests = new HasComplexArray { ComplexArray = complexArrayElements })
        {
            Assert.AreEqual(tests.ComplexArray[0].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[0].IntField, 2450);
            Assert.AreEqual(tests.ComplexArray[0].FloatField, -40);

            Assert.AreEqual(tests.ComplexArray[1].BoolField, false);
            Assert.AreEqual(tests.ComplexArray[1].IntField, 2451);
            Assert.AreEqual(tests.ComplexArray[1].FloatField, -41);

            Assert.AreEqual(tests.ComplexArray[2].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[2].IntField, 2452);
            Assert.AreEqual(tests.ComplexArray[2].FloatField, -42);

            Assert.AreEqual(tests.ComplexArray[3].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[3].IntField, 2453);
            Assert.AreEqual(tests.ComplexArray[3].FloatField, -43);

            Assert.AreEqual(tests.ComplexArray[4].BoolField, false);
            Assert.AreEqual(tests.ComplexArray[4].IntField, 2454);
            Assert.AreEqual(tests.ComplexArray[4].FloatField, -44);

            Assert.AreEqual(tests.ComplexArray[5].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[5].IntField, 2455);
            Assert.AreEqual(tests.ComplexArray[5].FloatField, -45);

            Assert.AreEqual(tests.ComplexArray[6].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[6].IntField, 2456);
            Assert.AreEqual(tests.ComplexArray[6].FloatField, -46);

            Assert.AreEqual(tests.ComplexArray[7].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[7].IntField, 2457);
            Assert.AreEqual(tests.ComplexArray[7].FloatField, -47);

            Assert.AreEqual(tests.ComplexArray[8].BoolField, false);
            Assert.AreEqual(tests.ComplexArray[8].IntField, 2458);
            Assert.AreEqual(tests.ComplexArray[8].FloatField, -48);

            Assert.AreEqual(tests.ComplexArray[9].BoolField, true);
            Assert.AreEqual(tests.ComplexArray[9].IntField, 2459);
            Assert.AreEqual(tests.ComplexArray[9].FloatField, -49);
        }

        foreach (var complexArrayElement in complexArrayElements)
        {
            complexArrayElement.Dispose();
        }
    }

    [Test]
    public void TestConstRefIndexer()
    {
        using (var indexproperty = new TestIndexedProperties())
        {
            Assert.That(indexproperty[2], Is.EqualTo(2));
        }
    }

    [Test]
    public void TestVoidPtrReturningIndexer()
    {
        using (var indexproperty = new TestIndexedProperties())
        {
            uint n = 21;
            Assert.That(*((int*)indexproperty[n]), Is.EqualTo(21));
        }
    }

    [Test]
    public void TestFuncWithTypedefedFuncPtrAsParam()
    {
        TypedefedFuncPtr function = (a, b) => 5;
        Assert.That(CSharp.CSharpCool.FuncWithTypedefedFuncPtrAsParam(function), Is.EqualTo(5));
    }

    [Test]
    public void TestIncrement()
    {
        var bar = new Bar();
        bar.Index = 5;
        bar++;
        Assert.That(bar.Index, Is.EqualTo(6));
        bar.Dispose();
    }

    [Test]
    public void TestArrayParams()
    {
        Foo[] pointers = { new Foo { A = 2 }, new Foo { A = 5 } };
        int[] ints = { 6, 7 };
        Foo[] values = { new Foo { A = 10 }, new Foo { A = 20 } };
        using (var testArrays = new TestArrays())
        {
            Assert.That(testArrays.TakeArrays(pointers, ints, values), Is.EqualTo(50));
            Assert.That(testArrays.VirtualTakeArrays(pointers, ints, values), Is.EqualTo(50));
        }
    }

    [Test]
    public void TestFixedArrayParams()
    {
        Foo[] pointers = { new Foo { A = 2 }, new Foo { A = 5 }, new Foo { A = 7 } };
        var int1 = 6;
        var int2 = 7;
        var int3 = 8;
        var int4 = 9;
        int[] ints = { int1, int2, int3, int4 };
        int*[] intPointers = { &int1, &int2, &int3, &int4, &int1 };
        using (var testArrays = new TestArrays())
        {
            Assert.That(testArrays.TakeArrays(pointers, ints, intPointers), Is.EqualTo(80));
            Assert.That(testArrays.VirtualTakeArrays(pointers, ints, intPointers), Is.EqualTo(80));
        }
    }

    [Test]
    public void TestStringArrayParams()
    {
        string[] strings = { "The ", "test ", "works." };
        using (var testArrays = new TestArrays())
        {
            Assert.That(testArrays.TakeStringArray(strings), Is.EqualTo("The test works."));
            Assert.That(testArrays.TakeConstStringArray(strings), Is.EqualTo("The test works."));
        }
    }

    [Test]
    public void TestHasFixedArrayOfPointers()
    {
        using (var hasFixedArrayOfPointers = new HasFixedArrayOfPointers())
        {
            var foos = new Foo[] { new Foo() { A = 5 }, new Foo { A = 15 }, new Foo { A = 20 } };
            hasFixedArrayOfPointers.FixedArrayOfPointers = foos;
            for (int i = 0; i < hasFixedArrayOfPointers.FixedArrayOfPointers.Length; i++)
                Assert.That(hasFixedArrayOfPointers.FixedArrayOfPointers[i].A,
                    Is.EqualTo(foos[i].A));
            foreach (var foo in foos)
                foo.Dispose();
        }
    }

    [Test]
    public void TestVirtualIndirectCallInNative()
    {
        using (Inter i = new Inter())
        {
            using (InterfaceTester tester = new InterfaceTester())
            {
                tester.SetInterface(i);
                Assert.That(tester.Get(10), Is.EqualTo(IntPtr.Zero));
            }
        }
    }

    [Test]
    public void TestConstCharStarRef()
    {
        Assert.That(CSharp.CSharpCool.TakeConstCharStarRef("Test"), Is.EqualTo("Test"));
    }

    [Test]
    public void TestRValueReferenceToPointer()
    {
        int value = 5;
        IntPtr intPtr = CSharp.CSharpCool.RValueReferenceToPointer((IntPtr*)&value);
        Assert.That((int)intPtr, Is.EqualTo(value));
    }

    [Test]
    public void TakeRefToPointerToObject()
    {
        using (Foo foo = new Foo { A = 25 })
        {
            Foo returnedFoo = CSharp.CSharpCool.TakeReturnReferenceToPointer(foo);
            Assert.That(returnedFoo.A, Is.EqualTo(foo.A));
            using (Qux qux = new Qux())
            {
                Assert.That(qux.TakeReferenceToPointer(foo), Is.EqualTo(foo.A));
            }
        }
    }

    [Test]
    public void TestImplicitConversionToString()
    {
        using (Foo foo = new Foo("name"))
        {
            string name = foo;
            Assert.That(name, Is.EqualTo("name"));
        }
    }

    [Test]
    public void TestHasFunctionPointerField()
    {
        using (var hasFunctionPtrField = new HasFunctionPtrField())
        {
            hasFunctionPtrField.FunctionPtrField = @string => @string.Length;
            Assert.That(hasFunctionPtrField.FunctionPtrField("Test"), Is.EqualTo(4));
            hasFunctionPtrField.FunctionPtrTakeFunctionPtrField = field => field();
            Assert.That(hasFunctionPtrField.FunctionPtrTakeFunctionPtrField(() => 42), Is.EqualTo(42));
        }
    }

    public class Inter : SimpleInterface
    {
        public override int Size => s;
        public override int Capacity => s;
        public override IntPtr Get(int n) { return new IntPtr(0); }
        private int s = 0;
    }

    private class OverrideVirtualTemplate : VirtualTemplate<int>
    {
        public override int Function() => 10;
    }

    [Test]
    public void TestTypemapTypedefParam()
    {
        Assert.That(CSharp.CSharpCool.TakeTypemapTypedefParam(false), Is.False);
    }

    [Test]
    public void TestStringMarshall()
    {
        var strings = new[] { "ЀЁЂЃЄЅІЇЈЉЊЋЌЍЎЏАБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюя" +
                              "ѐёђѓєѕіїјљњћќѝўџѠѡѢѣѤѥѦѧѨѩѪѫѬѭѮѯѰѱѲѳѴѵѶѷѸѹѺѻѼѽѾѿҀҁҊҋҌҍҎҏҐґҒғҔҕҖҗҘҙҚқҜҝҞҟҠҡҢңҤҥҦҧҨҩ" +
                              "ҪҫҬҭҮүҰұҲҳҴҵҶҷҸҹҺһҼҽҾҿӀӁӂӃӄӅӆӇӈӉӊӋӌӍӎӏӐӑӒӓӔӕӖӗӘәӚӛӜӝӞӟӠӡӢӣӤӥӦӧӨөӪӫӬӭӮӯӰӱӲӳӴӵӶӷӸӹӺӻӼӽ" +
                              "ӾӿԀԁԂԃԄԅԆԇԈԉԊԋԌԍԎԏԐԑԒԓ",

                              "აბგდევზთიკლმნოპჟრსტუფქღყშჩცძწჭხჯჰჱჲჳჴჵჶჷჸჹჺ",

                              "ԱԲԳԴԵԶԷԸԹԺԻԼԽԾԿՀՁՂՃՄՅՆՇՈՉՊՋՌՍՎՏՐՑՒՓՔՕՖՙաբգդեզէըթժիլխծկհձղճմյնշոչպջռսվտրցւփքօֆև",

                              "々〆〱〲〳〴〵〻〼ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづて" +
                              "でとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎわゐゑをんゔゕ" +
                              "ゖゝゞゟァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニ" +
                              "ヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワヰヱヲンヴヵヶヷヸヹヺ" +
                              "ーヽヾヿㇰㇱㇲㇳㇴㇵㇶㇷㇸㇹㇺㇻㇼㇽㇾㇿ",

                              "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzªµºÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞ" +
                              "ßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵ" +
                              "ĶķĸĹĺĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſƀƁƂƃƄƅƆƇƈƉƊ" +
                              "ƋƌƍƎƏƐƑƒƓƔƕƖƗƘƙƚƛƜƝƞƟƠơƢƣƤƥƦƧƨƩƪƫƬƭƮƯưƱƲƳƴƵƶƷƸƹƺƻƼƽƾƿǀǁǂǃǄǅǆǇǈǉǊǋǌǍǎǏǐǑǒǓǔǕǖǗǘǙǚǛǜǝ" +
                              "ǞǟǠǡǢǣǤǥǦǧǨǩǪǫǬǭǮǯǰǱǲǳǴǵǶǷǸǹǺǻǼǽǾǿȀȁȂȃȄȅȆȇȈȉȊȋȌȍȎȏȐȑȒȓȔȕȖȗȘșȚțȜȝȞȟȠȡȢȣȤȥȦȧȨȩȪȫȬȭȮȯȰȱȲȳ" +
                              "ȴȵȶȷȸȹȺȻȼȽȾȿɀɁɂɃɄɅɆɇɈɉɊɋɌɍɎɏḀḁḂḃḄḅḆḇḈḉḊḋḌḍḎḏḐḑḒḓḔḕḖḗḘḙḚḛḜḝḞḟḠḡḢḣḤḥḦḧḨḩḪḫḬḭḮḯḰḱḲḳḴḵḶḷḸḹḺḻḼḽ" +
                              "ḾḿṀṁṂṃṄṅṆṇṈṉṊṋṌṍṎṏṐṑṒṓṔṕṖṗṘṙṚṛṜṝṞṟṠṡṢṣṤṥṦṧṨṩṪṫṬṭṮṯṰṱṲṳṴṵṶṷṸṹṺṻṼṽṾṿẀẁẂẃẄẅẆẇẈẉẊẋẌẍẎẏẐẑẒẓẔẕẖẗẘẙẚ" +
                              "ẛẞẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ" +
                              "ⱠⱡⱢⱣⱤⱥⱦⱧⱨⱩⱪⱫⱬⱭⱱⱲⱳⱴⱵⱶⱷ" };

        foreach (var @string in strings)
        {
            var cs = @string;
            Assert.That(CSharp.CSharpCool.TestCSharpString(cs, out var @out), Is.EqualTo(cs));
            Assert.That(@out, Is.EqualTo(cs));
            Assert.That(CSharp.CSharpCool.TestCSharpStringWide(cs, out var outWide), Is.EqualTo(cs));
            Assert.That(outWide, Is.EqualTo(cs));
            Assert.That(CSharp.CSharpCool.TestCSharpString16(cs, out var out16), Is.EqualTo(cs));
            Assert.That(out16, Is.EqualTo(cs));
            Assert.That(CSharp.CSharpCool.TestCSharpString32(cs, out var out32), Is.EqualTo(cs));
            Assert.That(out32, Is.EqualTo(cs));
        }
    }

    [Test]
    public void TestConversionFunction()
    {
        using (var c = new ConversionFunctions())
        {
            Assert.That(*(short*)c, Is.EqualTo(100));
            Assert.That((short)c, Is.EqualTo(100));
        }
    }

    [Test]
    public void TestFunctionToStaticMethod()
    {
        Assert.That(CSharp.CSharpCool.TestFunctionToStaticMethod(new FTIStruct()).A, Is.EqualTo(6));
        Assert.That(CSharp.CSharpCool.TestFunctionToStaticMethodStruct(new FTIStruct(), new FTIStruct() { A = 6 }), Is.EqualTo(6));
        Assert.That(CSharp.CSharpCool.TestFunctionToStaticMethodRefStruct(new FTIStruct(), new FTIStruct() { A = 6 }), Is.EqualTo(6));
        Assert.That(CSharp.CSharpCool.TestFunctionToStaticMethodConstStruct(new FTIStruct(), new FTIStruct() { A = 6 }), Is.EqualTo(6));
        Assert.That(CSharp.CSharpCool.TestFunctionToStaticMethodConstRefStruct(new FTIStruct(), new FTIStruct() { A = 6 }), Is.EqualTo(6));
    }

    [Test]
    public void TestFunctionToInstanceMethod()
    {
        Assert.That(new TestClass().FunctionToInstanceMethod(5), Is.EqualTo(25));
        Assert.That(new TestClass().FunctionToInstanceMethod(new FTIStruct() { A = 5 }), Is.EqualTo(25));
    }

    [TestCase(typeof(FTIStruct), ExpectedResult = true)]
    [TestCase(typeof(ClassWithoutNativeToManaged), ExpectedResult = false)]
    public bool TestClassGenerateNativeToManaged(Type type)
    {
        return type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Any(x => x.Name.Contains("NativeToManaged"));
    }

    [Test]
    public void TestFunctionTemplate()
    {
        Assert.That(CSharpTemplatesCool.FunctionTemplate(5.0), Is.EqualTo(5 + 4.2));
        Assert.That(CSharpTemplatesCool.FunctionTemplate(6f), Is.EqualTo(6 + 4.1f));
        Assert.That(CSharpTemplatesCool.FunctionTemplate(7), Is.EqualTo(7 + 4));
    }

    [Test]
    public void TestPatialRefSupport()
    {
        var myclass = new ClassWithIntValue();
        var backup = myclass;
        myclass.Value = 7;

        CSharp.CSharpCool.ModifyCore(ref myclass);
        Assert.That(myclass.Value, Is.EqualTo(10));
        Assert.That(myclass, Is.SameAs(myclass));

        CSharp.CSharpCool.CreateCore(ref myclass);
        Assert.That(myclass.Value, Is.EqualTo(20));
        Assert.That(myclass, Is.Not.SameAs(backup));
    }


    class CallByValueInterfaceImpl : CallByValueInterface
    {
        public override void CallByValue(RuleOfThreeTester value)
        {
        }
        public override void CallByReference(RuleOfThreeTester value)
        {
        }
        public override void CallByPointer(RuleOfThreeTester value)
        {
        }
    }

    [Test]
    public void TestCallByValueCppToCSharpValue()
    {
        RuleOfThreeTester.Reset();
        CallByValueInterface @interface = new CallByValueInterfaceImpl();
        CSharp.CSharpCool.CallCallByValueInterfaceValue(@interface);

        Assert.That(RuleOfThreeTester.ConstructorCalls, Is.EqualTo(1));
        Assert.That(RuleOfThreeTester.DestructorCalls, Is.EqualTo(2));
        Assert.That(RuleOfThreeTester.CopyConstructorCalls, Is.EqualTo(1));
        Assert.That(RuleOfThreeTester.CopyAssignmentCalls, Is.EqualTo(0));
    }

    [Test]
    public void TestCallByValueCppToCSharpReference()
    {
        RuleOfThreeTester.Reset();
        CallByValueInterface @interface = new CallByValueInterfaceImpl();
        CSharp.CSharpCool.CallCallByValueInterfaceReference(@interface);

        Assert.That(RuleOfThreeTester.ConstructorCalls, Is.EqualTo(1));
        Assert.That(RuleOfThreeTester.DestructorCalls, Is.EqualTo(1));
        Assert.That(RuleOfThreeTester.CopyConstructorCalls, Is.EqualTo(0));
        Assert.That(RuleOfThreeTester.CopyAssignmentCalls, Is.EqualTo(0));
    }

    [Test]
    public void TestCallByValueCppToCSharpPointer()
    {
        RuleOfThreeTester.Reset();
        CallByValueInterface @interface = new CallByValueInterfaceImpl();
        CSharp.CSharpCool.CallCallByValueInterfacePointer(@interface);

        Assert.That(RuleOfThreeTester.ConstructorCalls, Is.EqualTo(1));
        Assert.That(RuleOfThreeTester.DestructorCalls, Is.EqualTo(1));
        Assert.That(RuleOfThreeTester.CopyConstructorCalls, Is.EqualTo(0));
        Assert.That(RuleOfThreeTester.CopyAssignmentCalls, Is.EqualTo(0));
    }

    [Test]
    public void TestPointerToClass()
    {
        Assert.IsTrue(CSharp.CSharpCool.PointerToClass.IsDefaultInstance);
        Assert.IsTrue(CSharp.CSharpCool.PointerToClass.IsValid);
    }

    [Test]
    public void TestValueTypeOutParameter()
    {
        Assert.AreEqual(2, CSharp.CSharpCool.ValueTypeOutParameter(out var unionTestA, out var unionTestB));
        Assert.AreEqual(2, unionTestA.A);
        Assert.AreEqual(2, unionTestB.B);
    }

    [TestCase("hi")]
    [TestCase(2u)]
    public void TestOptional<T>(T value)
    {
        Assert.That(new CSharp.Optional<T>() != new CSharp.Optional<T>(value));
        Assert.That(new CSharp.Optional<T>() != value);
        Assert.That(new CSharp.Optional<T>() == new CSharp.Optional<T>());
        Assert.That(new CSharp.Optional<T>(value) == new CSharp.Optional<T>(value));
        Assert.That(new CSharp.Optional<T>(value) == value);
    }

    [Test]
    public void TestOptionalIntPtr()
    {
        Assert.That(new CSharp.Optional<IntPtr>() != new CSharp.Optional<IntPtr>(IntPtr.MaxValue));
        Assert.That(new CSharp.Optional<IntPtr>() != IntPtr.MaxValue);
        Assert.That(new CSharp.Optional<IntPtr>() == new CSharp.Optional<IntPtr>());
        Assert.That(new CSharp.Optional<IntPtr>(IntPtr.MaxValue) == new CSharp.Optional<IntPtr>(IntPtr.MaxValue));
        Assert.That(new CSharp.Optional<IntPtr>(IntPtr.MaxValue) == IntPtr.MaxValue);
    }

    [Test]
    public void TestValueTypeStringMember()
    {
        var test = new CSharp.ValueType();
        Assert.AreEqual(string.Empty, test.StringMember);
        Assert.AreEqual(null, test.CharPtrMember);
        test.StringMember = "test";
        test.CharPtrMember = "test2";
        Assert.AreEqual("test", test.StringMember);
        Assert.AreEqual("test2", test.CharPtrMember);
        test.Dispose();
    }

    [Test]
    [Ignore("https://github.com/mono/CppSharp/issues/1786")]
    public void TestValueTypeStringMemberDefaulted()
    {
        CSharp.ValueType test = default;
        Assert.AreEqual(string.Empty, test.StringMember);
        Assert.AreEqual(null, test.CharPtrMember);
        test.StringMember = "test";
        test.CharPtrMember = "test2";
        Assert.AreEqual("test", test.StringMember);
        Assert.AreEqual("test2", test.CharPtrMember);
        test.Dispose();
    }

    [Test]
    public void TestValueTypeStringMemberDefaultedCtor()
    {
        var test = new CSharp.ValueTypeNoCtor();
        Assert.AreEqual(string.Empty, test.StringMember);
        Assert.AreEqual(null, test.CharPtrMember);
        test.StringMember = "test";
        test.CharPtrMember = "test2";
        Assert.AreEqual("test", test.StringMember);
        Assert.AreEqual("test2", test.CharPtrMember);
        test.Dispose();
    }
}
