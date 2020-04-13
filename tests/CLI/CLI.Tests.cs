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
}