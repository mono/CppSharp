using NamespacesDerived;
using NUnit.Framework;

[TestFixture]
public class NamespaceDerivedTests
{
    [Test]
    public void TestNonRenamedMethod()
    {
        // TODO: the premake is broken and does not add a reference to NamespaceBase.CSharp
        //var derived = new Derived();
        //var parent = derived.Parent;
        //derived.parent(0);
    }
}