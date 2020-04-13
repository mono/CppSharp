using CppSharp.Utils;
using NUnit.Framework;
using CLI;
using System.Text;
using System;

public class CLITests : GeneratorTestFixture
{
    [Test]
    public void TestTypes()
    {
        // Attributed types
        var sum = new Types().AttributedSum(3, 4);
        Assert.That(sum, Is.EqualTo(7));
    }

    [Test]
    public void TestStdString()
    {
        Assert.AreEqual("test_test", new Date(0, 0, 0).TestStdString("test"));
    }

    [Test]
    public void TestByRefEnumParam()
    {
        using (var byRefEnumParam = new TestByRefEnumParam())
        {
            Assert.AreEqual(EnumParam.E1, byRefEnumParam.GetPassedEnumParam(EnumParam.E1));
        }
    }

    [Test]
    public void GetEmployeeNameFromOrgTest()
    {
        using (EmployeeOrg org = new EmployeeOrg())
        {
            Assert.AreEqual("Employee", org.Employee.Name);
        }
    }

    [Test]
    public void TestConsumerOfEnumNestedInClass()
    {
        using (NestedEnumConsumer consumer = new NestedEnumConsumer())
        {
            Assert.AreEqual(ClassWithNestedEnum.NestedEnum.E1, consumer.GetPassedEnum(ClassWithNestedEnum.NestedEnum.E1));
        }
    }

    [Test]
    public void TestChangePassedMappedTypeNonConstRefParam()
    {
        using (TestMappedTypeNonConstRefParamConsumer consumer = new TestMappedTypeNonConstRefParamConsumer())
        {
            string val = "Initial";
            consumer.ChangePassedMappedTypeNonConstRefParam(ref val);

            Assert.AreEqual("ChangePassedMappedTypeNonConstRefParam", val);
        }
    }

    [Test]
    public void TestMultipleConstantArraysParamsTestMethod()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("TestMulti");
        sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));

        byte[] bytes2 = Encoding.ASCII.GetBytes("TestMulti2");
        sbyte[] sbytes2 = Array.ConvertAll(bytes2, q => Convert.ToSByte(q));

        string s = CLI.CLI.MultipleConstantArraysParamsTestMethod(sbytes, sbytes2);
        Assert.AreEqual("TestMultiTestMulti2", s);
    }

    [Test]
    public void TestMultipleConstantArraysParamsTestMethodLongerSourceArray()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("TestMultipleConstantArraysParamsTestMethodLongerSourceArray");
        sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));

        Assert.Throws<InvalidOperationException>(() => CLI.CLI.MultipleConstantArraysParamsTestMethod(sbytes, new sbyte[] { }));
    }

    [Test]
    public void TestPointerToTypedefPointerTestMethod()
    {
        int a = 50;
        using (PointerToTypedefPointerTest lp = new PointerToTypedefPointerTest())
        {
            CLI.CLI.PointerToTypedefPointerTestMethod(lp, 100);
            Assert.AreEqual(100, lp.Val);
        }
    }

    [Test]
    public void TestPointerToPrimitiveTypedefPointerTestMethod()
    {
        int a = 50;
        CLI.CLI.PointerToPrimitiveTypedefPointerTestMethod(ref a, 100);
        Assert.AreEqual(100, a);
    }

    [Test]
    public void TestStructWithNestedUnionTestMethod()
    {
        using (StructWithNestedUnion val = new StructWithNestedUnion())
        {
            byte[] bytes = Encoding.ASCII.GetBytes("TestUnions");
            sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));

            UnionNestedInsideStruct unionNestedInsideStruct;
            unionNestedInsideStruct.SzText = sbytes;

            Assert.AreEqual(sbytes.Length, unionNestedInsideStruct.SzText.Length);
            Assert.AreEqual("TestUnions", unionNestedInsideStruct.SzText);

            val.NestedUnion = unionNestedInsideStruct;

            Assert.AreEqual(10, val.NestedUnion.SzText.Length);
            Assert.AreEqual("TestUnions", val.NestedUnion.SzText);

            string ret = CLI.CLI.StructWithNestedUnionTestMethod(val);

            Assert.AreEqual("TestUnions", ret);
        }
    }

    [Test]
    public void TestStructWithNestedUnionLongerSourceArray()
    {
        using (StructWithNestedUnion val = new StructWithNestedUnion())
        {
            byte[] bytes = Encoding.ASCII.GetBytes("TestStructWithNestedUnionLongerSourceArray");
            sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));

            UnionNestedInsideStruct unionNestedInsideStruct;
            unionNestedInsideStruct.SzText = sbytes;

            Assert.Throws<InvalidOperationException>(() => val.NestedUnion = unionNestedInsideStruct);
        }
    }

    [Test]
    public void TestUnionWithNestedStructTestMethod()
    {
        using (StructNestedInsideUnion val = new StructNestedInsideUnion())
        {
            byte[] bytes = Encoding.ASCII.GetBytes("TestUnions");
            sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));
            val.SzText = sbytes;

            UnionWithNestedStruct unionWithNestedStruct;
            unionWithNestedStruct.NestedStruct = val;

            Assert.AreEqual(10, unionWithNestedStruct.NestedStruct.SzText.Length);
            Assert.AreEqual("TestUnions", unionWithNestedStruct.NestedStruct.SzText);

            string ret = CLI.CLI.UnionWithNestedStructTestMethod(unionWithNestedStruct);

            Assert.AreEqual("TestUnions", ret);
        }
    }

    [Test]
    public void TestUnionWithNestedStructArrayTestMethod()
    {
        using (StructNestedInsideUnion val = new StructNestedInsideUnion())
        {
            using (StructNestedInsideUnion val2 = new StructNestedInsideUnion())
            {
                byte[] bytes = Encoding.ASCII.GetBytes("TestUnion1");
                sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));
                val.SzText = sbytes;

                byte[] bytes2 = Encoding.ASCII.GetBytes("TestUnion2");
                sbyte[] sbytes2 = Array.ConvertAll(bytes2, q => Convert.ToSByte(q));
                val2.SzText = sbytes2;

                UnionWithNestedStructArray unionWithNestedStructArray;
                unionWithNestedStructArray.NestedStructs = new StructNestedInsideUnion[] { val, val2 };

                Assert.AreEqual(2, unionWithNestedStructArray.NestedStructs.Length);

                string ret = CLI.CLI.UnionWithNestedStructArrayTestMethod(unionWithNestedStructArray);

                Assert.AreEqual("TestUnion1TestUnion2", ret);
            }
        }
    }

    [Test]
    public void TestVectorPointerGetter()
    {
        using (VectorPointerGetter v = new VectorPointerGetter())
        {
            var list = v.VecPtr;

            Assert.AreEqual(1, list.Count);

            Assert.AreEqual("VectorPointerGetter", list[0]);
        }
    }
}