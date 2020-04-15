using CppSharp.Utils;
using NUnit.Framework;
using CLI;

public class CLITests : GeneratorTestFixture
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
    public void TestPointerToTypedefPointerTestMethod()
    {
        using (PointerToTypedefPointerTest lp = new PointerToTypedefPointerTest())
        {
            lp.Val = 50;
            CLI.CLI.PointerToTypedefPointerTestMethod(lp, 100);
            Assert.AreEqual(100, lp.Val);
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