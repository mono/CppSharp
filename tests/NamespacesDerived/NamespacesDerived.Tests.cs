using NamespacesDerived;
using NUnit.Framework;

[TestFixture]
public class NamespaceDerivedTests
{
    [Test]
    public void TestNonRenamedMethod()
    {
        var derived = new Derived();
        var parent = derived.Parent;
        derived.parent(0);
    }
}