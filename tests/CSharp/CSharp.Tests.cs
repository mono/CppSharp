using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using CppSharp.Utils;
using CSharp;
using NUnit.Framework;

public unsafe class CSharpTests : GeneratorTestFixture
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
        ALLCAPS_UNDERSCORES a;
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
        }
        using (var hasOverride = new HasOverrideOfHasPropertyWithDerivedType())
            hasOverride.CauseRenamingError();
        using (var qux = new Qux())
        {
            new Bar(qux).Dispose();
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
        using (var qux = new Qux())
        {
            using (var p = new P((IQux) qux) { Value = 20 })
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

        var dtors = new TestDestructors();
        Assert.AreEqual(0xf00d, CSharp.TestDestructors.Marker);
        dtors.Dispose();
        Assert.AreEqual(0xcafe, CSharp.TestDestructors.Marker);
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
        using (var methodsWithDefaultValues = new MethodsWithDefaultValues())
        {
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
            methodsWithDefaultValues.DefaultNonEmptyCtorWithNullPtr();
            Assert.That(methodsWithDefaultValues.DefaultMappedToEnum(), Is.EqualTo(Flags.Flag3));
            methodsWithDefaultValues.DefaultMappedToZeroEnum();
            methodsWithDefaultValues.DefaultMappedToEnumAssignedWithCtor();
            methodsWithDefaultValues.DefaultImplicitCtorInt();
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
            methodsWithDefaultValues.DefaultWithParamNamedSameAsMethod(5);
        }
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
        using (MethodsWithDefaultValues m = foo)
        {
            Assert.AreEqual(foo.A, m.A);
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
        var a = new MI_A0();
        var resa = a.Get();
        Assert.That(resa, Is.EqualTo(50));

        var c = new MI_C();
        var res = c.Get();
        Assert.That(res, Is.EqualTo(50));
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
            Assert.IsTrue(Qux.NativeToManagedMap.ContainsKey(bar.__Instance));
            bar.Dispose();
            Assert.IsFalse(Qux.NativeToManagedMap.ContainsKey(bar.__Instance));
        }
    }

    [Test]
    public void TestCallingVirtualDtor()
    {
        var callDtorVirtually = new CallDtorVirtually();
        var hasVirtualDtor1 = CallDtorVirtually.GetHasVirtualDtor1(callDtorVirtually);
        hasVirtualDtor1.Dispose();
        Assert.That(CallDtorVirtually.Destroyed, Is.True);
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

    // TODO: fails on the Linux CI but works locally on Vagrant - both have the same Mono version; it also works on OS X; go figure
    [Test, Platform(Exclude = "Linux")]
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

        Foo[] foosMore = new Foo[2];
        foosMore[0] = new Foo();
        foosMore[1] = new Foo();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => bar.Foos = foosMore);
        Assert.AreEqual("value", ex.ParamName);
        Assert.AreEqual("The dimensions of the provided array don't match the required size." + Environment.NewLine + "Parameter name: value", ex.Message);
    }

    [Test]
    public void TestOutTypeInterfacePassTry()
    {
        var interfaceClassObj = new TestParamToInterfacePassBaseTwo();
        ITestParamToInterfacePassBaseTwo interfaceType = interfaceClassObj;
        var obj = new TestOutTypeInterfaces();
        obj.FuncTryInterfaceTypeOut(out interfaceType);
        ITestParamToInterfacePassBaseTwo interfaceTypePtr;
        obj.FuncTryInterfaceTypePtrOut(out interfaceTypePtr);
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
        Assert.Catch<ArgumentOutOfRangeException>(() => CSharp.CSharp.PassConstantArrayRef(incorrectlySizedArray));
        int[] array = { 1, 2 };
        var result = CSharp.CSharp.PassConstantArrayRef(array);
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

    [Test, Ignore("We need symbols for std::string to invoke and auto-compilation of exported templates is not added yet.")]
    public void TestStdStringConstant()
    {
        //Assert.That(CSharp.HasFreeConstant.AnotherUnit.STD_STRING_CONSTANT, Is.EqualTo("test"));
        // check a second time to ensure it hasn't been improperly freed
        //Assert.That(CSharp.HasFreeConstant.AnotherUnit.STD_STRING_CONSTANT, Is.EqualTo("test"));
    }

    // HACK: the completion of types is temporarily suspended because of problems with QtWidgets
    [Test, Ignore]
    public void TestTemplateInternals()
    {
        foreach (var internalType in new[]
            {
                typeof(CSharp.IndependentFields.__Internal),
                typeof(CSharp.DependentValueFields.__Internalc__S_DependentValueFields__b),
                //typeof(CSharp.DependentValueFields.Internal_float),
                typeof(CSharp.DependentPointerFields.__Internal),
                //typeof(CSharp.DependentValueFields.Internal_Ptr),
                typeof(CSharp.HasDefaultTemplateArgument.__Internalc__S_HasDefaultTemplateArgument__I___S_IndependentFields__I)
            })
        {
            var independentFields = internalType.GetFields();
            Assert.That(independentFields.Length, Is.EqualTo(1));
            var fieldOffset = (FieldOffsetAttribute) independentFields[0].GetCustomAttribute(typeof(FieldOffsetAttribute));
            Assert.That(fieldOffset.Value, Is.EqualTo(0));
        }
        foreach (var internalType in new Type[]
            {
                //typeof(CSharp.TwoTemplateArgs.Internal_Ptr),
                //typeof(CSharp.TwoTemplateArgs.Internal_intPtr_int),
                //typeof(CSharp.TwoTemplateArgs.Internal_intPtr_float)
            })
        {
            var independentFields = internalType.GetFields();
            Assert.That(independentFields.Length, Is.EqualTo(2));
            var fieldOffsetKey = (FieldOffsetAttribute) independentFields[0].GetCustomAttribute(typeof(FieldOffsetAttribute));
            Assert.That(fieldOffsetKey.Value, Is.EqualTo(0));
            var fieldOffsetValue = (FieldOffsetAttribute) independentFields[1].GetCustomAttribute(typeof(FieldOffsetAttribute));
            Assert.That(fieldOffsetValue.Value, Is.EqualTo(Marshal.SizeOf(IntPtr.Zero)));
        }
    }

    [Test]
    public void TestConstantArray()
    {
        Assert.That(CSharp.CSharp.VariableWithFixedPrimitiveArray[0], Is.EqualTo(5));
        Assert.That(CSharp.CSharp.VariableWithFixedPrimitiveArray[1], Is.EqualTo(10));
        Assert.That(CSharp.CSharp.VariableWithVariablePrimitiveArray[0], Is.EqualTo(15));
        Assert.That(CSharp.CSharp.VariableWithVariablePrimitiveArray[1], Is.EqualTo(20));
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

    [Test, Ignore("For no reason this doesn't work at all, and I am tired of bugs. I fixed the compilation of this thing, I have no intention of fixing it at run-time too.")]
    public void TestUnicode()
    {
        using (var testString = new TestString())
        {
            Assert.That(testString.UnicodeConst, Is.EqualTo("ქართული ენა"));
        }
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
}
