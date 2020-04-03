using CppSharp.Utils;
using NUnit.Framework;
using CLI;

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
}