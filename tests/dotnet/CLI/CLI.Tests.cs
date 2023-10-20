using NUnit.Framework;
using CLI;
using System.Text;
using System;

[TestFixture]
public class CLITests
{
    [Test]
    public void TestTypes()
    {
        // Attributed types
        using (var types = new Types())
        {
            var sum = types.AttributedSum(3, 4);
            Assert.That(sum, Is.EqualTo(7));
        }
    }

    [Test]
    public void TestStdString()
    {
        using (var date = new Date(0, 0, 0))
        {
            Assert.AreEqual("test_test", date.TestStdString("test"));
        }
    }

    [Test]
    public void GetEmployeeNameFromOrgTest()
    {
        using (var org = new EmployeeOrg())
        {
            Assert.AreEqual("Employee", org.Employee.Name);
        }
    }

    [Test]
    public void TestConsumerOfEnumNestedInClass()
    {
        using (var consumer = new NestedEnumConsumer())
        {
            Assert.AreEqual(ClassWithNestedEnum.NestedEnum.E1, consumer.GetPassedEnum(ClassWithNestedEnum.NestedEnum.E1));
        }
    }

    [Test]
    public void TestChangePassedMappedTypeNonConstRefParam()
    {
        using (var consumer = new TestMappedTypeNonConstRefParamConsumer())
        {
            string val = "Initial";
            consumer.ChangePassedMappedTypeNonConstRefParam(ref val);

            Assert.AreEqual("ChangePassedMappedTypeNonConstRefParam", val);
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

    [Test]
    public void TestMultipleConstantArraysParamsTestMethod()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("TestMulti");
        sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));

        byte[] bytes2 = Encoding.ASCII.GetBytes("TestMulti2");
        sbyte[] sbytes2 = Array.ConvertAll(bytes2, q => Convert.ToSByte(q));

        string s = CLI.CLICool.MultipleConstantArraysParamsTestMethod(sbytes, sbytes2);
        Assert.AreEqual("TestMultiTestMulti2", s);
    }

    [Test]
    public void TestMultipleConstantArraysParamsTestMethodLongerSourceArray()
    {
        byte[] bytes = Encoding.ASCII.GetBytes("TestMultipleConstantArraysParamsTestMethodLongerSourceArray");
        sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));

        Assert.Throws<InvalidOperationException>(() => CLI.CLICool.MultipleConstantArraysParamsTestMethod(sbytes, new sbyte[] { }));
    }

    [Test]
    public void TestStructWithNestedUnionTestMethod()
    {
        using (var val = new StructWithNestedUnion())
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

            string ret = CLI.CLICool.StructWithNestedUnionTestMethod(val);

            Assert.AreEqual("TestUnions", ret);
        }
    }

    [Test]
    public void TestStructWithNestedUnionLongerSourceArray()
    {
        using (var val = new StructWithNestedUnion())
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
        using (var val = new StructNestedInsideUnion())
        {
            byte[] bytes = Encoding.ASCII.GetBytes("TestUnions");
            sbyte[] sbytes = Array.ConvertAll(bytes, q => Convert.ToSByte(q));
            val.SzText = sbytes;

            UnionWithNestedStruct unionWithNestedStruct;
            unionWithNestedStruct.NestedStruct = val;

            Assert.AreEqual(10, unionWithNestedStruct.NestedStruct.SzText.Length);
            Assert.AreEqual("TestUnions", unionWithNestedStruct.NestedStruct.SzText);

            string ret = CLI.CLICool.UnionWithNestedStructTestMethod(unionWithNestedStruct);

            Assert.AreEqual("TestUnions", ret);
        }
    }

    [Test]
    public void TestUnionWithNestedStructArrayTestMethod()
    {
        using (var val = new StructNestedInsideUnion())
        {
            using (var val2 = new StructNestedInsideUnion())
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

                string ret = CLI.CLICool.UnionWithNestedStructArrayTestMethod(unionWithNestedStructArray);

                Assert.AreEqual("TestUnion1TestUnion2", ret);
            }
        }
    }
}