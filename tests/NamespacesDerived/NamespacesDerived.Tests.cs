using NamespacesDerived;
using NUnit.Framework;

[TestFixture]
public class NamespaceDerivedTests
{
    [Test]
    public void TestCodeGeneration()
    {
        using (new DerivedFromSecondaryBaseInDependency()) { }
    }

    [Test]
    public void TestNonRenamedMethod()
    {
        using (var derived = new Derived())
        {
            var parent = derived.Parent;
            derived.parent(0);
        }
    }

    [Test]
    public void TestOverrideMethodFromDependency()
    {
        using (var overrideMethodFromDependency = new OverrideMethodFromDependency())
        {
            using (var managedObject = new OverrideMethodFromDependency())
            {
                overrideMethodFromDependency.ManagedObject = managedObject;
                Assert.That(overrideMethodFromDependency.CallManagedOverride(), Is.EqualTo(2));
            }
        }
    }

    private class OverrideMethodFromDependency : HasVirtualInDependency
    {
        public override int VirtualInCore(int parameter) => 2;
    }
}